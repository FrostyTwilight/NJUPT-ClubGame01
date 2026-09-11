using Godot;
using NJUPTClubGame.scripts;
using System;
using System.Threading;
using System.Threading.Tasks;

public partial class PlayerController : RigidBody2D
{
	[Export]
	public RayCast2D MoveRayCast { get; set; }
	[Export]
	public float Speed { get; set; }
	[Export]
	public float JumpHight { get; set; }
	[Export]
	public bool CanMove { get; set; } = true;
	[Export]
	public Gun Gun { get; set; }
	[Export]
	public float LineForceFactor { get; set; }
	[Export]
	public float LineLenModifierFactor { get; set; }
	[Export]
	public float LineBreakLimit { get; set; }

	private CustomFSM fsm;

	private float move_axis;
	private bool is_jump;
	private Vector2? modify_vel;

	private bool hook_thrown;
	private float hook_length;

	private Tween shoot_tween;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		fsm = new();

		fsm.SwitchToState(State_Init);
	}

	private async Task State_Init(CustomFSM fsm, CancellationToken cancellation)
	{
		fsm.SwitchToState(State_Idle);
	}

	private async Task State_Idle(CustomFSM fsm, CancellationToken cancellation)
	{
		fsm.AddTransition("FIRE", State_Fire);
		fsm.AddTransition("JUMP", State_Jump);

		CanMove = true;
		hook_thrown = false;

	}

	private async Task State_Jump(CustomFSM fsm, CancellationToken cancellation)
	{
		fsm.AddTransition(CustomFSM.EVENT_FINISHED, State_Idle);

		is_jump = true;
	}

	private async Task State_Fire(CustomFSM fsm, CancellationToken cancellation)
	{
		CanMove = false;

		fsm.AddTransition(CustomFSM.EVENT_FINISHED, State_HookIdle);

		if(!Gun.Fire(out var collider))
		{
			fsm.SwitchToState(State_Idle);
		}

		

		var hook = Gun.Hook;
		var targetPos = hook.GlobalPosition;
		hook.GlobalPosition = Gun.GlobalPosition;

		hook_length = (hook.GlobalPosition - targetPos).Length();

		shoot_tween?.Kill();
		shoot_tween = hook.CreateTween();
		shoot_tween.TweenProperty(hook, "global_position",
			targetPos, 0.1)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Quad)
			.Finished += () =>
			{
				fsm.SendEvent("HOOK_FIRE");
			};

		await fsm.WaitForEvent("HOOK_FIRE");
	}

	// 终止牵引/断绳子
	private async Task State_Break(CustomFSM fsm, CancellationToken cancellation)
	{
		fsm.AddTransition(CustomFSM.EVENT_FINISHED, State_Idle);

		Gun.HideHook();

		hook_thrown = false;


		shoot_tween?.Kill();
		modify_vel = Vector2.Zero; //不停会飞出去

		shoot_tween = null;

		await fsm.NextFrame();

	}

	private async Task State_HookJump(CustomFSM fsm, CancellationToken cancellation)
	{
		fsm.AddTransition(CustomFSM.EVENT_FINISHED, State_HookIdle);

		is_jump = true;
	}
	private async Task State_HookIdle(CustomFSM fsm, CancellationToken cancellation)
	{
		fsm.AddTransition("FIRE", State_Drag); // 左键立刻牵引
		fsm.AddTransition("LINE_BREAK", State_Break);
		fsm.AddTransition("JUMP", State_HookJump);

		CanMove = true;
		hook_thrown = true;

	}

	private async Task State_Drag(CustomFSM fsm, CancellationToken cancellation)
	{
		fsm.AddTransition("LINE_BREAK", State_Break);
		fsm.AddTransition(CustomFSM.EVENT_FINISHED, State_HookIdle);

		hook_length = 0;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);

		fsm.Update(delta);

		if (CanMove) {

			//左右不分（ ）
			var move = Input.GetAxis("Left", "Right");
			move_axis = move * Speed;
		}
		else
		{
			move_axis = 0;
		}

		if(hook_thrown)
		{
			hook_length += Input.GetAxis("Line_Down", "Line_Up") * LineLenModifierFactor;

			if(hook_length < 0)
			{
				hook_length = 0;
			}
		}

		if(Input.IsActionJustPressed("Jump"))
		{
			fsm.SendEvent("JUMP");
		}

		if(Input.IsActionJustPressed("Fire"))
		{
			fsm.SendEvent("FIRE");
		}

		if(Input.IsActionJustPressed("Line_Break"))
		{
			fsm.SendEvent("LINE_BREAK");
		}
	}
	public override void _IntegrateForces(PhysicsDirectBodyState2D state)
	{
		base._IntegrateForces(state);
		var vel = state.LinearVelocity;
		vel.X = move_axis;

		if(is_jump)
		{
			is_jump = false;
			vel.Y = JumpHight;
		}
		state.LinearVelocity = vel;

		if(hook_thrown)
		{
			var offset = GlobalPosition - Gun.Hook.GlobalPosition;
			var distance = offset.Length();
			if(distance > hook_length)
			{
				var transform = state.Transform;
				if (distance > LineBreakLimit)
				{
					MoveRayCast.TargetPosition = -offset * 0.8f;
					MoveRayCast.ForceRaycastUpdate();
					
					if(MoveRayCast.IsColliding())
					{
						//断了
						fsm.SendEvent("LINE_BREAK");
						transform.Origin = MoveRayCast.GetCollisionPoint();
						goto FIN;
					}
				}

				transform.Origin -= offset.Normalized() * (distance - hook_length) * LineForceFactor;

				FIN:
				state.Transform = transform;
			}
		}

		if(modify_vel != null)
		{
			state.LinearVelocity = modify_vel.Value;
			modify_vel = null;
		}
	}
}

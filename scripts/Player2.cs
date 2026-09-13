using Godot;
using NJUPTClubGame.scripts;
using System;
using System.Threading;
using System.Threading.Tasks;

public partial class Player2 : CharacterBody2D
{
	[ExportCategory("Props")]
	[Export]
	public float Speed { get; set; } = 300.0f;
	[Export]
	public float JumpVelocity { get; set; } = -400.0f;
	[Export]
	public Color LineMissing { get; set; } = Colors.Red;
	[Export]
	public Color LineCaught { get; set; } = Colors.Green;
	[Export]
	public Color LineJoint { get; set; } = Colors.DarkGreen;
	[Export]
	public float LineWidth { get; set; } = 2;
	[Export]
	public float LineDashWidth { get; set; } = 2;


	[ExportCategory("Nodes")]
	[Export]
	public Node2D CurrentAnchor { get; set; }
	[Export]
	public Node2D Gun { get; set; }
	[Export]
	public Node2D GunEnd { get; set; }
	[Export]
	public RayCast2D GunRayCast { get; set; }

	private CustomFSM fsm;

	private float raycast_prev_timeout;
	private Node2D raycast_prev_anchor;
	private Node2D raycast_anchor;
	private Vector2 raycast_target;

	private float anchorDistance;

	public bool CanShootHook => CurrentAnchor == null;

	public override void _Ready()
	{
		fsm = new();
		fsm.SwitchToState(State_Idle);
	}

	private async Task State_Idle(CustomFSM fsm, CancellationToken cancellationToken)
	{
		fsm.AddTransition("FIRE", State_Fire);
	}

	private async Task State_Fire(CustomFSM fsm, CancellationToken cancellationToken)
	{
		if(raycast_anchor == null)
		{
			if (raycast_prev_anchor == null || raycast_prev_timeout < 0)
			{
				fsm.SwitchToState(State_Idle);
			}
			raycast_anchor = raycast_prev_anchor;
		}

		CurrentAnchor = raycast_anchor;
		anchorDistance = (CurrentAnchor.GlobalPosition - GlobalPosition).Length();

		fsm.SwitchToState(State_AnchorIdle);
	}

	private async Task State_AnchorIdle(CustomFSM fsm, CancellationToken cancellationToken)
	{
		fsm.AddTransition("LINE_BREAK", State_AnchorLineBreak);
	}

	private async Task State_AnchorLineBreak(CustomFSM fsm, CancellationToken cancellationToken)
	{
		CurrentAnchor = null;

		fsm.SwitchToState(State_Idle);
	}

	public override void _Process(double delta)
	{
		raycast_prev_timeout -= (float)delta;


		fsm.Update(delta);

		//处理输入
		{
			if(Input.IsActionJustPressed("Fire"))
			{
				fsm.SendEvent("FIRE");
			}
			if(Input.IsActionJustPressed("Line_Break"))
			{
				fsm.SendEvent("LINE_BREAK");
			}
		}

		// 处理 Gun 旋转
		{
			Vector2 targetPos;

			if (CanShootHook)
			{
				targetPos = GetGlobalMousePosition();
			}
			else
			{
				targetPos = CurrentAnchor.GlobalPosition;
			}

			var offset = targetPos - GlobalPosition;
			var angle = Mathf.Atan2(offset.Y, offset.X);
			Gun.Rotation = angle;
		}
		//处理命中
		if(CanShootHook)
		{
			raycast_anchor = null;

			GunRayCast.ForceRaycastUpdate();

			if(GunRayCast.IsColliding())
			{
				raycast_target = GunRayCast.GetCollisionPoint();
				if(GunRayCast.GetCollider() is CollisionObject2D collision)
				{
					if(collision.GetCollisionLayerValue(5))
					{
						raycast_anchor = collision;
						raycast_prev_anchor = raycast_anchor;
						raycast_prev_timeout = 0.5f;
					}
				}
			}
		}

		QueueRedraw();
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}
		if (Input.IsActionJustPressed("Jump"))
		{
			velocity.Y = JumpVelocity;
		}

		var move = Input.GetAxis("Left", "Right");
		if (move != 0)
		{
			velocity.X = move * Speed;
		}
		else if(CanShootHook && IsOnFloor())
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
		}

		// 锚点约束

		if(CurrentAnchor != null)
		{
			var offset = CurrentAnchor.GlobalPosition - GlobalPosition;
			var distance = offset.Length();
			var dir = offset.Normalized();
			var dir2 = dir.Rotated(Mathf.Pi / 2);
			var vel = Mathf.Abs(velocity.Dot(dir2));
			var a = dir * (vel * vel / distance);
			velocity += a * (float)delta;

			var vel2 = velocity.Dot(dir);
			if(vel2 < 0)
			{
				velocity += dir * (-vel2);
			}
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	public override void _Draw()
	{
		if(CanShootHook)
		{
			DrawDashedLine(
			ToLocal(GunEnd.GlobalPosition),
			ToLocal(raycast_target),
			raycast_anchor == null ? LineMissing : LineCaught,
			width: LineWidth,
			dash: LineDashWidth
			);

		}
		else
		{
			DrawLine(
				ToLocal(GunEnd.GlobalPosition),
				ToLocal(CurrentAnchor.GlobalPosition),
				LineJoint,
				width: LineWidth);
		}
	}
}

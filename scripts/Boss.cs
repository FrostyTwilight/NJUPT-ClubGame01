using Godot;
using NJUPTClubGame.scripts;
using System;
using System.Threading;
using System.Threading.Tasks;

public partial class Boss : Area2D
{
	[Export]
	public int BossPhase { get; set; } = 0;
	[Export]
	public float BulletVel { get; set; } = 10;
	[Export]
	public float LaserLineWidth { get; set; } = 3;

	[Export]
	public Color LaserPrepareColor { get; set; } = Colors.GreenYellow;
	[Export]
	public Color LaserReadyColor { get; set; } = Colors.Yellow;
	[Export]
	public Color LaserFireColor { get; set; } = Colors.Red;

	[Export]
	[ExportCategory("Nodes")]
	public GpuParticles2D HitExp { get; set; }
	[Export]
	public Node2D BossPoints { get; set; }

	[Export]
	public Node2D StartPoint { get; set; }

	[Export]
	public Node2D FloorRedArea { get; set; }

	[Export]
	public Node2D BossWin { get; set; }

	[ExportCategory("Prefabs")]
	[Export]
	public PackedScene BossBulletPrefab { get; set; }

	private bool playerEntered = false;

	private bool CanShoot => BossPhase >= 2;
	private bool CanLaser => BossPhase >= 5;
	private Player2 player;
	private CustomFSM fsm;
	private Node2D bulletParents;

	private Vector2? laserTarget;
	private Color laserColor;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		fsm = new();

		fsm.AddGlobalTransition("PLAYER RESPAWN", State_PlayerDie);

		fsm.SwitchToState(State_Init);

		BodyEntered += Boss_BodyEntered;
		BodyExited += Boss_BodyExited;
	}

	private void Boss_BodyExited(Node2D body)
	{
		if (body is Player2)
		{
			playerEntered = false;
		}
	}

	private void SetFloorRedAreaActive(bool active)
	{
		if(active)
		{
			FloorRedArea.Position = Vector2.Zero;
		}
		else
		{
			FloorRedArea.Position = new(99999, 99999);
		}
	}

	private void Boss_BodyEntered(Node2D body)
	{ 
		if(body is Player2 player && !playerEntered)
		{
			this.player = player;
			playerEntered = true;

			fsm.SendEvent("HIT");
		}
	}

	private async Task State_Init(CustomFSM fsm, CancellationToken cancellationToken)
	{
		fsm.AddTransition(CustomFSM.EVENT_FINISHED, State_FirstIdle);

		GlobalPosition = StartPoint.GlobalPosition;
		BossPhase = 0;

		SetFloorRedAreaActive(false);

		laserTarget = null;

		bulletParents?.QueueFree();
		bulletParents = null;
	}

	private async Task State_Win(CustomFSM fsm, CancellationToken cancellationToken)
	{
		fsm.AddTransition("NEXT", State_PlayerDie);

		BossWin.Visible = true;

		CustomFSM.BroadcastEvent("PLAYER RESPAWN");
	}

	private async Task State_NextPhase(CustomFSM fsm, CancellationToken cancellationToken)
	{
		fsm.AddTransition("NEXT X", State_Idle);
		fsm.AddTransition("WIN", State_Win);

		await Task.Delay(TimeSpan.FromSeconds(0.3), cancellationToken);

		GD.Print("Next Phase: " + BossPhase);

		BossPhase++;

		if(BossPhase >= 7)
		{
			SetFloorRedAreaActive(true);
		}
		if(BossPhase > 11 && !BossWin.Visible)
		{
			GD.Print("Win.");
			fsm.SendEvent("WIN");
		}

		fsm.SendEvent("NEXT X");
	}

	private async Task State_Hit(CustomFSM fsm, CancellationToken cancellationToken)
	{
		fsm.AddTransition("NEXT", State_NextPhase);

		// Clean bullets
		bulletParents?.QueueFree();

		bulletParents = new();
		AddSibling(bulletParents);

		// Emit particle
		HitExp.Emitting = false;
		HitExp.Restart();
		HitExp.Emitting = true;

		await Task.Delay(10, cancellationToken);

		// Select next pos

		var oldPos = GlobalPosition;

		while (true)
		{
			var childCount = BossPoints.GetChildCount();
			var nextPos = BossPoints.GetChild<Node2D>(Random.Shared.Next(0, childCount));

			if((nextPos.GlobalPosition - GlobalPosition).LengthSquared() > 1000)
			{
				GlobalPosition = nextPos.GlobalPosition;

				ForceUpdateTransform();
				
				GD.Print("New pos: " + GlobalPosition);
				break;
			}
		}

		await Task.Delay(TimeSpan.FromSeconds(0.3), cancellationToken);

		fsm.SendEvent("NEXT");
	}

	private async Task State_PlayerDie(CustomFSM fsm, CancellationToken cancellationToken)
	{
		fsm.AddTransition("NEXT", State_Init);

		fsm.SendEvent("NEXT");
	}

	private async Task State_Idle(CustomFSM fsm, CancellationToken cancellationToken)
	{
		fsm.AddTransition("HIT", State_Hit);
		fsm.AddTransition("SHOOT", State_Shoot);
		fsm.AddTransition("LASER", State_LaserPrepare);

		laserTarget = null;


		while (true)
		{
			if (CanShoot)
			{
				if (Random.Shared.NextDouble() < 0.45)
				{
					fsm.SendEvent("SHOOT");
				}
			}
			if(CanLaser)
			{
				if (Random.Shared.NextDouble() < 0.35)
				{
					fsm.SendEvent("LASER");
				}
			}
		
			await Task.Delay(1, cancellationToken);
		}
		
	}

	private async Task State_LaserPrepare(CustomFSM fsm, CancellationToken cancellationToken)
	{
		fsm.AddTransition("HIT", State_Hit);
		fsm.AddTransition("LASER READY", State_Laser);

		laserColor = LaserPrepareColor;

		var dir = (player.GlobalPosition - GlobalPosition).Normalized();
		laserTarget = dir * 9999;

		_ = Task.Delay(TimeSpan.FromSeconds(3), cancellationToken)
			.ContinueWith(_ => Callable.From(() => fsm.SendEvent("LASER READY")).CallDeferred(), cancellationToken,
			TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current);

		while (true)
		{
			dir = (player.GlobalPosition - GlobalPosition).Normalized();
			laserTarget = dir * 9999;

			laserColor = LaserPrepareColor;

			await Task.Delay(10, cancellationToken);
		}
	}

	private async Task State_Laser(CustomFSM fsm, CancellationToken cancellationToken)
	{
		fsm.AddTransition("HIT", State_Hit);
		fsm.AddTransition("NEXT", State_Idle);

		laserColor = LaserReadyColor;

		var dir = laserTarget.Value.Normalized();

		await Task.Delay(TimeSpan.FromSeconds(0.3), cancellationToken);

		laserColor = LaserFireColor;

		var bullet = (RigidBody2D)BossBulletPrefab.Instantiate();
		bulletParents.AddChild(bullet);

		bullet.GlobalPosition = GlobalPosition;
		bullet.ApplyImpulse(bullet.Mass * BulletVel * dir * 10);
		bullet.CollisionLayer = 1 << 5;

		await Task.Delay(TimeSpan.FromSeconds(0.3), cancellationToken);

		fsm.SendEvent("NEXT");
	}

	private async Task State_Shoot(CustomFSM fsm, CancellationToken cancellationToken)
	{
		fsm.AddTransition("HIT", State_Hit);
		fsm.AddTransition("NEXT", State_Idle);

		await Task.Delay(TimeSpan.FromSeconds(Random.Shared.NextDouble() * 3 + 1), cancellationToken);

		var dir = (player.GlobalPosition - GlobalPosition).Normalized();

		await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);

		var bullet = (RigidBody2D)BossBulletPrefab.Instantiate();
		bulletParents.AddChild(bullet);

		bullet.GlobalPosition = GlobalPosition;
		bullet.ApplyImpulse(bullet.Mass * BulletVel * dir);

		await Task.Delay(TimeSpan.FromSeconds(0.2), cancellationToken);

		bullet.CollisionLayer = 1 << 5;

		fsm.SendEvent("NEXT");
	}


	private async Task State_FirstIdle(CustomFSM fsm, CancellationToken cancellationToken)
	{
		fsm.AddTransition("HIT", State_FirstHit);
	}

	private async Task State_FirstHit(CustomFSM fsm, CancellationToken cancellationToken)
	{
		fsm.AddTransition(CustomFSM.EVENT_FINISHED, State_Hit);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		fsm.Update(delta);

		QueueRedraw();
	}

	public override void _Notification(int what)
	{
		fsm?.Notification(what);
	}

	public override void _Draw()
	{
		if(laserTarget != null)
		{
			var target = laserTarget.Value;
			DrawLine(Vector2.Zero, target, laserColor, LaserLineWidth);
		}
	}
}

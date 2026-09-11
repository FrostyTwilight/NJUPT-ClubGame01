using Godot;
using System;
using System.Threading.Tasks;

public partial class Gun : Node2D
{
	[Export]
	public float HookVel { get; set; }
	[Export]
	public PlayerController Player { get; set; }
	[Export]
	public Node2D Hook { get; set; }
	[Export]
	public RayCast2D RayCast2D { get; set; }
	public bool CanFire => !Hook.Visible;

	public void HideHook()
	{
		Hook.Visible = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.

	public bool Fire(out GodotObject collider)
	{
		RayCast2D.ForceRaycastUpdate();

		collider = null;

		if (!RayCast2D.IsColliding())
		{
			return false;
		}

		GD.Print("Colliding!");

		Hook.GlobalPosition = RayCast2D.GetCollisionPoint();
		collider = RayCast2D.GetCollider();
		Hook.Rotation = Rotation + 3.14f / 2;
		Hook.Visible = true;
		Player.CanMove = false;
		return true;
	}

	public override void _Process(double delta)
	{
		//跟随鼠标/hooker

		Vector2 targetPos;
		if(!CanFire)
		{
			targetPos = Hook.GlobalPosition;
		}
		else
		{
			var mouseScreenPos = GetViewport().GetMousePosition();
			targetPos = GetCanvasTransform().AffineInverse() * mouseScreenPos;
		}
		var offset = targetPos - GlobalPosition;

		var t = Mathf.Atan2(offset.Y, offset.X);
		Rotation = t;
	}
}

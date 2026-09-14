using Godot;
using System;

public partial class RespawnPoint : Area2D
{
	[Export]
	public Node2D Point { get; set; }

	public override void _Ready()
	{
		BodyEntered += RespawnPoint_BodyEntered;
	}

	public Vector2 GetRespawnPosition()
	{
		return Point.GlobalPosition;
	}

	public void OnRespawn(Player2 player)
	{

	}

	private void RespawnPoint_BodyEntered(Node2D body)
	{
		if(body is Player2 player)
		{
			GD.Print("Set respawn point");
			player.SetRespawnPoint(this);
		}
	}
}

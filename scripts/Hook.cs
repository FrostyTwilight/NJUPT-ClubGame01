using Godot;
using System;

public partial class Hook : Node2D
{
	[Export]
	public Line2D Line { get; set; }
	[Export]
	public Gun Gun { get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(Line != null && Visible)
		{
			if(Gun != null)
			{
				Line.ClearPoints();
				Line.AddPoint(Vector2.Zero);
				Line.AddPoint(Line.ToLocal(Gun.GlobalPosition));
			}
		}
	}
}

using Godot;
using OverEasy;

public partial class SetDataTreeCollision : CollisionShape2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		OverEasyGlobals.setDataTreeCollision = this;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

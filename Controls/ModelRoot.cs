using Godot;
using OverEasy;

public partial class ModelRoot : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		OverEasyGlobals.modelRoot = this;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

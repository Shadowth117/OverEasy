using Godot;
using OverEasy;

public partial class ObjectScrollContainerButton : Button
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		OverEasyGlobals.objectScrollContainerButton = this;
		this.ButtonUp += OverEasyGlobals.OnObjectScrollContainerButtonReleased;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

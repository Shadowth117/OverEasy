using Godot;
using OverEasy;

public partial class ObjectScrollContainer : ScrollContainer
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		OverEasyGlobals.objectScrollContainer = this;

		//Force it off at the start
        OverEasyGlobals.OnObjectScrollContainerButtonReleased();
        OverEasyGlobals.OnObjectScrollContainerButtonReleased();
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
    }
}

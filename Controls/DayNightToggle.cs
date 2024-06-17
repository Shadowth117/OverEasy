using Godot;
using OverEasy;

public partial class DayNightToggle : CheckButton
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		OverEasyGlobals.dayNightToggle = this;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override void _Pressed()
    {
		OverEasyGlobals.DayNightToggle();
    }
}

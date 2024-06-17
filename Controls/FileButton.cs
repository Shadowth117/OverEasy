using Godot;
using OverEasy;

public partial class FileButton : MenuButton
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetPopup().Connect("id_pressed", new Callable(this, MethodName._onFileButtonMenuSelectionLocal));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	public void _onFileButtonMenuSelectionLocal(long id)
	{
		OverEasyGlobals.OnFileButtonMenuSelection(id);
	}
}

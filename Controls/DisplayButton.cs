using Godot;
using OverEasy;

public partial class DisplayButton : MenuButton
{
    public override void _Ready()
    {
        OverEasyGlobals.DisplayBtn = this;
        GetPopup().Connect("id_pressed", new Callable(this, MethodName._onDisplayButtonMenuSelectionLocal));
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
    public void _onDisplayButtonMenuSelectionLocal(long id)
    {
        OverEasyGlobals.OnDisplayButtonMenuSelection((int)id);
    }
}

using Godot;
using OverEasy;

public partial class EditButton : MenuButton
{
    public override void _Ready()
    {
        GetPopup().Connect("id_pressed", new Callable(this, MethodName._onEditButtonMenuSelectionLocal));
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
    public void _onEditButtonMenuSelectionLocal(long id)
    {
        OverEasyGlobals.OnEditButtonMenuSelection(id);
    }
}

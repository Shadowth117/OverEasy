using Godot;
using OverEasy;

public partial class EditButton : MenuButton
{
    public override void _Ready()
    {
        GetPopup().Connect("id_pressed", new Callable(this, MethodName._onEditButtonMenuSelectionLocal));
        OverEasyGlobals.editBtn = this;
    }

    public override void _Pressed()
    {
        OverEasyGlobals.GetCurrentEditMenu();
        base._Pressed();
    }

    public void _onEditButtonMenuSelectionLocal(long id)
    {
        OverEasyGlobals.OnEditButtonMenuSelection(id);
    }
}

using Godot;
using OverEasy;

public partial class SettingsButton : MenuButton
{
    public override void _Ready()
    {
        OverEasyGlobals.SettingsBtn = this;
        GetPopup().Connect("id_pressed", new Callable(this, MethodName._onSettingsButtonMenuSelectionLocal));
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
    public void _onSettingsButtonMenuSelectionLocal(long id)
    {
        OverEasyGlobals.OnSettingsButtonMenuSelection((int)id);
    }
}

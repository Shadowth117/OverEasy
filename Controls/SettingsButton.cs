using Godot;
using OverEasy;

public partial class SettingsButton : MenuButton
{
    //Billy Hatcher
    public bool worldTransformToggle = false;
    public bool warpCameraToSelected = true;

    public override void _Ready()
    {
        OverEasyGlobals.settingsBtn = this;
        GetPopup().Connect("id_pressed", new Callable(this, MethodName._onSettingsButtonMenuSelectionLocal));
    }
    public override void _Pressed()
    {
        OverEasyGlobals.GetCurrentSettingsMenu();
        base._Pressed();
    }

    public void _onSettingsButtonMenuSelectionLocal(long id)
    {
        OverEasyGlobals.OnSettingsButtonMenuSelection((int)id);
    }
}

using Godot;
using OverEasy;

public partial class DisplayButton : MenuButton
{
    //Billy Hatcher
    public bool dayNightPreviewToggle = true;
    public bool displayLndTerrain = true;
    public bool displayMc2Terrain = false;

    public override void _Ready()
    {
        OverEasyGlobals.displayBtn = this;
        GetPopup().Connect("id_pressed", new Callable(this, MethodName._onDisplayButtonMenuSelectionLocal));
    }
    public override void _Pressed()
    {
        OverEasyGlobals.GetCurrentDisplayMenu();
        base._Pressed();
    }

    public void _onDisplayButtonMenuSelectionLocal(long id)
    {
        OverEasyGlobals.OnDisplayButtonMenuSelection((int)id);
    }
}

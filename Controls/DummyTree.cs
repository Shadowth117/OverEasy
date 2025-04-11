using Godot;

namespace OverEasy.Controls
{
    public partial class DummyTree : Tree
    {
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            this.Visible = false;
            this.Modulate = OverEasyGlobals.mainFillColorInactive;
            OverEasyGlobals.dummyTree = this;
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
        }
    }
}

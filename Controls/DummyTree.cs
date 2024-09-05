using Godot;

namespace OverEasy.Controls
{
    public partial class DummyTree : Tree
    {
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            this.Visible = false;
            this.Modulate = new Color(0.7f, 0.7f, 0.7f, 0.4f);
            OverEasyGlobals.dummyTree = this;
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
        }
    }
}

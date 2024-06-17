using Godot;
using OverEasy;

public partial class GUIArea : Area2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
        OverEasyGlobals.setDataTree.Modulate = new Color(0.7f, 0.7f, 0.7f, 0.4f);
        OverEasyGlobals.objectScrollContainer.Modulate = new Color(0.7f, 0.7f, 0.7f, 0.4f);
        
        bool inside = false;
		var mousePos = GetGlobalMousePosition();
        foreach (var child in GetChildren())
		{
			if(child is CollisionShape2D)
            {
                var coll = (CollisionShape2D)child;
                var shape = (RectangleShape2D)coll.Shape;
                var size = shape.Size;
                var pos = coll.GlobalPosition;

                if (mousePos.X < pos.X + size.X / 2 && mousePos.X > pos.X - size.X / 2
                    && mousePos.Y < pos.Y + size.Y / 2 && mousePos.Y > pos.Y - size.Y / 2)
                {
                    if(child == OverEasyGlobals.setDataTreeCollision)
                    {
                        OverEasyGlobals.setDataTree.Modulate = new Color(0.7f, 0.7f, 0.7f, 1);
                    } else if (child == OverEasyGlobals.objectPanelCollision)
                    {
                        OverEasyGlobals.objectScrollContainer.Modulate = new Color(0.7f, 0.7f, 0.7f, 1);
                    }
                    inside = true;
                    break;
                }
            }
		}
        
        OverEasyGlobals.mouseInGuiArea = inside;
    }
}

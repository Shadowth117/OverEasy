using Godot;
using OverEasy;

public partial class ObjectPanelCollision : CollisionShape2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		OverEasyGlobals.objectPanelCollision = this;
		RectangleShape2D shape = (RectangleShape2D)this.Shape;
		shape.Size = new Vector2(shape.Size.X, 0);
		this.Shape = shape;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

using Godot;
using OverEasy;
using System.Linq;

public partial class SetDataTree : Tree
{
	public bool shouldClear = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.Modulate = new Color(0.7f, 0.7f, 0.7f, 0.3f);
		OverEasyGlobals.setDataTree = this;
		this.MouseEntered += OnMouseEntered;
		this.MouseExited += OnMouseExited;
		this.Columns = 3;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
    }

	public void OnMouseEntered()
	{
		this.Modulate = new Color(0.7f, 0.7f, 0.7f, 1);
	}
	public void OnMouseExited()
	{
		this.Modulate = new Color(0.7f, 0.7f, 0.7f, 0.3f);
    }
}

using Godot;
using OverEasy;
using System.Linq;

public partial class SetDataTree : Tree
{
	public bool shouldClear = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.Modulate = new Color(0.7f, 0.7f, 0.7f, 0.4f);
		OverEasyGlobals.setDataTree = this;
		this.Columns = 4;
		this.SetColumnExpand(0, true);
		this.SetColumnExpand(1, false);
		this.SetColumnExpand(2, false);
		this.SetColumnExpand(3, false);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

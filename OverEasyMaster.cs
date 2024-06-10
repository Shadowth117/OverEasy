using AquaModelLibrary.Data.Ninja.Model;
using Godot;
using OverEasy;

public partial class OverEasyMaster : Control
{
	public bool attached = false;
	[Export]
	public VBoxContainer ColorSchemaTemplate = null;
	[Export]
	public VBoxContainer FloatSchemaTemplate = null;
	[Export]
	public VBoxContainer IntSchemaTemplate = null;
	[Export]
	public VBoxContainer LabelSchemaTemplate = null;
	[Export]
	public VBoxContainer StringSchemaTemplate = null;
	[Export]
	public VBoxContainer Vector2SchemaTemplate = null;
	[Export]
	public VBoxContainer Vector3SchemaTemplate = null;
	[Export]
	public VBoxContainer Vector4SchemaTemplate = null;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		OverEasyGlobals.StartUp();
		OverEasyGlobals.OESceneTree = GetTree();
		OverEasyGlobals.OEMainViewPort = GetViewport();

		OverEasyGlobals.OESceneTree.Root.SizeChanged += OverEasyGlobals.OnWindowSizeChanged;
		
		OverEasyGlobals.ColorSchemaTemplate = this.ColorSchemaTemplate;
		OverEasyGlobals.FloatSchemaTemplate = this.FloatSchemaTemplate;
		OverEasyGlobals.IntSchemaTemplate = this.IntSchemaTemplate;
		OverEasyGlobals.LabelSchemaTemplate = this.LabelSchemaTemplate;
		OverEasyGlobals.StringSchemaTemplate = this.StringSchemaTemplate;
		OverEasyGlobals.Vector2SchemaTemplate = this.Vector2SchemaTemplate;
		OverEasyGlobals.Vector3SchemaTemplate = this.Vector3SchemaTemplate;
		OverEasyGlobals.Vector4SchemaTemplate = this.Vector4SchemaTemplate;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(OverEasyGlobals.OESceneTree != null && attached == false)
		{
			attached = true;
			OverEasyGlobals.AttachDialogs();
		}
	}
}

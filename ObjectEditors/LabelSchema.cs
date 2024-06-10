using Godot;
using System;

public partial class LabelSchema : VBoxContainer
{
	public RichTextLabel text = null;
	public string icon = "[img]res://addons/datatable_godot/icons/String.svg[/img]";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		text = (RichTextLabel)GetChild(0);
	}

	public string GetTitle()
	{
		return text.Text;
	}

	public void SetTitle(string newText)
	{
		text.Text = newText;
	}
}

using Godot;
using OverEasy;
using System;

public partial class ColorSchema : VBoxContainer
{
	public RichTextLabel text = null;
	public ColorPickerButton input = null;
	public string icon = "[img]res://addons/datatable_godot/icons/Color.svg[/img]";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		text = (RichTextLabel)GetChild(0);
		input = (ColorPickerButton)GetChild(1);
		input.ColorChanged += this.ColorChangedEvent;
	}

	public void ColorChangedEvent(Color color)
	{
		OverEasyGlobals.UpdateData();
		return;
	}

	public string GetTitle()
	{
		return text.Text;
	}

	public void SetTitle(string newText)
	{
		text.Text = newText;
	}

	public Color GetValue()
	{
		return input.Color;
	}

	public void SetValue(Variant newValue)
	{

		if(newValue.VariantType == Variant.Type.String)
		{
			string convert = newValue.AsString();
			if(convert.StartsWith("C/"))
			{
				convert = convert.Replace("C/", "");
			} else
			{
				return;
			}

			var converter = convert.Split(",");
			newValue = new Color(Single.Parse(converter[0]), Single.Parse(converter[1]), Single.Parse(converter[2]), Single.Parse(converter[3]));
		}
		if(newValue.VariantType == Variant.Type.Color)
		{
			input.Color = newValue.AsColor();
			return;
		}
		input.Color = new Color(0, 0, 0, 0);
	}
}


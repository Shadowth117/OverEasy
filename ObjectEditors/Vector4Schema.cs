using Godot;
using OverEasy;
using System;

public partial class Vector4Schema : VBoxContainer
{
	public RichTextLabel text = null;
	public SpinBox xInput = null;
	public SpinBox yInput = null;
	public SpinBox zInput = null;
	public SpinBox wInput = null;
	public string icon = "";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		text = (RichTextLabel)GetChild(0);
		xInput = (SpinBox)GetChild(1).GetChild(0).GetChild(1);
		yInput = (SpinBox)GetChild(1).GetChild(1).GetChild(1);
		zInput = (SpinBox)GetChild(1).GetChild(2).GetChild(1);
		wInput = (SpinBox)GetChild(1).GetChild(3).GetChild(1);

		xInput.ValueChanged += ValueChanged;
		yInput.ValueChanged += ValueChanged;
		zInput.ValueChanged += ValueChanged;
		wInput.ValueChanged += ValueChanged;
    }

    public void ValueChanged(double value)
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

	public Vector4 GetValue()
	{
		return new Vector4((float)xInput.Value, (float)yInput.Value, (float)zInput.Value, (float)wInput.Value);
	}

	public void SetValue(Variant newValue)
	{
		if (newValue.VariantType == Variant.Type.String)
		{
			var str = newValue.AsString().Replace("(", "").Replace(")", "");
			var splitStr = str.Split(',');
			if(splitStr.Length == 4)
			{
				newValue = new Vector4(Single.Parse(splitStr[0]), Single.Parse(splitStr[1]), Single.Parse(splitStr[2]), Single.Parse(splitStr[3]));
			}
		}
		if (newValue.VariantType == Variant.Type.Vector4)
		{
			var vec3 = newValue.AsVector4();
			xInput.SetValueNoSignal((Math.Round(vec3.X * 10000))/10000);
			yInput.SetValueNoSignal((Math.Round(vec3.Y * 10000))/10000);
			zInput.SetValueNoSignal((Math.Round(vec3.Z * 10000))/10000);
			wInput.SetValueNoSignal((Math.Round(vec3.W * 10000))/10000);
			return;
		}

		xInput.Value = 0;
		yInput.Value = 0;
		zInput.Value = 0;
		wInput.Value = 0;
	}
}

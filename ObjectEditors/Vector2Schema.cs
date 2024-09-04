using Godot;
using OverEasy;
using System;

public partial class Vector2Schema : VBoxContainer
{
    public RichTextLabel text = null;
    public SpinBox xInput = null;
    public SpinBox yInput = null;
    public string icon = "";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        text = (RichTextLabel)GetChild(0);
        xInput = (SpinBox)GetChild(1).GetChild(0).GetChild(1);
        yInput = (SpinBox)GetChild(1).GetChild(1).GetChild(1);

        xInput.ValueChanged += ValueChanged;
        yInput.ValueChanged += ValueChanged;
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

    public Vector2 GetValue()
    {
        return new Vector2((float)xInput.Value, (float)yInput.Value);
    }

    public void SetValue(Variant newValue)
    {
        if (newValue.VariantType == Variant.Type.String)
        {
            var str = newValue.AsString().Replace("(", "").Replace(")", "");
            var splitStr = str.Split(',');
            if(splitStr.Length == 2)
            {
                newValue = new Vector2(Single.Parse(splitStr[0]), Single.Parse(splitStr[1]));
            }
        }
        if (newValue.VariantType == Variant.Type.Vector2)
        {
            var vec3 = newValue.AsVector2();
            xInput.SetValueNoSignal((Math.Round(vec3.X * 10000))/10000);
            yInput.SetValueNoSignal((Math.Round(vec3.Y * 10000))/10000);
            return;
        }

        xInput.Value = 0;
        yInput.Value = 0;
    }
}

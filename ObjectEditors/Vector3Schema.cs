using Godot;
using OverEasy;
using System;

public partial class Vector3Schema : VBoxContainer
{
    public RichTextLabel text = null;
    public SpinBox xInput = null;
    public SpinBox yInput = null;
    public SpinBox zInput = null;
    public string icon = "";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        text = (RichTextLabel)GetChild(0);
        xInput = (SpinBox)GetChild(1).GetChild(0).GetChild(1);
        yInput = (SpinBox)GetChild(1).GetChild(1).GetChild(1);
        zInput = (SpinBox)GetChild(1).GetChild(2).GetChild(1);

        xInput.ValueChanged += ValueChanged;
        yInput.ValueChanged += ValueChanged;
        zInput.ValueChanged += ValueChanged;
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

    public Vector3 GetValue()
    {
        return new Vector3((float)xInput.Value, (float)yInput.Value, (float)zInput.Value);
    }

    public void SetValue(Variant newValue)
    {
        if (newValue.VariantType == Variant.Type.String)
        {
            var str = newValue.AsString().Replace("(", "").Replace(")", "");
            var splitStr = str.Split(',');
            if(splitStr.Length == 3)
            {
                newValue = new Vector3(Single.Parse(splitStr[0]), Single.Parse(splitStr[1]), Single.Parse(splitStr[2]));
            }
        }
        if (newValue.VariantType == Variant.Type.Vector3)
        {
            var vec3 = newValue.AsVector3();
            xInput.SetValueNoSignal((Math.Round(vec3.X * 10000))/10000);
            yInput.SetValueNoSignal((Math.Round(vec3.Y * 10000))/10000);
            zInput.SetValueNoSignal((Math.Round(vec3.Z * 10000))/10000);
            return;
        }

        xInput.Value = 0;
        yInput.Value = 0;
        zInput.Value = 0;
    }
}

using Godot;
using OverEasy;
using System;

public partial class IntSchema : VBoxContainer
{
    public RichTextLabel text = null;
    public SpinBox input = null;
    public string icon = "";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        text = (RichTextLabel)GetChild(0);
        input = (SpinBox)GetChild(1);
        input.ValueChanged += this.ValueChanged;
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

    public long GetValue()
    {
        return (long)input.Value;
    }

    public void SetValue(Variant newValue)
    {
        int min = GetMeta("Min").AsInt32();
        int max = GetMeta("Max").AsInt32();
        long? value = null;
        if (newValue.VariantType == Variant.Type.Int || newValue.VariantType == Variant.Type.Float)
        {
            value = newValue.AsInt32();
            return;
        }
        else if (newValue.VariantType == Variant.Type.String)
        {
            value = Int64.Parse(newValue.AsString());
            return;
        }

        if (value >= min && value <= max && value != null)
        {
            input.SetValueNoSignal((double)value);
        }

        input.SetValueNoSignal(0);
    }
}

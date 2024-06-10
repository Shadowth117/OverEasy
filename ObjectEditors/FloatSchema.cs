using Godot;
using OverEasy;
using System;

public partial class FloatSchema : VBoxContainer
{
    public RichTextLabel text = null;
    public SpinBox input = null;
    public string icon = "[img]res://addons/datatable_godot/icons/float.svg[/img]";

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

    public float GetValue()
    {
        return (float)input.Value;
    }

    public void SetValue(Variant newValue)
    {
        if (newValue.VariantType == Variant.Type.Float)
        {
            input.SetValueNoSignal(newValue.AsSingle());
            return;
        }
        if (newValue.VariantType == Variant.Type.String)
        {
            input.SetValueNoSignal(Single.Parse(newValue.AsString()));
            return;
        }

        input.SetValueNoSignal(0); 
    }
}

using Godot;
using OverEasy;

public partial class StringSchema : VBoxContainer
{
	public RichTextLabel text = null;
	public LineEdit input = null;
	public string icon = "";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		text = (RichTextLabel)GetChild(0);
		input = (LineEdit)GetChild(1);
		input.TextChanged += TextChanged;
    }

    public void TextChanged(string value)
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

	public string GetValue()
	{
		return input.Text;
	}

	public void SetValue(Variant newText)
	{
		input.Text = (string)newText;
	}
}

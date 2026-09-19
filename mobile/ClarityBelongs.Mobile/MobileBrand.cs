namespace ClarityBelongs.Mobile;

public static class MobileBrand
{
    public static readonly Color Ink = Color.FromArgb("#17202A");
    public static readonly Color Muted = Color.FromArgb("#66717D");
    public static readonly Color Teal = Color.FromArgb("#287A72");
    public static readonly Color Paper = Color.FromArgb("#FFFFFF");
    public static readonly Color Background = Color.FromArgb("#F4F7F7");
    public static readonly Color Line = Color.FromArgb("#DFE7E6");
    public static readonly Color TealTint = Color.FromArgb("#E8F5F1");

    public static Label Eyebrow(string text) => new()
    {
        Text = text.ToUpperInvariant(),
        TextColor = Teal,
        FontAttributes = FontAttributes.Bold,
        FontSize = 12,
        CharacterSpacing = 1
    };

    public static Label Title(string text, double size = 30) => new()
    {
        Text = text,
        TextColor = Ink,
        FontAttributes = FontAttributes.Bold,
        FontSize = size
    };

    public static Label Body(string text, double size = 14) => new()
    {
        Text = text,
        TextColor = Muted,
        FontSize = size,
        LineHeight = 1.3
    };

    public static Border Card(View content, double padding = 16) => new()
    {
        Padding = padding,
        Stroke = Line,
        StrokeThickness = 1,
        BackgroundColor = Paper,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
        {
            CornerRadius = 16
        },
        Content = content
    };

    public static void Primary(Button button)
    {
        button.BackgroundColor = Color.FromArgb("#1F6F68");
        button.TextColor = Colors.White;
        button.CornerRadius = 12;
        button.Padding = new Thickness(16, 12);
    }

    public static void Secondary(Button button)
    {
        button.BackgroundColor = Paper;
        button.TextColor = Ink;
        button.BorderColor = Line;
        button.BorderWidth = 1;
        button.CornerRadius = 12;
        button.Padding = new Thickness(16, 12);
    }
}

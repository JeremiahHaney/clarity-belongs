namespace ClarityBelongs.Mobile;

public sealed class MainPage : ContentPage
{
    private static readonly Color Ink = Color.FromArgb("#17202A");
    private static readonly Color Muted = Color.FromArgb("#66717D");
    private static readonly Color Teal = Color.FromArgb("#287A72");
    private static readonly Color Paper = Color.FromArgb("#FFFFFF");
    private static readonly Color Cream = Color.FromArgb("#F7FAF9");
    private static readonly Color Line = Color.FromArgb("#D8E7E4");

    public MainPage()
    {
        Title = "Clarity Belongs";
        BackgroundColor = Cream;

        var content = new VerticalStackLayout
        {
            Spacing = 18,
            Padding = new Thickness(20, 28, 20, 40),
            Children =
            {
                new Label
                {
                    Text = "CLARITY BELONGS",
                    TextColor = Teal,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 13
                },
                new Label
                {
                    Text = "Know what changed.\nKnow what matters.",
                    TextColor = Ink,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 34
                },
                new Label
                {
                    Text = "Your watches, alerts, history, and evidence in one calm place.",
                    TextColor = Muted,
                    FontSize = 16
                },
                CreateStatusCard(),
                CreateSectionTitle("YOUR CLARITY"),
                CreateNavigationCard("My Clarity", "Overview of everything you are watching", "●"),
                CreateNavigationCard("Watches", "Website, domain, DNS, certificate, and endpoint watches", "◉"),
                CreateNavigationCard("Alerts", "See failures, recoveries, expirations, and changes", "!"),
                CreateNavigationCard("History", "Review observations and before-and-after evidence", "↻"),
                CreateNavigationCard("Account", "Membership, profile, and app settings", "○")
            }
        };

        Content = new ScrollView
        {
            Content = content
        };
    }

    private static View CreateStatusCard()
    {
        return new Border
        {
            Padding = 18,
            Stroke = Line,
            StrokeThickness = 1,
            BackgroundColor = Paper,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = 18
            },
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label
                    {
                        Text = "Mobile companion",
                        TextColor = Ink,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 18
                    },
                    new Label
                    {
                        Text = "The Android shell is ready. Next we connect it to your existing Clarity account and live watch data.",
                        TextColor = Muted,
                        FontSize = 14
                    }
                }
            }
        };
    }

    private static Label CreateSectionTitle(string text) =>
        new()
        {
            Text = text,
            TextColor = Teal,
            FontAttributes = FontAttributes.Bold,
            FontSize = 12,
            Margin = new Thickness(0, 8, 0, 0)
        };

    private static View CreateNavigationCard(
        string title,
        string description,
        string icon)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };

        grid.Add(
            new Border
            {
                WidthRequest = 44,
                HeightRequest = 44,
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#E8F5F1"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 12
                },
                Content = new Label
                {
                    Text = icon,
                    TextColor = Teal,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 19,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                }
            },
            0,
            0);

        grid.Add(
            new VerticalStackLayout
            {
                Spacing = 3,
                Children =
                {
                    new Label
                    {
                        Text = title,
                        TextColor = Ink,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 17
                    },
                    new Label
                    {
                        Text = description,
                        TextColor = Muted,
                        FontSize = 13,
                        MaxLines = 2
                    }
                }
            },
            1,
            0);

        grid.Add(
            new Label
            {
                Text = "›",
                TextColor = Teal,
                FontSize = 28,
                VerticalTextAlignment = TextAlignment.Center
            },
            2,
            0);

        return new Border
        {
            Padding = 15,
            Stroke = Line,
            StrokeThickness = 1,
            BackgroundColor = Paper,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = 16
            },
            Content = grid
        };
    }
}

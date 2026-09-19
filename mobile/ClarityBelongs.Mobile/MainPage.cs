namespace ClarityBelongs.Mobile;

public sealed class MainPage : ContentPage
{
    private static readonly Color Ink = Color.FromArgb("#17202A");
    private static readonly Color Muted = Color.FromArgb("#66717D");
    private static readonly Color Teal = Color.FromArgb("#287A72");
    private static readonly Color Paper = Color.FromArgb("#FFFFFF");
    private static readonly Color Background = Color.FromArgb("#F4F7F7");
    private static readonly Color Line = Color.FromArgb("#DFE7E6");
    private static readonly Color TealTint = Color.FromArgb("#E8F5F1");
    private static readonly Color Blue = Color.FromArgb("#4F87C8");
    private static readonly Color GoldTint = Color.FromArgb("#FFF5D8");

    public MainPage()
    {
        Title = "Clarity Belongs";
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Background;

        var content = new VerticalStackLayout
        {
            Spacing = 18,
            Padding = new Thickness(18, 22, 18, 40),
            MaximumWidthRequest = 720,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                CreateBrandHeader(),
                CreateHero(),
                CreateWatchingCard(),
                CreateSectionTitle("MY CLARITY"),
                CreateNavigationCard(
                    "My Clarity",
                    "Overview of everything you are watching",
                    "clarity_logo.svg",
                    TealTint),
                CreateNavigationCard(
                    "Watches",
                    "Website, domain, DNS, certificate, and endpoint watches",
                    "icon_watches.svg",
                    Color.FromArgb("#EAF1FB")),
                CreateNavigationCard(
                    "Alerts",
                    "Failures, recoveries, expirations, and recorded changes",
                    "icon_alerts.svg",
                    GoldTint),
                CreateNavigationCard(
                    "History",
                    "Observations and before-and-after evidence",
                    "icon_history.svg",
                    Color.FromArgb("#F1EAFA")),
                CreateNavigationCard(
                    "Account",
                    "Membership, profile, and app settings",
                    "icon_account.svg",
                    Color.FromArgb("#EDF2F4"))
            }
        };

        Content = new ScrollView
        {
            BackgroundColor = Background,
            Content = content
        };
    }

    private static View CreateBrandHeader()
    {
        return new HorizontalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Image
                {
                    Source = "clarity_logo.svg",
                    WidthRequest = 38,
                    HeightRequest = 38,
                    VerticalOptions = LayoutOptions.Center
                },
                new VerticalStackLayout
                {
                    Spacing = 1,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label
                        {
                            Text = "Clarity Belongs",
                            TextColor = Ink,
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold
                        },
                        new Label
                        {
                            Text = "KEEP AN EYE ON WHAT MATTERS",
                            TextColor = Muted,
                            FontSize = 9,
                            CharacterSpacing = 1.1
                        }
                    }
                }
            }
        };
    }

    private static View CreateHero()
    {
        return new Border
        {
            Padding = new Thickness(20, 24),
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#F4FBF9"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = 24
            },
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Label
                    {
                        Text = "CLARITY BELONGS",
                        TextColor = Teal,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 12,
                        CharacterSpacing = 1.0
                    },
                    new Label
                    {
                        Text = "Tell Clarity what matters.\nWe keep an eye on it.",
                        TextColor = Ink,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 32,
                        LineHeight = 1.05
                    },
                    new Label
                    {
                        Text = "Your watches, status, saved changes, alerts, and history in one calm place.",
                        TextColor = Muted,
                        FontSize = 15,
                        LineHeight = 1.35
                    }
                }
            }
        };
    }

    private static View CreateWatchingCard()
    {
        var topline = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 9
        };

        topline.Add(
            new Border
            {
                WidthRequest = 10,
                HeightRequest = 10,
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#43A27A"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 5
                },
                VerticalOptions = LayoutOptions.Center
            },
            0,
            0);

        topline.Add(
            new Label
            {
                Text = "Clarity is watching",
                TextColor = Color.FromArgb("#52606C"),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                VerticalTextAlignment = TextAlignment.Center
            },
            1,
            0);

        topline.Add(
            new Border
            {
                Padding = new Thickness(9, 5),
                StrokeThickness = 0,
                BackgroundColor = TealTint,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 999
                },
                Content = new Label
                {
                    Text = "Mobile",
                    TextColor = Teal,
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold
                }
            },
            2,
            0);

        return new Border
        {
            Padding = 18,
            Stroke = Color.FromArgb("#D5E5E2"),
            StrokeThickness = 1,
            BackgroundColor = Paper,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = 22
            },
            Shadow = new Shadow
            {
                Opacity = 0.08f,
                Radius = 22,
                Offset = new Point(0, 8)
            },
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    topline,
                    CreateWatchStatusRow(
                        "clarity_logo.svg",
                        "My Clarity",
                        "Live account summary will appear here",
                        "Ready",
                        TealTint,
                        Teal),
                    CreateWatchStatusRow(
                        "icon_alerts.svg",
                        "Recent alerts",
                        "Failures, recovery, expiration, and changes",
                        "Next",
                        Color.FromArgb("#EAF1FB"),
                        Blue)
                }
            }
        };
    }

    private static View CreateWatchStatusRow(
        string icon,
        string title,
        string description,
        string status,
        Color statusBackground,
        Color statusText)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 11
        };

        grid.Add(
            new Border
            {
                WidthRequest = 38,
                HeightRequest = 38,
                StrokeThickness = 0,
                BackgroundColor = TealTint,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 12
                },
                Content = new Image
                {
                    Source = icon,
                    WidthRequest = 21,
                    HeightRequest = 21
                }
            },
            0,
            0);

        grid.Add(
            new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label
                    {
                        Text = title,
                        TextColor = Ink,
                        FontSize = 14,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = description,
                        TextColor = Color.FromArgb("#76828D"),
                        FontSize = 12,
                        MaxLines = 2
                    }
                }
            },
            1,
            0);

        grid.Add(
            new Border
            {
                Padding = new Thickness(8, 5),
                StrokeThickness = 0,
                BackgroundColor = statusBackground,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 999
                },
                Content = new Label
                {
                    Text = status,
                    TextColor = statusText,
                    FontSize = 10,
                    FontAttributes = FontAttributes.Bold
                }
            },
            2,
            0);

        return new Border
        {
            Padding = 13,
            Stroke = Color.FromArgb("#E6ECEF"),
            StrokeThickness = 1,
            BackgroundColor = Paper,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = 15
            },
            Content = grid
        };
    }

    private static Label CreateSectionTitle(string text) =>
        new()
        {
            Text = text,
            TextColor = Teal,
            FontAttributes = FontAttributes.Bold,
            FontSize = 12,
            CharacterSpacing = 1.0,
            Margin = new Thickness(0, 8, 0, 0)
        };

    private static View CreateNavigationCard(
        string title,
        string description,
        string icon,
        Color iconBackground)
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
                WidthRequest = 46,
                HeightRequest = 46,
                StrokeThickness = 0,
                BackgroundColor = iconBackground,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 13
                },
                Content = new Image
                {
                    Source = icon,
                    WidthRequest = 24,
                    HeightRequest = 24,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            },
            0,
            0);

        grid.Add(
            new VerticalStackLayout
            {
                Spacing = 3,
                VerticalOptions = LayoutOptions.Center,
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
                        MaxLines = 2,
                        LineBreakMode = LineBreakMode.TailTruncation
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
                FontSize = 27,
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

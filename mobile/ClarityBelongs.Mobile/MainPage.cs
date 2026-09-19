namespace ClarityBelongs.Mobile;

public sealed class MainPage : ContentPage
{
    private readonly MobileApiClient _api;
    private readonly Label _status = MobileBrand.Body("Loading My Clarity…", 13);
    private readonly VerticalStackLayout _stats = new() { Spacing = 10 };
    private readonly VerticalStackLayout _recent = new() { Spacing = 10 };

    public MainPage(MobileApiClient api)
    {
        _api = api;
        Title = "My Clarity";
        BackgroundColor = MobileBrand.Background;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 18,
                Padding = new Thickness(18, 22, 18, 40),
                MaximumWidthRequest = 760,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    CreateBrandHeader(),
                    MobileBrand.Eyebrow("My Clarity"),
                    MobileBrand.Title("Tell Clarity what matters.\nWe keep an eye on it.", 31),
                    MobileBrand.Body(
                        "Your live watches, attention items, alerts, and recorded changes.",
                        15),
                    _status,
                    _stats,
                    MobileBrand.Eyebrow("Recent changes"),
                    _recent
                }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var dashboard = await _api.GetDashboardAsync();
            _stats.Clear();
            _recent.Clear();

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                },
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto)
                },
                ColumnSpacing = 10,
                RowSpacing = 10
            };

            grid.Add(StatCard(dashboard.FollowingCount, "Watching"), 0, 0);
            grid.Add(StatCard(dashboard.NeedsAttentionCount, "Needs attention"), 1, 0);
            grid.Add(StatCard(dashboard.RecentChangeCount, "Recent changes"), 0, 1);
            grid.Add(StatCard(dashboard.NotificationCount, "Alerts"), 1, 1);
            _stats.Add(grid);

            foreach (var change in dashboard.Changes.Take(5))
            {
                _recent.Add(
                    MobileBrand.Card(
                        new VerticalStackLayout
                        {
                            Spacing = 4,
                            Children =
                            {
                                MobileBrand.Title(change.Title, 17),
                                MobileBrand.Body(change.FollowName, 12),
                                MobileBrand.Body(change.Summary, 13),
                                MobileBrand.Body(
                                    $"{change.Severity} · {change.DetectedAtUtc.ToLocalTime():g}",
                                    11)
                            }
                        }));
            }

            if (dashboard.Changes.Count == 0)
            {
                _recent.Add(
                    MobileBrand.Card(
                        MobileBrand.Body(
                            "No recorded changes yet. Your first successful checks establish the baseline.")));
            }

            _status.Text = dashboard.NeedsAttentionCount > 0
                ? $"{dashboard.NeedsAttentionCount} item(s) need your attention."
                : "Everything Clarity is watching is available below.";
        }
        catch (UnauthorizedAccessException)
        {
            _status.Text = "Your session expired. Sign in again.";
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
        }
    }

    private static View StatCard(
        int value,
        string label) =>
        MobileBrand.Card(
            new VerticalStackLayout
            {
                Spacing = 3,
                Children =
                {
                    new Label
                    {
                        Text = value.ToString(),
                        TextColor = MobileBrand.Ink,
                        FontSize = 28,
                        FontAttributes = FontAttributes.Bold
                    },
                    MobileBrand.Body(label, 12)
                }
            },
            14);

    private static View CreateBrandHeader() =>
        new HorizontalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Image
                {
                    Source = "clarity_logo.svg",
                    WidthRequest = 38,
                    HeightRequest = 38
                },
                new VerticalStackLayout
                {
                    Spacing = 1,
                    Children =
                    {
                        MobileBrand.Title("Clarity Belongs", 18),
                        new Label
                        {
                            Text = "KEEP AN EYE ON WHAT MATTERS",
                            TextColor = MobileBrand.Muted,
                            FontSize = 9,
                            CharacterSpacing = 1.1
                        }
                    }
                }
            }
        };
}

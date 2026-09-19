namespace ClarityBelongs.Mobile;

public sealed class LoginPage : ContentPage
{
    private readonly MobileApiClient _api;
    private readonly Func<Task> _signedIn;
    private readonly Entry _email = new() { Keyboard = Keyboard.Email, Placeholder = "Email" };
    private readonly Entry _password = new() { IsPassword = true, Placeholder = "Password" };
    private readonly Label _status = MobileBrand.Body(string.Empty, 13);
    private bool _checking;

    public LoginPage(
        MobileApiClient api,
        Func<Task> signedIn)
    {
        _api = api;
        _signedIn = signedIn;
        Title = "Sign in";
        BackgroundColor = MobileBrand.Background;

        var button = new Button { Text = "Sign in to Clarity" };
        MobileBrand.Primary(button);
        button.Clicked += async (_, _) => await SignInAsync(button);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 16,
                Padding = new Thickness(24, 60, 24, 40),
                MaximumWidthRequest = 520,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Image
                    {
                        Source = "clarity_logo.svg",
                        WidthRequest = 62,
                        HeightRequest = 62,
                        HorizontalOptions = LayoutOptions.Start
                    },
                    MobileBrand.Eyebrow("Clarity Belongs"),
                    MobileBrand.Title("Sign in to My Clarity", 34),
                    MobileBrand.Body("See your watches, alerts, history, and account from your phone.", 16),
                    MobileBrand.Card(
                        new VerticalStackLayout
                        {
                            Spacing = 12,
                            Children =
                            {
                                _email,
                                _password,
                                button,
                                _status
                            }
                        },
                        18)
                }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = TrySavedSessionAsync();
    }

    private async Task TrySavedSessionAsync()
    {
        if (_checking)
            return;

        _checking = true;

        try
        {
            if (!await _api.HasSessionAsync())
                return;

            _status.Text = "Opening My Clarity…";
            await _api.GetAccountAsync();
            await _signedIn();
        }
        catch
        {
            _api.SignOut();
            _status.Text = string.Empty;
        }
        finally
        {
            _checking = false;
        }
    }

    private async Task SignInAsync(Button button)
    {
        button.IsEnabled = false;
        _status.Text = "Signing in…";

        try
        {
            await _api.LoginAsync(
                _email.Text?.Trim() ?? string.Empty,
                _password.Text ?? string.Empty);
            await _signedIn();
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
        }
        finally
        {
            button.IsEnabled = true;
        }
    }
}

public sealed class ClarityTabbedPage : TabbedPage
{
    public ClarityTabbedPage(
        MobileApiClient api,
        Func<Task> signOut)
    {
        BarBackgroundColor = MobileBrand.Paper;
        BarTextColor = MobileBrand.Muted;
        SelectedTabColor = MobileBrand.Teal;
        UnselectedTabColor = MobileBrand.Muted;

        Children.Add(Wrap(new MainPage(api), "My Clarity", "clarity_logo.svg"));
        Children.Add(Wrap(new WatchesPage(api), "Watches", "icon_watches.svg"));
        Children.Add(Wrap(new AlertsPage(api), "Alerts", "icon_alerts.svg"));
        Children.Add(Wrap(new HistoryPage(api), "History", "icon_history.svg"));
        Children.Add(Wrap(new AccountPage(api, signOut), "Account", "icon_account.svg"));

#if ANDROID
        Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.TabbedPage
            .SetToolbarPlacement(
                this,
                Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.ToolbarPlacement.Bottom);
#endif
    }

    private static NavigationPage Wrap(
        Page page,
        string title,
        string icon) =>
        new(page)
        {
            Title = title,
            IconImageSource = icon,
            BarBackgroundColor = MobileBrand.Paper,
            BarTextColor = MobileBrand.Ink
        };
}

public sealed class WatchesPage : ContentPage
{
    private readonly MobileApiClient _api;
    private readonly VerticalStackLayout _items = new() { Spacing = 10 };
    private readonly Label _status = MobileBrand.Body("Loading watches…", 13);

    public WatchesPage(MobileApiClient api)
    {
        _api = api;
        Title = "Watches";
        BackgroundColor = MobileBrand.Background;

        var add = new Button { Text = "Watch something" };
        MobileBrand.Primary(add);
        add.Clicked += async (_, _) =>
            await Navigation.PushAsync(new AddWatchPage(_api));

        Content = CreatePage(
            "Watches",
            "Everything you asked Clarity to keep an eye on.",
            add,
            _status,
            _items);
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
            _items.Clear();

            foreach (var follow in dashboard.Follows)
                _items.Add(CreateFollowCard(follow));

            _status.Text = dashboard.Follows.Count == 1
                ? "1 active watch"
                : $"{dashboard.Follows.Count} active watches";
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
        }
    }

    private View CreateFollowCard(MobileFollowSummary follow)
    {
        var card = MobileBrand.Card(
            new VerticalStackLayout
            {
                Spacing = 5,
                Children =
                {
                    MobileBrand.Title(follow.Name, 18),
                    MobileBrand.Body($"{follow.Status} · {follow.MonitorType}", 13),
                    MobileBrand.Body(
                        follow.LatestChangeTitle
                            ?? $"Next check {follow.NextCheckAtUtc.ToLocalTime():g}",
                        12)
                }
            });

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
            await Navigation.PushAsync(
                new FollowDetailPage(_api, follow.FollowId));
        card.GestureRecognizers.Add(tap);
        return card;
    }

    private static ScrollView CreatePage(
        string title,
        string description,
        params View[] children)
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 14,
            Padding = new Thickness(18, 22, 18, 40),
            MaximumWidthRequest = 760,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                MobileBrand.Eyebrow("My Clarity"),
                MobileBrand.Title(title),
                MobileBrand.Body(description, 15)
            }
        };

        foreach (var child in children)
            stack.Add(child);

        return new ScrollView { Content = stack };
    }
}

public sealed class AlertsPage : ContentPage
{
    private readonly MobileApiClient _api;
    private readonly VerticalStackLayout _items = new() { Spacing = 10 };
    private readonly Label _status = MobileBrand.Body("Loading alerts…", 13);

    public AlertsPage(MobileApiClient api)
    {
        _api = api;
        Title = "Alerts";
        BackgroundColor = MobileBrand.Background;
        Content = BuildPage(
            "Alerts",
            "Failures, recoveries, expirations, and recorded changes.",
            _status,
            _items);
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
            _items.Clear();

            foreach (var alert in dashboard.Notifications)
            {
                _items.Add(
                    MobileBrand.Card(
                        new VerticalStackLayout
                        {
                            Spacing = 4,
                            Children =
                            {
                                MobileBrand.Title(alert.Subject, 17),
                                MobileBrand.Body(alert.BodySummary, 13),
                                MobileBrand.Body(alert.CreatedAtUtc.ToLocalTime().ToString("g"), 11)
                            }
                        }));
            }

            _status.Text = dashboard.Notifications.Count == 0
                ? "No alerts yet."
                : $"{dashboard.Notifications.Count} recent alerts";
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
        }
    }

    internal static ScrollView BuildPage(
        string title,
        string description,
        params View[] children)
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 14,
            Padding = new Thickness(18, 22, 18, 40),
            MaximumWidthRequest = 760,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                MobileBrand.Eyebrow("My Clarity"),
                MobileBrand.Title(title),
                MobileBrand.Body(description, 15)
            }
        };

        foreach (var child in children)
            stack.Add(child);

        return new ScrollView { Content = stack };
    }
}

public sealed class HistoryPage : ContentPage
{
    private readonly MobileApiClient _api;
    private readonly VerticalStackLayout _items = new() { Spacing = 10 };
    private readonly Label _status = MobileBrand.Body("Loading history…", 13);

    public HistoryPage(MobileApiClient api)
    {
        _api = api;
        Title = "History";
        BackgroundColor = MobileBrand.Background;
        Content = AlertsPage.BuildPage(
            "History",
            "Review recent recorded changes across all of your watches.",
            _status,
            _items);
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
            _items.Clear();

            foreach (var change in dashboard.Changes)
            {
                var review = new Button
                {
                    Text = change.IsAcknowledged
                        ? "Reviewed"
                        : "Mark reviewed",
                    IsEnabled = !change.IsAcknowledged
                };
                MobileBrand.Secondary(review);
                review.Clicked += async (_, _) =>
                {
                    await _api.AcknowledgeAsync(
                        change.FollowId,
                        change.ChangeId);
                    await LoadAsync();
                };

                _items.Add(
                    MobileBrand.Card(
                        new VerticalStackLayout
                        {
                            Spacing = 5,
                            Children =
                            {
                                MobileBrand.Title(change.Title, 17),
                                MobileBrand.Body(change.FollowName, 12),
                                MobileBrand.Body(change.Summary, 13),
                                MobileBrand.Body(
                                    $"{change.Severity} · {change.DetectedAtUtc.ToLocalTime():g}",
                                    11),
                                review
                            }
                        }));
            }

            _status.Text = dashboard.Changes.Count == 0
                ? "No changes recorded yet."
                : $"{dashboard.Changes.Count} recent changes";
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
        }
    }
}

public sealed class AccountPage : ContentPage
{
    private readonly MobileApiClient _api;
    private readonly Func<Task> _signOut;
    private readonly VerticalStackLayout _content = new() { Spacing = 12 };

    public AccountPage(
        MobileApiClient api,
        Func<Task> signOut)
    {
        _api = api;
        _signOut = signOut;
        Title = "Account";
        BackgroundColor = MobileBrand.Background;

        Content = AlertsPage.BuildPage(
            "Account",
            "Your Clarity membership and mobile session.",
            _content);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        _content.Clear();

        try
        {
            var account = await _api.GetAccountAsync();

            _content.Add(
                MobileBrand.Card(
                    new VerticalStackLayout
                    {
                        Spacing = 5,
                        Children =
                        {
                            MobileBrand.Eyebrow("Signed in"),
                            MobileBrand.Title(account.DisplayName, 22),
                            MobileBrand.Body(account.Email),
                            MobileBrand.Body($"{account.PlanName} plan · {account.ActiveFollowCount}/{account.MaxActiveFollows} follows"),
                            MobileBrand.Body($"{account.RemainingFollows} remaining · {account.HistoryDays} days history", 12)
                        }
                    }));

            var signOut = new Button { Text = "Sign out" };
            MobileBrand.Secondary(signOut);
            signOut.Clicked += async (_, _) =>
            {
                _api.SignOut();
                await _signOut();
            };
            _content.Add(signOut);
        }
        catch (Exception ex)
        {
            _content.Add(MobileBrand.Body(ex.Message));
        }
    }
}

public sealed class AddWatchPage : ContentPage
{
    private readonly MobileApiClient _api;
    private readonly Picker _product = new() { Title = "Choose a watch" };
    private readonly Entry _name = new() { Placeholder = "Name" };
    private readonly Entry _target = new() { Placeholder = "Public URL or domain" };
    private readonly Picker _cadence = new();
    private readonly Picker _importance = new();
    private readonly Label _status = MobileBrand.Body(string.Empty, 13);
    private MobileProductResponse[] _products = [];

    public AddWatchPage(MobileApiClient api)
    {
        _api = api;
        Title = "Watch something";
        BackgroundColor = MobileBrand.Background;

        _cadence.ItemsSource = new[] { "5", "15", "60", "360", "720", "1440", "10080" };
        _importance.ItemsSource = new[] { "Low", "Normal", "High", "Critical" };
        _importance.SelectedItem = "Normal";

        _product.SelectedIndexChanged += (_, _) => ApplyProduct();

        var save = new Button { Text = "Start watching" };
        MobileBrand.Primary(save);
        save.Clicked += async (_, _) => await SaveAsync(save);

        Content = AlertsPage.BuildPage(
            "Watch something",
            "Choose a supported public target and Clarity will begin building history.",
            _product,
            _name,
            _target,
            _cadence,
            _importance,
            save,
            _status);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_products.Length == 0)
            _ = LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            _products = await _api.GetProductsAsync();
            _product.ItemsSource = _products
                .Select(x => $"{x.Family} · {x.Name}")
                .ToArray();

            if (_products.Length > 0)
                _product.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
        }
    }

    private void ApplyProduct()
    {
        if (_product.SelectedIndex < 0
            || _product.SelectedIndex >= _products.Length)
        {
            return;
        }

        var selected = _products[_product.SelectedIndex];
        _name.Text = selected.Name;
        _target.Placeholder = selected.TargetPlaceholder;
        _cadence.SelectedItem = selected.DefaultCadenceMinutes.ToString();
        _importance.SelectedItem = selected.DefaultImportance;
    }

    private async Task SaveAsync(Button button)
    {
        if (_product.SelectedIndex < 0)
            return;

        button.IsEnabled = false;
        _status.Text = "Starting watch…";

        try
        {
            var selected = _products[_product.SelectedIndex];
            var cadence = int.TryParse(
                _cadence.SelectedItem?.ToString(),
                out var parsed)
                ? parsed
                : selected.DefaultCadenceMinutes;

            var followId = await _api.CreateFollowAsync(
                new MobileCreateFollowRequest(
                    selected.Slug,
                    _name.Text?.Trim() ?? selected.Name,
                    _target.Text?.Trim() ?? string.Empty,
                    cadence,
                    _importance.SelectedItem?.ToString()
                        ?? selected.DefaultImportance));

            _status.Text = "Clarity is watching.";
            await Navigation.PushAsync(
                new FollowDetailPage(_api, followId));
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
        }
        finally
        {
            button.IsEnabled = true;
        }
    }
}

public sealed class FollowDetailPage : ContentPage
{
    private readonly MobileApiClient _api;
    private readonly long _followId;
    private readonly VerticalStackLayout _content = new() { Spacing = 12 };

    public FollowDetailPage(
        MobileApiClient api,
        long followId)
    {
        _api = api;
        _followId = followId;
        Title = "Watch";
        BackgroundColor = MobileBrand.Background;
        Content = AlertsPage.BuildPage(
            "Watch",
            "Status, controls, and recent history.",
            _content);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        _content.Clear();

        try
        {
            var model = await _api.GetFollowAsync(_followId);

            _content.Add(
                MobileBrand.Card(
                    new VerticalStackLayout
                    {
                        Spacing = 5,
                        Children =
                        {
                            MobileBrand.Title(model.Name, 24),
                            MobileBrand.Body(model.Target),
                            MobileBrand.Body($"{model.Status} · {model.Importance} · every {FormatCadence(model.CheckCadenceMinutes)}")
                        }
                    }));

            var check = new Button { Text = "Check now" };
            MobileBrand.Primary(check);
            check.Clicked += async (_, _) =>
            {
                await _api.RunNowAsync(_followId);
                await LoadAsync();
            };

            var paused = model.Status.Equals(
                "Paused",
                StringComparison.OrdinalIgnoreCase);
            var pause = new Button
            {
                Text = paused ? "Resume" : "Pause"
            };
            MobileBrand.Secondary(pause);
            pause.Clicked += async (_, _) =>
            {
                await _api.SetPausedAsync(_followId, !paused);
                await LoadAsync();
            };

            _content.Add(
                new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children = { check, pause }
                });

            _content.Add(MobileBrand.Eyebrow("History"));

            if (model.Changes.Count == 0)
            {
                _content.Add(
                    MobileBrand.Body("No changes yet. The first successful check establishes the baseline."));
            }
            else
            {
                foreach (var change in model.Changes)
                {
                    var review = new Button
                    {
                        Text = change.IsAcknowledged
                            ? "Reviewed"
                            : "Mark reviewed",
                        IsEnabled = !change.IsAcknowledged
                    };
                    MobileBrand.Secondary(review);
                    review.Clicked += async (_, _) =>
                    {
                        await _api.AcknowledgeAsync(
                            _followId,
                            change.ChangeId);
                        await LoadAsync();
                    };

                    _content.Add(
                        MobileBrand.Card(
                            new VerticalStackLayout
                            {
                                Spacing = 4,
                                Children =
                                {
                                    MobileBrand.Title(change.Title, 17),
                                    MobileBrand.Body(change.Summary),
                                    MobileBrand.Body($"{change.Severity} · {change.DetectedAtUtc.ToLocalTime():g}", 11),
                                    review
                                }
                            }));
                }
            }
        }
        catch (Exception ex)
        {
            _content.Add(MobileBrand.Body(ex.Message));
        }
    }

    private static string FormatCadence(int minutes)
    {
        if (minutes >= 10080)
            return "1 week";

        if (minutes >= 1440)
            return $"{minutes / 1440} day";

        if (minutes >= 60)
            return $"{minutes / 60} hour";

        return $"{minutes} minutes";
    }
}

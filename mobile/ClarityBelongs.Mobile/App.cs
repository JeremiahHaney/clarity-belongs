namespace ClarityBelongs.Mobile;

public sealed class App : Application
{
    private readonly MobileApiClient _api;
    private Window? _window;

    public App(MobileApiClient api)
    {
        _api = api;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _window = new Window
        {
            Title = "Clarity Belongs",
            Page = CreateLoginPage()
        };

        return _window;
    }

    private Page CreateLoginPage() =>
        new NavigationPage(
            new LoginPage(
                _api,
                ShowShellAsync))
        {
            BarBackgroundColor = MobileBrand.Paper,
            BarTextColor = MobileBrand.Ink
        };

    private Task ShowShellAsync()
    {
        if (_window is not null)
            _window.Page = new ClarityTabbedPage(_api, ShowLoginAsync);

        return Task.CompletedTask;
    }

    private Task ShowLoginAsync()
    {
        if (_window is not null)
            _window.Page = CreateLoginPage();

        return Task.CompletedTask;
    }
}

using ClarityBelongs.Mobile.Core.Modules;

namespace ClarityBelongs.Mobile;

public sealed class MainPage : ContentPage
{
	public MainPage(IReadOnlyList<ClarityMobileModule> modules)
	{
		Title = "Clarity Belongs";

		var list = new VerticalStackLayout
		{
			Spacing = 12,
			Padding = 20
		};

		foreach (var module in modules)
		{
			list.Children.Add(new Button
			{
				Text = module.Title,
				CommandParameter = module.Route
			});
		}

		Content = new ScrollView
		{
			Content = list
		};
	}
}

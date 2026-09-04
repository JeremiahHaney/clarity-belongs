using ClarityBelongs.Mobile.Core.Modules;

namespace ClarityBelongs.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();

		builder
			.UseMauiApp<App>();

		builder.Services.AddSingleton(ClarityMobileModuleRegistry.Modules);
		builder.Services.AddSingleton<MainPage>();

		return builder.Build();
	}
}

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Loggez.Core.Interfaces;
using Loggez.Infrastructure.Parsing;
using Loggez.UI.Services;
using Loggez.UI.ViewModels;
using Loggez.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Loggez;

public partial class App : Application
{
    public override void Initialize() 
        => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogParser, DefaultLogParser>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddTransient<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        var provider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = provider.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
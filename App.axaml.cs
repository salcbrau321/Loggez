using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Loggez.Core.Interfaces;
using Loggez.Infrastructure.Analysis;
using Loggez.Infrastructure.External;
using Loggez.Infrastructure.FileLoading;
using Loggez.Infrastructure.Indexing;
using Loggez.Infrastructure.Parsing;
using Loggez.UI.Services;
using Loggez.UI.ViewModels;
using Loggez.UI.Views;
using Lucene.Net.Store;
using Microsoft.Extensions.DependencyInjection;

namespace Loggez;

public partial class App : Application
{
    public override void Initialize() 
        => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var indexFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Loggez", "Index");
        System.IO.Directory.CreateDirectory(indexFolder);
        
        var services = new ServiceCollection();
        services.AddSingleton<IFileLoaderService, FileLoaderService>();
        services.AddSingleton<IIndexService, IndexService>(sp => new IndexService(indexFolder, sp.GetRequiredService<IAnalyzerFactory>()));
        services.AddSingleton<IAnalyzerFactory, AnalyzerFactory>();
        services.AddSingleton<IExternalOpener, ExternalOpener>();
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
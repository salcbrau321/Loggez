using Avalonia;
using System;
using Avalonia.Controls.ApplicationLifetimes;
using Loggez.Core.Interfaces;
using Loggez.Core.Models;
using Loggez.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Loggez;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
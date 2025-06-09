using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Loggez.UI.ViewModels
{
    public partial class SettingsWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _externalOpenerPath = Settings.ExternalOpener;

        [ObservableProperty]
        private string _supportedExtensions = string.Join(",", Settings.SupportedExtensions);

        [RelayCommand]
        public void Save()
        {
            Settings.ExternalOpener = ExternalOpenerPath;
            Settings.SupportedExtensions =
                SupportedExtensions
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s =>
                    {
                        var t = s.Trim();
                        return t.StartsWith('.') ? t : "." + t;
                    })
                    .ToList();
            Settings.Save();
            CloseWindow();
        }

        [RelayCommand]
        public void Cancel() => CloseWindow();

        private void CloseWindow()
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = desktop.Windows
                    .OfType<Window>()
                    .FirstOrDefault(w => w.DataContext == this);
                window?.Close();
            }
        }
    }
}
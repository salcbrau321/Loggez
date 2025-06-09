using System.Collections.Generic;
using System.Linq;
using Loggez.Core.Interfaces;

namespace Loggez.UI.Services;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Threading.Tasks;

public class DialogService : IDialogService
{
    public async Task<string[]?> ShowOpenFileDialogAsync(string title, IEnumerable<string> filters, bool allowMultiple)
    {
        if (Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;

        var parent = desktop.MainWindow;
        var dlg = new OpenFileDialog
        {
            Title = title,
            AllowMultiple = allowMultiple,
            Filters = { new FileDialogFilter { Name = "Files", Extensions = filters.ToList() } }
        };
        return await dlg.ShowAsync(parent);
    }
}
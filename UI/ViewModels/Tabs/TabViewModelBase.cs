using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Loggez.UI.ViewModels.Tabs;

public abstract class TabViewModelBase : ObservableObject, ITabViewModel
{
    public string Title { get; }
    public IRelayCommand CloseCommand { get; }
    
    public event EventHandler? CloseRequested;

    protected TabViewModelBase(string title)
    {
        Title = title;
        CloseCommand = new RelayCommand(OnCloseCommand);
    }

    private void OnCloseCommand()
    {
        RunClose();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    protected abstract void RunClose();
}
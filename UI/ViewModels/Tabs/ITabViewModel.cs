using CommunityToolkit.Mvvm.Input;

namespace Loggez.UI.ViewModels.Tabs;

public interface ITabViewModel
{
    string Title { get; }
    IRelayCommand CloseCommand { get; }
}
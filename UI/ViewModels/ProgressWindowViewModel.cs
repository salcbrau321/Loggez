using CommunityToolkit.Mvvm.ComponentModel;

namespace Loggez.UI.ViewModels;

public class ProgressWindowViewModel : ObservableObject
{
    public int Total { get; }

    private int _completed;
    public int Completed
    {
        get => _completed;
        set => SetProperty(ref _completed, value);
    }

    public string Message { get; }

    public ProgressWindowViewModel(int total, string message = "Indexing…")
    {
        Total   = total;
        Message = message;
    }
}
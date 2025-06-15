using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Loggez.UI.ViewModels;

public abstract class SolutionItemViewModel : ObservableObject
{
    public string Name { get; }
    
    public string FullPath { get; }

    protected SolutionItemViewModel(string fullPath)
    {
        FullPath = fullPath;
        Name = Path.GetFileName(fullPath);
    }
}
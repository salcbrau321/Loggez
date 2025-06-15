using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace Loggez.UI.ViewModels;

public class SolutionFolderViewModel : SolutionItemViewModel
{
    public ObservableCollection<SolutionItemViewModel> Children { get; }

    public SolutionFolderViewModel(string fullPath, IEnumerable<SolutionItemViewModel> children)
        : base(fullPath)
    {
        Children = new ObservableCollection<SolutionItemViewModel>(children);
    }
}

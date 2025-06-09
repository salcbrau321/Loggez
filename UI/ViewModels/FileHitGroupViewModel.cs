using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Loggez.UI.ViewModels;

public class FileHitGroupViewModel
{
    public string FileName { get; set; }
    public ObservableCollection<HitViewModel> Hits { get; set; }
    
    public FileHitGroupViewModel(string fileName, IEnumerable<HitViewModel> hits)
    {
        this.FileName = fileName;
        this.Hits = new ObservableCollection<HitViewModel>(hits);
    }
}
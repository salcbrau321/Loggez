using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls.Shapes;

namespace Loggez.UI.ViewModels.Tabs.Search;

public class FileHitGroupViewModel
{
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public DateTime FileDate { get; set; }
    public ObservableCollection<HitViewModel> Hits { get; set; }
    
    public FileHitGroupViewModel(string filePath, DateTime fileDate, IEnumerable<HitViewModel> hits)
    {
        this.FilePath = filePath;
        this.FileName = System.IO.Path.GetFileName(filePath);
        this.FileDate = fileDate;
        this.Hits = new ObservableCollection<HitViewModel>(hits);
    }
}
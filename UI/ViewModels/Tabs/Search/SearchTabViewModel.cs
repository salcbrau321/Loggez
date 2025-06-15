using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loggez.Core.Enums;
using Loggez.Core.Interfaces;
using Loggez.Core.Models;

namespace Loggez.UI.ViewModels.Tabs.Search;

    public partial class SearchTabViewModel : TabViewModelBase
    {
        private readonly IIndexService _indexer;
        private readonly IExternalOpener _opener;
        private readonly DispatcherTimer _searchDebounceTimer;

        private readonly List<LogFile> _logs = new();
        
        public ObservableCollection<string> LoadedFiles { get; } = new();
        public ObservableCollection<HitViewModel> Hits { get; } = new();
        public ObservableCollection<FileHitGroupViewModel> FileHitGroups { get; } = new();

        [ObservableProperty] private string _searchQuery = "";
        [ObservableProperty] private DateTime? _fromDate    = null;
        [ObservableProperty] private DateTime? _toDate      = null;
        [ObservableProperty] private HitViewModel? _selectedHit = null;
        [ObservableProperty] private FileHitGroupViewModel? _selectedHitGroup = null;
        [ObservableProperty] private bool _canOpenExternal;
        [ObservableProperty] private string _selectedHitContent = "";
        [ObservableProperty] private TextDocument _selectedHitDocument = new TextDocument(string.Empty);
        [ObservableProperty] private GridLength _leftPaneWidth = new GridLength(1, GridUnitType.Star);
        public bool CanSearch => _indexer.IndexState == IndexState.Ready;
        public bool HasDateRangeError => FromDate.HasValue && ToDate.HasValue && FromDate > ToDate;
        public string DateRangeErrorMessage => HasDateRangeError
            ? "❗ Start date must be on or before End date."
            : "";
        
        public event EventHandler? RequestClose;
        
        public SearchTabViewModel(
            IIndexService indexer, 
            IExternalOpener opener,
            string title = "Search") : base(title)
        {
            _indexer  = indexer;
            _opener   = opener;

            _searchDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _searchDebounceTimer.Tick += (_, __) =>
            {
                _searchDebounceTimer.Stop();
                SearchCommand.Execute(null);
            };
        }

        protected override void RunClose()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        
        partial void OnSelectedHitGroupChanged(FileHitGroupViewModel? oldGroup, FileHitGroupViewModel? newGroup)
        {
            if (newGroup?.Hits.Count > 0) SelectedHit = newGroup.Hits[0];
        }
        
        partial void OnSearchQueryChanged(string oldVal, string newVal)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }
        
        partial void OnSelectedHitChanged(HitViewModel? _, HitViewModel? newVal)
        {
            if (newVal is null)
            {
                SelectedHitContent  = string.Empty;
                SelectedHitDocument = new TextDocument(string.Empty);
                CanOpenExternal     = false;
            }
            else
            {
                var fullText = File.ReadAllText(newVal.FullPath);
                SelectedHitContent  = fullText;
                SelectedHitDocument = new TextDocument(fullText);
                CanOpenExternal     = true;
            }
        }
        
        partial void OnFromDateChanged(DateTime? oldVal, DateTime? newVal)
            => SearchCommand.Execute(null);

        partial void OnToDateChanged(DateTime? oldVal, DateTime? newVal)
            => SearchCommand.Execute(null);

        [RelayCommand(CanExecute = nameof(CanSearch))]
        private async Task Search()
        {
            Hits.Clear();
            FileHitGroups.Clear();

            var searchResults = await _indexer.SearchAsync(SearchQuery, FromDate, ToDate);
            List<HitViewModel> results = new List<HitViewModel>();
            for (int i = 0; i < searchResults.Count; i++)
                results.Add(new HitViewModel(searchResults[i]));
            
            var newGroups = results
                .GroupBy(h => h.FullPath)
                .ToDictionary(g => g.Key, g => g.ToList());
            
            var prevSelectedPath = SelectedHitGroup?.FileName;

            for (int i = FileHitGroups.Count - 1; i >= 0; i--)
            {
                var grp = FileHitGroups[i];
                if (!newGroups.ContainsKey(grp.FileName))
                    FileHitGroups.RemoveAt(i);
            }

            foreach (var kv in newGroups)
            {
                var path     = kv.Key;
                var newHits  = kv.Value;
                var existing = FileHitGroups.FirstOrDefault(f => f.FileName == path);
                if (existing != null)
                {
                    existing.Hits.Clear();
                    foreach (var hit in newHits)
                        existing.Hits.Add(hit);
                }
                else
                {
                    FileHitGroups.Add(new FileHitGroupViewModel(path, newHits[0].FileDate, newHits));
                }
            }

            if (prevSelectedPath != null)
            {
                var want = FileHitGroups.FirstOrDefault(f => f.FileName == prevSelectedPath);
                if (want != null)
                {
                    SelectedHitGroup = want;
                    return;
                }
            }
            
            if (FileHitGroups.Count > 0)
                SelectedHitGroup = FileHitGroups[0];
            else
                SelectedHitGroup = null;
        }

        [RelayCommand(CanExecute = nameof(CanOpenExternal))]
        private void OpenExternal()
        {
            if (SelectedHit == null) return;
            _opener.Open(SelectedHit.FullPath, SelectedHit.LineNumber);
        }

        [RelayCommand]
        private void Clear()
        {
            _logs.Clear();
            LoadedFiles.Clear();
        }
        
        [RelayCommand]
        private void OpenHit(HitViewModel hit)
        {
            if (hit == null) return;

            var path = hit.FullPath;
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            else if (OperatingSystem.IsLinux())
                Process.Start("xdg-open", path);
            else if (OperatingSystem.IsMacOS())
                Process.Start("open", path);
            else
                throw new PlatformNotSupportedException();
        }


    }


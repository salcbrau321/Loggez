using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loggez.Core.Interfaces;
using Loggez.Core.Models;
using Loggez.UI.Views;

namespace Loggez.UI.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IDialogService _dialog;
        private readonly IFileLoaderService _loader;
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
        [ObservableProperty] private bool _isIndexing;
        [ObservableProperty] private bool _isIndexReady;
        [ObservableProperty] private bool _canOpenExternal;
        [ObservableProperty] private string _selectedHitContent = "";
        [ObservableProperty] private TextDocument _selectedHitDocument = new TextDocument(string.Empty);

        public bool CanSearch => IsIndexReady && !IsIndexing;
        public bool HasDateRangeError => FromDate.HasValue && ToDate.HasValue && FromDate > ToDate;
        public string DateRangeErrorMessage => HasDateRangeError
            ? "❗ Start date must be on or before End date."
            : "";

        public MainWindowViewModel(
            IDialogService     dialog,
            IFileLoaderService loader,
            IIndexService      indexer,
            IExternalOpener    opener)
        {
            _dialog   = dialog;
            _loader   = loader;
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

        partial void OnSelectedHitGroupChanged(FileHitGroupViewModel? oldGroup, FileHitGroupViewModel? newGroup)
        {
            if (newGroup?.Hits.Count > 0) SelectedHit = newGroup.Hits[0];
        }
        
        partial void OnIsIndexingChanged(bool oldValue, bool newValue)
            => OnPropertyChanged(nameof(CanSearch));

        partial void OnIsIndexReadyChanged(bool oldValue, bool newValue)
            => OnPropertyChanged(nameof(CanSearch));
        
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

        [RelayCommand]
        private void MinimizeWindow()
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow.WindowState = WindowState.Minimized;
        }

        [RelayCommand]
        private void MaximizeRestoreWindow()
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var win = desktop.MainWindow;
                win.WindowState = win.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
        }

        [RelayCommand]
        private void CloseWindow()
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        }
        
        [RelayCommand]
        private void Clear()
        {
            _logs.Clear();
            LoadedFiles.Clear();
            Hits.Clear();
            FileHitGroups.Clear();
            SelectedHit = null;
            IsIndexReady = false;
            SelectedHitContent = "";
        }

        [RelayCommand]
        private void Exit()
        {
            (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.Shutdown();
        }

        [RelayCommand]
        public async Task BrowseAsync()
        {
            var picks = await _dialog.ShowOpenFileDialogAsync(
                title: "Select log files or ZIPs",
                filters: new[] { "log", "txt", "zip" },
                allowMultiple: true);

            if (picks == null || picks.Length == 0)
                return;

            await LoadAndIndex(picks);
        }
        
        [RelayCommand]
        public async Task OpenSettingsAsync()
        {
            var vm  = new SettingsWindowViewModel { /* … */ };
            var dlg = new SettingsWindow { DataContext = vm };
            var desktop = (IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime;
            var mainWin = desktop?.MainWindow;
            if (mainWin != null) await dlg.ShowDialog(mainWin);
            else dlg.Show();
        }
        
        [RelayCommand]
        public async Task BrowseFolderAsync()
        {
            var lifetime = (IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime;
            var folder   = await new OpenFolderDialog { Title = "Select log folder" }
                                         .ShowAsync(lifetime.MainWindow);
            if (string.IsNullOrWhiteSpace(folder))
                return;

            await LoadAndIndex(new[] { folder });
        }

        [RelayCommand]
        public async Task BrowseZipAsync()
        {
            var lifetime = (IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime;
            var dlg = new OpenFileDialog
            {
                Title = "Select ZIP file",
                AllowMultiple = false,
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "ZIP", Extensions = new List<string> { "zip" } }
                }
            };
            var picks = await dlg.ShowAsync(lifetime.MainWindow);
            if (picks == null || picks.Length != 1)
                return;

            await LoadAndIndex(picks);
        }

        private async Task LoadAndIndex(string[] inputs)
        {
            _logs.Clear();
            LoadedFiles.Clear();
            Hits.Clear();
            FileHitGroups.Clear();
            SelectedHit = null;
            IsIndexing  = true;
            IsIndexReady = false;
            SelectedHitContent = "";
            
            var logs = await _loader.LoadAsync(inputs);
            foreach (var lf in logs)
            {
                _logs.Add(lf);
                LoadedFiles.Add(Path.GetFileName(lf.Path));
            }
            
            var total    = logs.Sum(l => l.Entries.Count);
            var progVm   = new ProgressWindowViewModel(total);
            var progress = new Progress<int>(c =>
                Dispatcher.UIThread.Post(() => progVm.Completed = c));
            var dlg      = new ProgressWindow { DataContext = progVm };
            dlg.Show((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
            
            await _indexer.BuildIndexAsync(logs, progress, CancellationToken.None);

            dlg.Close();
            IsIndexing = false;
            IsIndexReady = true;

            if (!string.IsNullOrWhiteSpace(SearchQuery))
                SearchCommand.Execute(null);
        }

        [RelayCommand(CanExecute = nameof(CanSearch))]
        private async Task Search()
        {
            Hits.Clear();
            FileHitGroups.Clear();

            var results = await _indexer.SearchAsync(SearchQuery, FromDate, ToDate);
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
                    FileHitGroups.Add(new FileHitGroupViewModel(path, newHits));
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
}

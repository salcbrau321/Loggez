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
using Loggez.UI.ViewModels.Tabs;
using Loggez.UI.ViewModels.Tabs.Search;
using Loggez.UI.Views;

namespace Loggez.UI.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IDialogService _dialog;
        private readonly IFileLoaderService _loader;
        private readonly IIndexService _indexer;
        private readonly IExternalOpener _opener;
        private readonly List<LogFile> _logs = new();
        
        private ObservableCollection<SolutionItemViewModel> _solutionItems =  new ObservableCollection<SolutionItemViewModel>();
        
        public ObservableCollection<string> LoadedFiles { get; } = new();
        public ObservableCollection<SolutionItemViewModel> SolutionItems
        {
            get => _solutionItems;
            private set => SetProperty(ref _solutionItems, value);
        }
        [ObservableProperty] private ITabViewModel _selectedTab;

        private SolutionItemViewModel _selectedSolutionItem;
        public SolutionItemViewModel SelectedSolutionItem
        {
            get => _selectedSolutionItem;
            set => SetProperty(ref _selectedSolutionItem, value);
        }
        
        public ObservableCollection<ITabViewModel> Tabs { get; }  = new ObservableCollection<ITabViewModel>();
        
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
            
            Action<ITabViewModel> close = tab =>
            {
                if (tab is SearchTabViewModel) return; // never close search
                Tabs.Remove(tab);
                if (SelectedTab == tab)
                    SelectedTab = Tabs[0];
            };
            var title = $"Search {Tabs.Count(t => t is SearchTabViewModel) + 1}";
            var searchTab = new SearchTabViewModel(_indexer, _opener);
            searchTab.RequestClose += CloseTab;
            
            Tabs.Add(searchTab);
            SelectedTab = searchTab;
        }

        private void CloseTab(object? sender, EventArgs eventArgs)
        {
            ITabViewModel tab = sender as ITabViewModel;
            Tabs.Remove(tab);
            if (SelectedTab == tab && Tabs.Count > 0)
                SelectedTab = Tabs[0];
        }
        
        private async Task LoadSolutionTreeAsync(string[] inputs)
        {
            await Task.Run(() =>
            {
                var roots = inputs.Select(path =>
                {
                    if (Directory.Exists(path))
                    {
                        return (SolutionItemViewModel)BuildFolderNode(new DirectoryInfo(path));
                    }
                    else
                    {
                        return (SolutionItemViewModel)new SolutionFileViewModel(path);
                    }
                });

                SolutionItems = new ObservableCollection<SolutionItemViewModel>(roots);
                OnPropertyChanged(nameof(SolutionItems));
            });
        }
        
        private SolutionFolderViewModel BuildFolderNode(DirectoryInfo dir)
        {
            var childFolders = dir
                .EnumerateDirectories()
                .Select(sub => BuildFolderNode(sub));
            
            var childFiles = dir
                .EnumerateFiles()
                .Select(f => new SolutionFileViewModel(f.FullName));
            
            var allChildren = childFolders
                .Cast<SolutionItemViewModel>()
                .Concat(childFiles);

            return new SolutionFolderViewModel(dir.FullName, allChildren);
        }
        
        private IEnumerable<SolutionItemViewModel> BuildFolder(DirectoryInfo dir)
        {
            yield return new SolutionFolderViewModel(
                dir.FullName,
                dir.GetDirectories().SelectMany(BuildFolder)
                    .Concat(dir.GetFiles().Select(f => new SolutionFileViewModel(f.FullName)))
            );
        }
        
        public IRelayCommand<ITabViewModel> CloseTabCommand { get; }
        
        [RelayCommand]
        private void MinimizeWindow()
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow.WindowState = WindowState.Minimized;
        }

        [RelayCommand]
        private void OpenLogFileTab(SolutionItemViewModel item)
        {
            if (item is SolutionFileViewModel file)
            {
                var path = file.FullPath; // assume FullPath property exists
                var tab  = new LogFileTabViewModel(path);
                tab.RequestClose += CloseTab;
                Tabs.Add(tab);
                SelectedTab = tab;
            }
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
        private void OpenSearchTab()
        {
            var title = $"Search {Tabs.Count(t => t is SearchTabViewModel) + 1}";
            var newTab = new SearchTabViewModel(_indexer, _opener, title);
            newTab.CloseRequested += CloseTab;
            
            Tabs.Add(newTab);
            SelectedTab = newTab;
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
            await LoadSolutionTreeAsync(inputs);
            
            dlg.Close();
        }
    }
}

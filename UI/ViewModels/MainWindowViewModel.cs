using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Loggez.Core.Interfaces;
using Loggez.Core.Models;
using Loggez.UI.Views;
using Lucene.Net.Analysis.Miscellaneous;
using Directory = System.IO.Directory;

namespace Loggez.UI.ViewModels
{
    
    public class SubstringAnalyzer : Analyzer
    {
        private readonly LuceneVersion _version;
        public SubstringAnalyzer(LuceneVersion version) => _version = version;

        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            var tokenizer = new KeywordTokenizer(reader);
            TokenStream stream = new LowerCaseFilter(_version, tokenizer);
            stream = new NGramTokenFilter(_version, stream, 4, 25);
            return new TokenStreamComponents(tokenizer, stream);
        }
    }

    public class RawAnalyzer : Analyzer
    {
        private readonly LuceneVersion _version;
        public RawAnalyzer(LuceneVersion version) => _version = version;

        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            var tokenizer = new KeywordTokenizer(reader);
            var stream = new LowerCaseFilter(_version, tokenizer);
            return new TokenStreamComponents(tokenizer, stream);
        }
    }

    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IDialogService _dialog;
        private readonly ILogParser     _parser;
        private readonly List<LogFile>  _logs = new();
        private readonly DispatcherTimer _searchDebounceTimer;
        private readonly RAMDirectory _luceneIndex = new();
        private readonly Analyzer _analyzer;

        private IndexWriter _writer;
        private IndexSearcher _searcher;

        public ObservableCollection<string> LoadedFiles      { get; } = new();
        public ObservableCollection<HitViewModel> Hits        { get; } = new();
        public ObservableCollection<FileHitGroupViewModel> FileHitGroups { get; } = new();

        public bool CanSearch => IsIndexReady && !IsIndexing;
        public bool HasDateRangeError => FromDate.HasValue && ToDate.HasValue && FromDate > ToDate;
        public string DateRangeErrorMessage => HasDateRangeError
            ? "❗ Start date must be on or before End date."
            : "";

        private TextDocument _selectedHitDocument = new("");
        public TextDocument SelectedHitDocument
        {
            get => _selectedHitDocument;
            set => SetProperty(ref _selectedHitDocument, value);
        }

        [ObservableProperty] private string     _searchQuery        = "";
        [ObservableProperty] private bool       _isCaseSensitive;
        [ObservableProperty] private DateTime?  _fromDate           = null;
        [ObservableProperty] private DateTime?  _toDate             = null;
        [ObservableProperty] private HitViewModel? _selectedHit     = null;
        [ObservableProperty] private string     _selectedHitContent = "";
        [ObservableProperty] private bool       _canOpenExternal;
        [ObservableProperty] private FileHitGroupViewModel? _selectedHitGroup = null;
        [ObservableProperty] private bool _isIndexing;
        [ObservableProperty] private bool _isIndexReady;
        
        partial void OnSelectedHitGroupChanged(FileHitGroupViewModel? _, FileHitGroupViewModel? nv)
        {
            if (nv?.Hits.Count > 0)
                SelectedHit = nv.Hits[0];
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
        partial void OnSearchQueryChanged(string _, string __)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }
        partial void OnIsCaseSensitiveChanged(bool _, bool __) => DoSearch();
        partial void OnFromDateChanged(DateTime? _, DateTime? __) => ValidateDates();
        partial void OnToDateChanged(DateTime? _, DateTime? __) => ValidateDates();
        partial void OnIsIndexingChanged(bool oldVal, bool newVal) => OnPropertyChanged(nameof(CanSearch));
        partial void OnIsIndexReadyChanged(bool oldVal, bool newVal) => OnPropertyChanged(nameof(CanSearch));
       
        
        public MainWindowViewModel(IDialogService dialog, ILogParser parser)
        {
            _dialog = dialog;
            _parser = parser;

            var substringAnalyzer = new SubstringAnalyzer(LuceneVersion.LUCENE_48);
            var rawAnalyzer       = new RawAnalyzer(LuceneVersion.LUCENE_48);

            _analyzer = new PerFieldAnalyzerWrapper(
                substringAnalyzer,
                new Dictionary<string, Analyzer>
                {
                    { "ContentRaw", rawAnalyzer }
                }
            );

            _searchDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _searchDebounceTimer.Tick += (_, __) =>
            {
                _searchDebounceTimer.Stop();
                DoSearch();
            };
        }

        private void MarkIndexStale()
        {
            IsIndexReady = false;
            OnPropertyChanged(nameof(CanSearch));
        }

        [RelayCommand]
        private void Clear()
        {
            _logs.Clear();
            LoadedFiles.Clear();
            Hits.Clear();
            FileHitGroups.Clear();
            SelectedHit = null;
            SelectedHitContent = "";
            CanOpenExternal = false;
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

            if (picks?.Length > 0)
            {
                var logFiles = new List<string>();
                foreach (var path in picks)
                {
                    if (Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        Directory.CreateDirectory(tempDir);
                        using var archive = ZipFile.OpenRead(path);
                        foreach (var entry in archive.Entries
                            .Where(e => Settings.SupportedExtensions
                                .Contains(Path.GetExtension(e.Name), StringComparer.OrdinalIgnoreCase)))
                        {
                            var dest = Path.Combine(tempDir, entry.Name);
                            entry.ExtractToFile(dest);
                            logFiles.Add(dest);
                        }
                    }
                    else
                    {
                        logFiles.Add(path);
                    }
                }
                await LoadAndIndexFiles(logFiles.ToArray());
            }
        }

        [RelayCommand]
        public async Task BrowseFolderAsync()
        {
            var lifetime   = (IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime;
            var mainWindow = lifetime.MainWindow;
            var folder     = await new OpenFolderDialog { Title = "Select log folder" }
                                      .ShowAsync(mainWindow);
            if (string.IsNullOrWhiteSpace(folder)) return;

            var exts = Settings.SupportedExtensions;
            var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                                 .Where(f => exts.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                                 .ToArray();
            await LoadAndIndexFiles(files);
        }

        [RelayCommand]
        public async Task BrowseZipAsync()
        {
            var lifetime   = (IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime;
            var mainWindow = lifetime.MainWindow;
            var dlg = new OpenFileDialog
            {
                Title = "Select ZIP file",
                AllowMultiple = false,
                Filters = new List<FileDialogFilter> { new FileDialogFilter { Name = "ZIP", Extensions = new List<string> { "zip" } } }
            };
            var result = await dlg.ShowAsync(mainWindow);
            if (result?.Length != 1) return;

            var zipPath = result[0];
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var logFiles = new List<string>();
            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries
                .Where(e => Settings.SupportedExtensions
                    .Contains(Path.GetExtension(e.Name), StringComparer.OrdinalIgnoreCase)))
            {
                var dest = Path.Combine(tempDir, entry.Name);
                entry.ExtractToFile(dest);
                logFiles.Add(dest);
            }
            await LoadAndIndexFiles(logFiles.ToArray());
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

        private async Task LoadAndIndexFiles(string[] files)
        {
            MarkIndexStale();
            IsIndexing = true;
            _logs.Clear();
            LoadedFiles.Clear();
            Hits.Clear();
            FileHitGroups.Clear(); 
            SelectedHit = null;
            CanOpenExternal = false;

            int entryCount = 0;
            foreach (var path in files)
            {
                LoadedFiles.Add(Path.GetFileName(path));
                var lf = new LogFile(path, _parser);
                lf.Parse();
                _logs.Add(lf);
                entryCount += lf.Entries.Count;
            }
            await BuildIndex(entryCount);
        }

        private async Task BuildIndex(int totalCount)
        {
            _writer?.Dispose();
            var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, _analyzer)
            {
                RAMBufferSizeMB = 256
            };
            _writer = new IndexWriter(_luceneIndex, config);

            var progressVm = new ProgressWindowViewModel(totalCount);
            ProgressWindow dlg = new ProgressWindow { DataContext = progressVm };
            var owner = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime) ?.MainWindow;
            if (owner != null)
                dlg.Show(owner);
            else
                dlg.Show();

            await Task.Run(() =>
            {
                try
                {
                    var count = 0;
                    foreach (var lf in _logs)
                    foreach (var entry in lf.Entries)
                    {
                        _writer.AddDocument(new Document
                        {
                            new StringField("FullPath", lf.Path, Field.Store.YES),
                            new Int32Field("LineNumber", entry.LineNumber,         Field.Store.YES),
                            new TextField("ContentRaw", entry.RawLine,            Field.Store.YES),
                            new TextField("ContentSubstr",  entry.RawLine, Field.Store.YES),
                            new Int64Field("FileTimestamp", lf.CreatedDate.Ticks,    Field.Store.YES)
                        });
                        Dispatcher.UIThread.Post(() => progressVm.Completed = ++count);
                    }
                    _writer.Commit();
                    _searcher = new IndexSearcher(DirectoryReader.Open(_luceneIndex));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            });

            IsIndexing = false;
            IsIndexReady = true;
            dlg.Close();
        }

        private void ValidateDates()
        {
            OnPropertyChanged(nameof(HasDateRangeError));
            OnPropertyChanged(nameof(DateRangeErrorMessage));
            DoSearch();
        }

        private void DoSearch()
        {
            Hits.Clear();
            FileHitGroups.Clear();

            if (string.IsNullOrWhiteSpace(SearchQuery) || _searcher == null)
                return;

            var term = IsCaseSensitive 
                ? SearchQuery 
                : SearchQuery.ToLowerInvariant();
            
            var tq = new TermQuery(new Term("ContentSubstr", term));
            Query finalQuery = tq;

            if (!HasDateRangeError && (FromDate.HasValue || ToDate.HasValue))
            {
                long min = FromDate?.Ticks ?? long.MinValue;
                long max = ToDate?.Ticks   ?? long.MaxValue;
                var range = NumericRangeQuery.NewInt64Range("FileTimestamp", min, max, true, true);
                finalQuery = new BooleanQuery
                {
                    { tq,    Occur.MUST },
                    { range, Occur.MUST }
                };
            }

            var topDocs = _searcher.Search(finalQuery, _logs.Count * 1000);
            foreach (var sd in topDocs.ScoreDocs)
            {
                var doc        = _searcher.Doc(sd.Doc);
                var path       = doc.Get("FullPath");
                var lineNumber = doc.GetField("LineNumber").GetInt32Value() ?? 0;
                var rawLine    = doc.Get("ContentSubstr");

                var lf    = _logs.First(l => l.Path == path);
                var entry = lf.Entries.First(e => e.LineNumber == lineNumber && e.RawLine == rawLine);
                var cmp   = IsCaseSensitive 
                          ? StringComparison.Ordinal 
                          : StringComparison.OrdinalIgnoreCase;

                Hits.Add(new HitViewModel(lf, entry, SearchQuery, cmp));
            }

            foreach (var grp in Hits.GroupBy(h => h.FullPath))
                FileHitGroups.Add(new FileHitGroupViewModel(grp.Key, grp));

            if (FileHitGroups.Count > 0)
                SelectedHitGroup = FileHitGroups[0];
        }

        [RelayCommand(CanExecute = nameof(CanOpenExternal))]
        private void OpenExternal()
        {
            if (SelectedHit is null) return;
            var cmd  = Settings.ExternalOpener;
            var args = $"+{SelectedHit.LineNumber} \"{SelectedHit.FullPath}\"";
            Process.Start(new ProcessStartInfo(cmd, args) { UseShellExecute = true });
        }

        [RelayCommand]
        private void OpenHit(HitViewModel? hit)
        {
            if (hit is null) return;
            var path = hit.FullPath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Process.Start("xdg-open", path);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", path);
            else
                throw new PlatformNotSupportedException();
        }
    }
}

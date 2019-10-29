using Ookii.Dialogs.Wpf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

#pragma warning disable RCS1031

namespace JSON_Viewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<JsonContainer> Items { get; set; } = new ObservableCollection<JsonContainer>();

        public SearchState SearchState { get; set; } = new SearchState();
        public string SelectedPath { get; set; } = "";
        public bool IsDarkThemed { get; set; }
        public bool AutoSearch { get; set; }

        public int UsedMemoryMB { get; set; }
        public string Status { get; set; } = "";
        public bool IsLoading { get; set; }

        private readonly DebounceDispatcher QueryDebouncer = new DebounceDispatcher();
        private readonly WeakReference<JsonContainer> PreviousMatchedElement = new WeakReference<JsonContainer>(null);
        private readonly DispatcherTimer MemoryTimer;

        private readonly ResourceDictionary LightDic = new ResourceDictionary { Source = new Uri("pack://application:,,,/Themes/Light.xaml") };
        private readonly ResourceDictionary DarkDic = new ResourceDictionary { Source = new Uri("pack://application:,,,/Themes/Dark.xaml") };

        private Configuration Config;
        private JsonDocument CurrentDocument;
        private JsonContainer RootContainer;
        private bool HasUpdatedSearch; //Dirty hack
        private int MemoryTimerCounter;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            LoadConfig();

            InitializeComponent();

            this.DataContext = this;

            MemoryTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.DataBind, MemoryTimer_Elapsed, Dispatcher);
            MemoryTimer.Start();
        }

        private void LoadConfig()
        {
            Config = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
            AutoSearch = bool.Parse(Config.AppSettings.Settings["AutoSearch"].Value);

            if (!AutoSearch)
                HasUpdatedSearch = true;

            string theme = Config.AppSettings.Settings["Theme"].Value;

            if (theme == "Light" || theme == null)
                SetLightTheme();
            else if (theme == "Dark")
                SetDarkTheme();
            else
                throw new Exception("Invalid theme");
        }

        private void MemoryTimer_Elapsed(object sender, EventArgs e)
        {
            if (!this.IsActive)
                return;

            UsedMemoryMB = (int)(GC.GetTotalMemory(MemoryTimerCounter++ % 3 == 0) / (1024 * 1024));
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
#if DEBUG
            await Load("data.json");
#else
            var args = Environment.GetCommandLineArgs();

            if (args.Length == 2)
                await Load(args[1]);
#endif
        }

        private async Task Load(string path)
        {
            using var file = File.OpenRead(path);

            if (file.Length > 50 * 1024 * 1024)
                AutoSearch = false; //Automatically disable auto search for files bigger than 50MB

            await Load(file);
        }

        private async Task Load(Stream data)
        {
            var sw = Stopwatch.StartNew();

            SearchState.Reset();
            Items.Clear();

            this.Cursor = Cursors.AppStarting;
            Status = "Loading...";
            IsLoading = true;

            CurrentDocument?.Dispose();
            CurrentDocument = await JsonDocument.ParseAsync(data);
            RootContainer = new JsonContainer(CurrentDocument.RootElement, "");

            Items.Add(RootContainer);

            this.Cursor = null;
            Status = $"Loaded {(float)data.Length / 1024 / 1024:0.0}MB of data in {sw.Elapsed}";
            IsLoading = false;

            Dispatcher.InvokeDelayed(4000, () => Status = "");
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string path = ((TreeViewItem)Tree.SelectedItem).Tag.ToString();

            Clipboard.SetText(JsonSerializer.Serialize(CurrentDocument.RootElement.Query(path)));
        }

        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaOpenFileDialog();
            
            if (dialog.ShowDialog() == true)
            {
                await Load(dialog.FileName);
            }
        }

        private void Query_Changed(object sender, TextChangedEventArgs e)
        {
            SearchState.Reset();
            UpdateSearchDebounce(sender, e);
        }

        private void UpdateSearchDebounce(object sender, EventArgs e)
        {
            if (AutoSearch)
                QueryDebouncer.Debounce(500, async _ => await UpdateSearch());
        }

        private void ExpandTo(string path)
        {
            string currPath = "";
            var currentContainer = RootContainer;

            RootContainer.IsExpanded = true;

            for (int i = 0; i < path.Length; i++)
            {
                char c = path[i];

                if (c == '.')
                {
                    Check();
                }

                currPath += c;
            }

            Check(true);

            void Check(bool highlight = false)
            {
                foreach (JsonContainer item in currentContainer.Children)
                {
                    if (item.Path == currPath)
                    {
                        item.IsExpanded = true;

                        if (highlight)
                        {
                            if (PreviousMatchedElement.TryGetTarget(out var el))
                                el.IsSelected = false;

                            item.IsSelected = true;

                            PreviousMatchedElement.SetTarget(item);
                        }

                        currentContainer = item;

                        break;
                    }
                }
            }
        }

        private async Task UpdateSearch()
        {
            if (!HasUpdatedSearch)
            {
                HasUpdatedSearch = true;
                return;
            }

            if (CurrentDocument == null)
            {
                SearchState.Reset();
                return;
            }

            var elementStack = new Stack<(JsonElement Element, object Key)>();
            var foundPaths = new List<string>();

            bool done = false;

            Status = "Searching...";
            Dispatcher.InvokeDelayed(250, () => { if (!done) IsLoading = true; });

            await Task.Run(() => SearchIn(CurrentDocument.RootElement));

            done = true;

            Status = null;
            IsLoading = false;

            SearchState.FoundPaths = foundPaths.ToArray();
            SearchState.CurrentMatchIndex = 0;

            if (foundPaths.Count > 0)
                ExpandTo(foundPaths[0]);

            void MatchFound()
            {
                var path = new StringBuilder();

                foreach (var item in elementStack.Reverse())
                {
                    if (item.Key is int)
                        path.Append(".[").Append(item.Key).Append("]");
                    else if (item.Key != null)
                        path.Append(".").Append(item.Key);
                }

                foundPaths.Add(path.ToString());
            }

            bool SearchString(string str) => SearchState.Query == null ? false : SearchState.RegexQuery
                    ? Regex.IsMatch(str, SearchState.Query)
                    : str.Contains(SearchState.Query);

            void SearchIn(JsonElement element, object key = null)
            {
                elementStack.Push((element, key));

                if (element.ValueKind == JsonValueKind.Array)
                {
                    int i = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        SearchIn(item, i++);
                    }
                }
                else if (element.ValueKind == JsonValueKind.Object)
                {
                    foreach (var item in element.EnumerateObject())
                    {
                        if (SearchState.SearchInNames && SearchString(item.Name))
                        {
                            elementStack.Push((element, item.Name));

                            MatchFound();

                            elementStack.Pop();
                        }

                        SearchIn(item.Value, item.Name);
                    }
                }
                else if (SearchState.SearchInValues
                     && ((element.ValueKind == JsonValueKind.String && SearchString(element.GetString()))
                     || SearchString(element.ToString())))
                {
                    MatchFound();
                }

                elementStack.Pop();
            }
        }

        private void NextMatch_Click(object sender, RoutedEventArgs e)
        {
            if (SearchState.CanGoToNextMatch)
            {
                SearchState.CurrentMatchIndex++;
                ExpandTo(SearchState.FoundPaths[SearchState.CurrentMatchIndex]);
            }
        }

        private void PreviousMatch_Click(object sender, RoutedEventArgs e)
        {
            if (SearchState.CanGoToPreviousMatch)
            {
                SearchState.CurrentMatchIndex--;
                ExpandTo(SearchState.FoundPaths[SearchState.CurrentMatchIndex]);
            }
        }

        private void TextBlock_Initialized(object sender, EventArgs e)
        {

        }

        private void FrameworkElement_Initialized(object sender, EventArgs e)
        {

        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            if (PreviousMatchedElement.TryGetTarget(out var j))
            {
                j.IsSelected = false;
                PreviousMatchedElement.SetTarget(null);
            }

            SearchState.Reset();
        }

        private void SetDarkTheme()
        {
            IsDarkThemed = true;
            Config.AppSettings.Settings["Theme"].Value = "Dark";
            Config.Save();

            Resources.MergedDictionaries.Remove(LightDic);
            Resources.MergedDictionaries.Add(DarkDic);
        }

        private void Dark_Checked(object sender, RoutedEventArgs e) => SetDarkTheme();

        private void SetLightTheme()
        {
            IsDarkThemed = false;
            Config.AppSettings.Settings["Theme"].Value = "Light";
            Config.Save();

            Resources.MergedDictionaries.Remove(DarkDic);
            Resources.MergedDictionaries.Add(LightDic);
        }

        private void Dark_Unchecked(object sender, RoutedEventArgs e) => SetLightTheme();

        private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedPath = (e.NewValue as JsonContainer)?.Path;
        }

        private void CopyPath_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(SelectedPath);
        }

        private void UpdateAutoSearch(object sender, RoutedEventArgs e)
        {
            Config.AppSettings.Settings["AutoSearch"].Value = AutoSearch.ToString();
            Config.Save();
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            await UpdateSearch();
        }

        private async void Query_KeyDown(object sender, KeyEventArgs e)
        {
            if (!AutoSearch && e.Key == Key.Return)
                await UpdateSearch();
        }
    }
}

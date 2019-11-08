﻿using JSON_Viewer.Themes;
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
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; set; } = new MainViewModel();

        private readonly DebounceDispatcher QueryDebouncer = new DebounceDispatcher();
        private readonly WeakReference<JsonContainer> PreviousMatchedElement = new WeakReference<JsonContainer>(null);
        private readonly DispatcherTimer MemoryTimer;

        private Configuration Config;
        private JsonDocument CurrentDocument;
        private JsonContainer RootContainer;
        private bool HasUpdatedSearch; //Dirty hack
        private int MemoryTimerCounter;

        public MainWindow()
        {
            InitializeComponent();

            LoadConfig();

            this.DataContext = ViewModel;

            MemoryTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.DataBind, MemoryTimer_Elapsed, Dispatcher);
            MemoryTimer.Start();
        }

        private void LoadConfig()
        {
            Config = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
            ViewModel.AutoSearch = bool.Parse(Config.AppSettings.Settings["AutoSearch"].Value);

            if (!ViewModel.AutoSearch)
                HasUpdatedSearch = true;

            string theme = Config.AppSettings.Settings["Theme"].Value;

            Application.Current.Resources.MergedDictionaries.Clear(); //Remove merged dictionary used for designer

            ViewModel.ThemeManager = new ThemeManager(Application.Current.Resources, Dispatcher);

            var configTheme = ViewModel.ThemeManager.Themes.SingleOrDefault(o => o.Name == theme);

            ViewModel.ThemeManager.CurrentTheme = configTheme ?? ViewModel.ThemeManager.Themes[0];
        }

        private void MemoryTimer_Elapsed(object sender, EventArgs e)
        {
            if (!this.IsActive)
                return;

            ViewModel.UsedMemoryMB = (int)(GC.GetTotalMemory(MemoryTimerCounter++ % 3 == 0) / (1024 * 1024));
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
                ViewModel.AutoSearch = false; //Automatically disable auto search for files bigger than 50MB

            await Load(file);
        }

        private async Task Load(Stream data)
        {
            var sw = Stopwatch.StartNew();

            ViewModel.SearchState.Reset();
            ViewModel.Items.Clear();

            this.Cursor = Cursors.AppStarting;
            ViewModel.Status = "Loading...";
            ViewModel.IsLoading = true;

            CurrentDocument?.Dispose();

            try
            {
                CurrentDocument = await JsonDocument.ParseAsync(data);
            }
            catch (JsonException ex)
            {
                MessageBox.Show("Failed to read JSON file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                this.Cursor = null;
                ViewModel.Status = "";
                ViewModel.IsLoading = false;
                return;
            }

            RootContainer = new JsonContainer(CurrentDocument.RootElement, "");

            ViewModel.Items.Add(RootContainer);

            this.Cursor = null;
            ViewModel.Status = $"Loaded {(float)data.Length / 1024 / 1024:0.0}MB of data in {sw.Elapsed}";
            ViewModel.IsLoading = false;

            Dispatcher.InvokeDelayed(4000, () => ViewModel.Status = "");
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string path = ((TreeViewItem)Tree.SelectedItem).Tag.ToString();

            Clipboard.SetText(JsonSerializer.Serialize(CurrentDocument.RootElement.Query(path)));
        }

        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaOpenFileDialog();
            dialog.Filter = "JSON Files|*.json|All files|*.*";
            
            if (dialog.ShowDialog() == true)
            {
                await Load(dialog.FileName);
            }
        }

        private void Query_Changed(object sender, TextChangedEventArgs e)
        {
            ViewModel.SearchState.Reset();
            UpdateSearchDebounce(sender, e);
        }

        private void UpdateSearchDebounce(object sender, EventArgs e)
        {
            if (ViewModel.AutoSearch)
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
                ViewModel.SearchState.Reset();
                return;
            }

            var elementStack = new Stack<(JsonElement Element, object Key)>();
            var foundPaths = new List<string>();

            bool done = false;

            ViewModel.Status = "Searching...";
            Dispatcher.InvokeDelayed(250, () => { if (!done) ViewModel.IsLoading = true; });

            await Task.Run(() => SearchIn(CurrentDocument.RootElement));

            done = true;

            ViewModel.Status = null;
            ViewModel.IsLoading = false;

            ViewModel.SearchState.FoundPaths = foundPaths.ToArray();
            ViewModel.SearchState.CurrentMatchIndex = 0;

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

            bool SearchString(string str) => ViewModel.SearchState.Query == null ? false : ViewModel.SearchState.RegexQuery
                    ? Regex.IsMatch(str, ViewModel.SearchState.Query)
                    : str.Contains(ViewModel.SearchState.Query);

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
                        if (ViewModel.SearchState.SearchInNames && SearchString(item.Name))
                        {
                            elementStack.Push((element, item.Name));

                            MatchFound();

                            elementStack.Pop();
                        }

                        SearchIn(item.Value, item.Name);
                    }
                }
                else if (ViewModel.SearchState.SearchInValues
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
            if (ViewModel.SearchState.CanGoToNextMatch)
            {
                ViewModel.SearchState.CurrentMatchIndex++;
                ExpandTo(ViewModel.SearchState.FoundPaths[ViewModel.SearchState.CurrentMatchIndex]);
            }
        }

        private void PreviousMatch_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SearchState.CanGoToPreviousMatch)
            {
                ViewModel.SearchState.CurrentMatchIndex--;
                ExpandTo(ViewModel.SearchState.FoundPaths[ViewModel.SearchState.CurrentMatchIndex]);
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

            ViewModel.SearchState.Reset();
        }

        private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ViewModel.SelectedPath = (e.NewValue as JsonContainer)?.Path;
        }

        private void CopyPath_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ViewModel.SelectedPath);
        }

        private void UpdateAutoSearch(object sender, RoutedEventArgs e)
        {
            Config.AppSettings.Settings["AutoSearch"].Value = ViewModel.AutoSearch.ToString();
            Config.Save();
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            await UpdateSearch();
        }

        private async void Query_KeyDown(object sender, KeyEventArgs e)
        {
            if (!ViewModel.AutoSearch && e.Key == Key.Return)
                await UpdateSearch();
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.Length > 0)
                    await Load(files[0]);
            }
        }

        private void ExpandAll_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ExpandAll_Click(object sender, MouseButtonEventArgs e)
        {
            ViewModel.Status = "This may take a while...";
            this.UpdateLayout();
            
            using (Dispatcher.DisableProcessing())
            {
                ExpandAll(RootContainer);
            }

            ViewModel.Status = null;

            static void ExpandAll(JsonContainer c)
            {
                c.IsExpanded = true;

                foreach (var item in c.Children)
                {
                    ExpandAll(item);
                }
            }
        }

        private void Themes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Config.AppSettings.Settings["Theme"].Value = ViewModel.ThemeManager.CurrentTheme.Name;
            Config.Save();
        }

        private async void New_Click(object sender, RoutedEventArgs e)
        {
            string json = JsonInputWindow.ShowAsDialog();
            
            if (json != null)
            {
                using var mem = new MemoryStream(Encoding.UTF8.GetBytes(json));
                await Load(mem);
            }
        }

        private async void ExecuteQuery_Click(object sender, RoutedEventArgs e)
        {
            var results = await JsonQueryExecutor.RunQuery(RootContainer, ViewModel.QueryPretty);

            ViewModel.Items.Clear();

            if (results is JsonContainer elem)
            {
                ViewModel.Items.Add(elem);
            }
            else if (results is IEnumerable<JsonContainer> elems)
            {
                foreach (var item in elems)
                {
                    ViewModel.Items.Add(item);
                }
            }
        }
    }
}

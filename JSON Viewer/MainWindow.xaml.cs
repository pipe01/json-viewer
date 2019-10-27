using Ookii.Dialogs.Wpf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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

#pragma warning disable RCS1031

namespace JSON_Viewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<JsonContainer> Items { get; set; } = new ObservableCollection<JsonContainer>();

        public SearchState SearchState { get; set; } = new SearchState();

        private readonly DebounceDispatcher QueryDebouncer = new DebounceDispatcher();
        private readonly WeakReference<JsonContainer> PreviousMatchedElement = new WeakReference<JsonContainer>(null);

        private JsonDocument CurrentDocument;
        private JsonContainer RootContainer;
        private bool HasUpdatedSearch; //Dirty hack

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
#if DEBUG
            await Load("data.json");
#endif
        }

        private async Task Load(string path)
        {
            using var file = File.OpenRead(path);
            await Load(file);
        }

        private async Task Load(Stream data)
        {
            SearchState.Reset();
            Items.Clear();

            CurrentDocument = await JsonDocument.ParseAsync(data);
            RootContainer = new JsonContainer(CurrentDocument.RootElement, "");

            Items.Add(RootContainer);
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

        private void UpdateSearchDebounce(object sender, TextChangedEventArgs e) => UpdateSearchDebounce(sender, (EventArgs)e);
        private void UpdateSearchDebounce(object sender, EventArgs e)
        {
            QueryDebouncer.Debounce(500, _ => UpdateSearch());
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
                            item.IsSelected = true;

                            if (PreviousMatchedElement.TryGetTarget(out var el))
                                el.IsSelected = false;

                            PreviousMatchedElement.SetTarget(item);
                        }

                        currentContainer = item;
                    }
                }
            }
        }

        private void UpdateSearch()
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

            SearchIn(CurrentDocument.RootElement);

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
    }
}

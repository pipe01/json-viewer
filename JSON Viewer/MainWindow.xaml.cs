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
        public ObservableCollection<TreeViewItem> Items { get; set; } = new ObservableCollection<TreeViewItem>();

        public SearchState SearchState { get; set; } = new SearchState();

        private readonly ContextMenu ItemMenu;
        private readonly DebounceDispatcher QueryDebouncer = new DebounceDispatcher();

        private JsonDocument CurrentDocument;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;
            this.ItemMenu = FindResource(nameof(ItemMenu)) as ContextMenu;
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
            Items.Clear();
            Items.Add(new TreeViewItem { Header = "Loading..." });

            CurrentDocument = await JsonDocument.ParseAsync(data);
            AddToken(CurrentDocument.RootElement, Items);

            Items.RemoveAt(0); //Remove "Loading" item

            void AddToken(JsonElement t, IList items, object name = null, string path = "")
            {
                var item = new TreeViewItem();

                if (LoadItem(false))
                {
                    item.Items.Add(null);

                    item.Expanded += delegate
                    {
                        LoadItem(true);
                    };
                }

                item.ContextMenu = ItemMenu;
                items.Add(item);

                //Returns true if the token has children
                bool LoadItem(bool loadChildren)
                {
                    bool hasChildren = false;

                    loadChildren = loadChildren && item.Items.Count == 1 && item.Items[0] == null;

                    switch (t.ValueKind)
                    {
                        case JsonValueKind.Array:
                            var arrLen = t.GetArrayLength();
                            item.Header = $"[{arrLen}]";

                            if (loadChildren)
                            {
                                int i = 0;
                                foreach (var arrItem in t.EnumerateArray())
                                {
                                    AddToken(arrItem, item.Items, i, path + $".[{i}]");
                                    i++;
                                }
                            }

                            hasChildren = arrLen > 0;
                            break;
                        case JsonValueKind.Object:
                            item.Header = "{...}";

                            if (loadChildren)
                            {
                                foreach (var objItem in t.EnumerateObject())
                                {
                                    AddToken(objItem.Value, item.Items, objItem.Name, path + $".{objItem.Name}");
                                }
                            }

                            hasChildren = t.EnumerateObject().MoveNext();
                            break;
                        default:
                            item.Header = JsonSerializer.Serialize(t);
                            break;
                    }

                    if (name != null)
                        item.Header = (name is int ? name : $"\"{name}\"") + ":   " + item.Header;

                    if (loadChildren)
                        item.Items.RemoveAt(0);

                    item.Tag = path;
                    return hasChildren;
                }
            }
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

        private void Query_TextChanged(object sender, TextChangedEventArgs e)
        {
            QueryDebouncer.Debounce(1000, _ =>
            {
                SearchState.Query = ((TextBox)sender).Text;
                UpdateSearch();
            });
        }

        private void ExpandTo(string path)
        {
            string currPath = "";
            ItemsControl currentContainer = Tree;
            char c = default;

            for (int i = 0; i < path.Length; i++)
            {
                c = path[i];

                if (c == '.')
                {
                    Check();
                }
                else
                {
                    currPath += c;
                }
            }

            Check(true);

            void Check(bool highlight = false)
            {
                var node = currentContainer.Items.Cast<TreeViewItem>().SingleOrDefault(o => o.Tag is string str && str == currPath);

                if (node != null)
                {
                    currentContainer = node;
                    node.IsExpanded = true;

                    currPath += c;

                    if (highlight)
                        node.IsSelected = true;
                }
            }
        }

        private void UpdateSearch()
        {
            var elementStack = new Stack<(JsonElement Element, object Key)>();
            var foundPaths = new List<string>();

            SearchIn(CurrentDocument.RootElement);

            SearchState.FoundPaths = foundPaths.ToArray();
            ExpandTo(foundPaths[0]);

            void MatchFound()
            {
                var path = new StringBuilder();

                foreach (var item in elementStack.Reverse())
                {
                    if (item.Key is int)
                        path.Append(".[").Append(item.Key).Append("]");
                    else
                        path.Append(".").Append(item.Key);
                }

                foundPaths.Add(path.ToString());
            }

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
                        if (SearchState.SearchInNames && item.Name.Contains(SearchState.Query))
                        {
                            elementStack.Push((element, item.Name));

                            MatchFound();

                            elementStack.Pop();
                        }
                        else
                        {
                            SearchIn(item.Value, item.Name);
                        }
                    }
                }
                else if (SearchState.SearchInValues
                     && ((element.ValueKind == JsonValueKind.String && element.GetString().Contains(SearchState.Query))
                     || element.ToString().Contains(SearchState.Query)))
                {
                    MatchFound();
                }

                elementStack.Pop();
            }
        }
    }
}

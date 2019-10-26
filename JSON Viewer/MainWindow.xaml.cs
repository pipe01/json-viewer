using Ookii.Dialogs.Wpf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private readonly ContextMenu ItemMenu;

        private JsonDocument CurrentDocument;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;
            this.ItemMenu = FindResource(nameof(ItemMenu)) as ContextMenu;
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            await Load("data.json");
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

            Items.RemoveAt(0);

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
    }
}

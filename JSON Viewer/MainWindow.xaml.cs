using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
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

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;
            this.ItemMenu = FindResource(nameof(ItemMenu)) as ContextMenu;

            var testData = new WebClient().DownloadString("https://stats2.u.gg/lol/1.1/matchups/9_21/ranked_solo_5x5/84/1.2.6.json");

            var token = JToken.Parse(testData);
            AddToken(token, Items);

            void AddToken(JToken t, IList items, string name = null)
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

                    switch (t)
                    {
                        case JArray arr:
                            item.Header = $"[{arr.Count}]";

                            if (loadChildren)
                            {
                                int i = 0;
                                foreach (var arrItem in arr)
                                {
                                    AddToken(arrItem, item.Items, (i++).ToString());
                                }
                            }

                            hasChildren = arr.Count > 0;
                            break;
                        case JObject obj:
                            item.Header = "{...}";

                            if (loadChildren)
                            {
                                foreach (var objItem in obj)
                                {
                                    AddToken(objItem.Value, item.Items, objItem.Key);
                                }
                            }

                            hasChildren = obj.Count > 0;
                            break;
                        case JValue val:
                            item.Header = JsonConvert.SerializeObject(val.Value);
                            break;
                    }

                    if (name != null)
                        item.Header = $"\"{name}\":   " + item.Header;

                    if (loadChildren)
                        item.Items.RemoveAt(0);

                    return hasChildren;
                }
            }
        }
    }
}

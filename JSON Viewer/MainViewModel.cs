using JSON_Viewer.Themes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace JSON_Viewer
{
    public class MainViewModel : DependencyObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<TabViewModel> Tabs { get; set; } = new ObservableCollection<TabViewModel>();

        private const string QueryPrefix = "";
        public string QueryPretty
        {
            get => QueryPrefix + Query;
            set
            {
                if (value.Length > QueryPrefix.Length)
                    Query = value.Substring(QueryPrefix.Length);
                else
                    Query = "";

                Debug.WriteLine(Query);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(QueryPrefix)));
            }
        }
        public string Query { get; set; }

        public ThemeManager ThemeManager { get; set; }

        public int SelectedTabIndex { get; set; }
        public TabViewModel SelectedTab
        {
            get => SelectedTabIndex < 0 ? (Tabs.Count > 0 ? Tabs[0] : null) : Tabs[SelectedTabIndex];
            set => SelectedTabIndex = Tabs.IndexOf(value);
        }

        public SearchState SearchState => SelectedTab.SearchState;

        public int UsedMemoryMB { get; set; }
        public string Status { get; set; } = "";
        public bool IsLoading { get; set; }
    }
}

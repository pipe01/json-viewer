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

        public ObservableCollection<JsonContainer> Items { get; set; } = new ObservableCollection<JsonContainer>();

        public SearchState SearchState { get; set; } = new SearchState();
        public string SelectedPath { get; set; } = "";
        public bool AutoSearch { get; set; }

        private const string QueryPrefix = "root => ";
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

        public int UsedMemoryMB { get; set; }
        public string Status { get; set; } = "";
        public bool IsLoading { get; set; }
    }
}

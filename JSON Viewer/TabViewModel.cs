using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;

namespace JSON_Viewer
{
    public class TabViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string TabName { get; set; }
        public string FilePath { get; set; }

        public ObservableCollection<JsonContainer> Items { get; set; } = new ObservableCollection<JsonContainer>();

        public JsonDocument CurrentDocument { get; set; }
        public JsonContainer RootContainer { get; set; }

        public SearchState SearchState { get; set; } = new SearchState();
        public string SelectedPath { get; set; } = "";
        public bool AutoSearch { get; set; }
    }
}

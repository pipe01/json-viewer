using System.ComponentModel;

namespace JSON_Viewer
{
    public class SearchState : INotifyPropertyChanged
    {
        public string Query { get; set; }

        public bool SearchInNames { get; set; } = true;
        public bool SearchInValues { get; set; } = true;

        public string[] FoundPaths { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

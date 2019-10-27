using System.ComponentModel;

namespace JSON_Viewer
{
    public class SearchState : INotifyPropertyChanged
    {
        public string Query { get; set; }

        public bool SearchInNames { get; set; } = true;
        public bool SearchInValues { get; set; } = true;
        public bool RegexQuery { get; set; }

        public string[] FoundPaths { get; set; }
        public int CurrentMatchIndex { get; set; }


        public bool CanGoToNextMatch => FoundPaths != null && CurrentMatchIndex < FoundPaths.Length - 1;
        public bool CanGoToPreviousMatch => FoundPaths != null && CurrentMatchIndex > 0;

        public int CurrentMatchIndexPlusOne => CurrentMatchIndex + 1;
        public bool AnyMatches => FoundPaths?.Length > 0;


        public event PropertyChangedEventHandler PropertyChanged;

        public void Reset()
        {
            FoundPaths = null;
            CurrentMatchIndex = 0;
        }
    }
}

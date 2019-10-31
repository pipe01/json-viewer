using System.Windows;

namespace JSON_Viewer.Themes
{
    public class Theme
    {
        public string Name { get; }
        public bool BuiltIn { get; }
        public ResourceDictionary Resources { get; }

        public string WpfName => Name + (BuiltIn ? " (built-in)" : null);

        public Theme(string name, bool builtIn, ResourceDictionary resources)
        {
            this.Name = name;
            this.BuiltIn = builtIn;
            this.Resources = resources;
        }
    }
}

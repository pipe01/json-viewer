using System.Windows;

namespace JSON_Viewer.Themes
{
    public class Theme
    {
        public string Name { get; }
        public string Path { get; }
        public ResourceDictionary Resources { get; set; }

        public bool BuiltIn => Path == null;
        public string WpfName => Name + (BuiltIn ? " (built-in)" : null);

        public Theme(string name, string path, ResourceDictionary resources)
        {
            this.Name = name;
            this.Path = path;
            this.Resources = resources;
        }
    }
}

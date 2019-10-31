using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xaml;

namespace JSON_Viewer.Themes
{
    public class ThemeManager : INotifyPropertyChanged
    {
        private static readonly string ThemeFolder = Path.GetFullPath("./themes");

        public ObservableCollection<Theme> Themes { get; set; } = new ObservableCollection<Theme>();

        private Theme _currentTheme;
        public Theme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                this._currentTheme = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTheme)));

                ApplyTheme(value);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly ResourceDictionary LightTheme = new ResourceDictionary { Source = new Uri("pack://application:,,,/Themes/Light.xaml") };
        private readonly ResourceDictionary DarkTheme = new ResourceDictionary { Source = new Uri("pack://application:,,,/Themes/Dark.xaml") };

        private readonly IDictionary<string, DebounceDispatcher> Debouncers = new Dictionary<string, DebounceDispatcher>();

        private readonly ResourceDictionary Resources;
        private readonly Dispatcher Dispatcher;
        private readonly FileSystemWatcher Watcher;

        public ThemeManager(ResourceDictionary resources, Dispatcher dispatcher)
        {
            this.Resources = resources;
            this.Dispatcher = dispatcher;

            Themes.Add(new Theme("Light", null, LightTheme));
            Themes.Add(new Theme("Dark", null, DarkTheme));

            LoadAllThemes();

            Watcher = new FileSystemWatcher(ThemeFolder, "*.xaml");
            Watcher.Changed += this.Watcher_Changed;
            Watcher.EnableRaisingEvents = true;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            var debouncer = Debouncers.TryGetValue(e.FullPath, out var d) ? d : Debouncers[e.FullPath] = new DebounceDispatcher();

            debouncer.Debounce(100, _ =>
            {
                Debouncers.Remove(e.FullPath);

                string fileName = Path.GetFileNameWithoutExtension(e.FullPath);
                var theme = Themes.SingleOrDefault(o => o.Name == fileName);

                if (theme != null)
                {
                    if (TryLoadResources(WebUtility.UrlDecode(theme.Resources.Source.AbsolutePath), theme.Name, out var r))
                        theme.Resources = r;
                    else
                        return;

                    if (theme == CurrentTheme)
                    {
                        Resources.MergedDictionaries.Remove(theme.Resources);

                        ApplyTheme(theme);
                    }
                }
                else
                {
                    LoadTheme(e.FullPath);
                }
            }, disp: Dispatcher);
        }

        private void LoadAllThemes()
        {
            if (!Directory.Exists(ThemeFolder))
            {
                Directory.CreateDirectory(ThemeFolder);
                return;
            }

            foreach (var theme in Directory.EnumerateFiles(ThemeFolder, "*.xaml"))
            {
                LoadTheme(theme);
            }
        }

        private Theme LoadTheme(string path, bool add = true)
        {
            string name = Path.GetFileNameWithoutExtension(path);

            if (!TryLoadResources(path, name, out var resources))
                return null;

            var theme = new Theme(name, path, resources);

            if (add)
                Themes.Add(theme);

            return theme;
        }

        private void ApplyTheme(Theme theme)
        {
            bool remove = Resources.MergedDictionaries.Count > 0;
            Resources.MergedDictionaries.Add(theme.Resources);

            if (remove)
            {
                Resources.MergedDictionaries.RemoveAt(0);
            }
        }

        private static bool TryLoadResources(string path, string themeName, out ResourceDictionary resources)
        {
            try
            {
                resources = new ResourceDictionary { Source = new Uri(path, UriKind.Absolute) };
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while loading the theme {themeName} ({ex.GetType().Name}):\n{ex.Message}", "Theme error", MessageBoxButton.OK, MessageBoxImage.Warning);
                resources = null;
                return false;
            }
        }
    }
}

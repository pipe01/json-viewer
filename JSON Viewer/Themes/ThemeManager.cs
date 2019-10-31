using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

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

        private readonly ResourceDictionary Resources;
        private readonly Dispatcher Dispatcher;
        private readonly FileSystemWatcher Watcher;

        public ThemeManager(ResourceDictionary resources, Dispatcher dispatcher)
        {
            this.Resources = resources;
            this.Dispatcher = dispatcher;

            Themes.Add(new Theme("Light", null, LightTheme));
            Themes.Add(new Theme("Dark", null, DarkTheme));

            foreach (var item in GetAllThemes())
            {
                Themes.Add(item);
            }

            Watcher = new FileSystemWatcher(ThemeFolder, "*.xaml");
            Watcher.Changed += this.Watcher_Changed;
            Watcher.EnableRaisingEvents = true;
        }

        private async void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            string fileName = Path.GetFileNameWithoutExtension(e.FullPath);
            var theme = Themes.SingleOrDefault(o => o.Name == fileName);
            
            if (theme != null)
            {
                await Task.Delay(100); //Wait for the program that's writing to finish

                Dispatcher.Invoke(() =>
                {
                    theme.Resources = new ResourceDictionary { Source = theme.Resources.Source };

                    if (theme == CurrentTheme)
                    {
                        Resources.MergedDictionaries.Remove(theme.Resources);

                        ApplyTheme(theme);
                    }
                });
            }
        }

        private IEnumerable<Theme> GetAllThemes()
        {
            if (!Directory.Exists(ThemeFolder))
            {
                Directory.CreateDirectory(ThemeFolder);
                yield break;
            }

            foreach (var theme in Directory.EnumerateFiles(ThemeFolder, "*.xaml"))
            {
                yield return new Theme(Path.GetFileNameWithoutExtension(theme), theme,
                    new ResourceDictionary { Source = new Uri(theme, UriKind.Absolute) });
            }
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
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace JSON_Viewer.Themes
{
    public static class ThemeManager
    {
        private static readonly string ThemeFolder = Path.GetFullPath("./themes");

        public static IEnumerable<Theme> LoadAllThemes()
        {
            if (!Directory.Exists(ThemeFolder))
            {
                Directory.CreateDirectory(ThemeFolder);
                yield break;
            }

            foreach (var theme in Directory.EnumerateFiles(ThemeFolder, "*.xaml"))
            {
                yield return new Theme(Path.GetFileNameWithoutExtension(theme), false,
                    new ResourceDictionary { Source = new Uri(theme, UriKind.Absolute) });
            }
        }
    }
}

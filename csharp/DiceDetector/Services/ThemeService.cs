using DiceDetector.Models;
using DiceDetector.Services.Interfaces;
using System.Windows;

namespace DiceDetector.Services
{
    public class ThemeService : IThemeService
    {
        private AppTheme _currentTheme = AppTheme.Dark;

        public AppTheme CurrentTheme => _currentTheme;

        public void SetTheme(AppTheme theme)
        {
            if (_currentTheme == theme)
                return;

            _currentTheme = theme;

            var app = Application.Current;
            var mergedDicts = app.Resources.MergedDictionaries;

            // Find and remove the current theme dictionary (DarkTheme or LightTheme)
            for (var i = mergedDicts.Count - 1; i >= 0; i--)
            {
                var source = mergedDicts[i].Source?.OriginalString;
                if (source != null &&
                    (source.Contains("DarkTheme", StringComparison.OrdinalIgnoreCase) ||
                     source.Contains("LightTheme", StringComparison.OrdinalIgnoreCase)))
                {
                    mergedDicts.RemoveAt(i);
                }
            }

            // Insert the new theme dictionary at position 0 (before AppStyles)
            var newTheme = new ResourceDictionary
            {
                Source = new Uri($"Themes/{theme}Theme.xaml", UriKind.Relative)
            };
            mergedDicts.Insert(0, newTheme);
        }

        public void ToggleTheme()
        {
            SetTheme(_currentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
        }
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace CeMCP
{
    public class ConfigurationWindow : Window
    {
        private Frame _mainFrame;
        private ConfigPage _configPage;

        public ConfigurationWindow(McpPlugin plugin)
        {
            InitializeWindow();
            _configPage = new ConfigPage(plugin);
            _mainFrame.Content = _configPage;
            ApplyTheme();
        }

        private void InitializeWindow()
        {
            Title = "MCP Server Configuration";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanResize;

            _mainFrame = new Frame
            {
                NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden
            };

            Content = _mainFrame;
        }

        private void ApplyTheme()
        {
            bool isDarkMode = IsWindowsInDarkMode();
            
            if (isDarkMode)
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                Foreground = new SolidColorBrush(Colors.White);
                _mainFrame.Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
            }
            else
            {
                Background = new SolidColorBrush(Colors.White);
                Foreground = new SolidColorBrush(Colors.Black);
                _mainFrame.Background = new SolidColorBrush(Colors.White);
            }

            _configPage?.ApplyTheme(isDarkMode);
        }

        private static bool IsWindowsInDarkMode()
        {
            try
            {
                var value = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
                return (int)value == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
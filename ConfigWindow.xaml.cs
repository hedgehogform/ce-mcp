using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CeMCP.Models;

namespace CeMCP
{
    public partial class ConfigWindow : Window
    {
        private readonly ConfigurationModel _viewModel;
        private readonly McpPlugin _plugin;

        public ConfigWindow(McpPlugin plugin)
        {
            InitializeComponent();
            _plugin = plugin;

            _viewModel = new ConfigurationModel();
            DataContext = _viewModel;

            _viewModel.LoadFromServerConfig();
            UpdateServerStatus();

            ApplyTheme(IsWindowsInDarkMode());
        }

        private void UpdateServerStatus()
        {
            if (_plugin?.GetServerWrapper()?.IsRunning == true)
                _viewModel.ServerStatus = "Running";
            else
                _viewModel.ServerStatus = "Stopped";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.SaveToServerConfig();
                _viewModel.TestResult = "Configuration saved successfully.";
            }
            catch (Exception ex)
            {
                _viewModel.TestResult = $"Error saving configuration: {ex.Message}";
            }
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.TestResult = "Testing connection...";
            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                {
                    var response = await client.GetAsync($"{_viewModel.BaseUrl}/api/cheatengine/health");
                    _viewModel.TestResult = response.IsSuccessStatusCode
                        ? "✓ Connection successful! Server is responding."
                        : $"✗ Server responded with status: {response.StatusCode}";
                }
            }
            catch (HttpRequestException ex)
            {
                _viewModel.TestResult = $"✗ Connection failed: {ex.Message}";
            }
            catch (TaskCanceledException)
            {
                _viewModel.TestResult = "✗ Connection timed out. Server may not be running.";
            }
            catch (Exception ex)
            {
                _viewModel.TestResult = $"✗ Error testing connection: {ex.Message}";
            }
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_plugin?.GetServerWrapper()?.IsRunning == true)
                {
                    _plugin.StopServer();
                    _viewModel.TestResult = "Server stopped.";
                }
                else
                {
                    _viewModel.SaveToServerConfig();
                    _plugin?.StartServer();
                    _viewModel.TestResult = "Server started.";
                }
                UpdateServerStatus();
            }
            catch (Exception ex)
            {
                _viewModel.TestResult = $"Error: {ex.Message}";
            }
        }

        private void OpenSwaggerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var url = _viewModel.SwaggerUrl;
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                _viewModel.TestResult = $"Opening Swagger UI in browser: {url}";
            }
            catch (Exception ex)
            {
                _viewModel.TestResult = $"Error opening Swagger UI: {ex.Message}";
            }
        }

        public void ApplyTheme(bool isDarkMode)
        {
            var textBoxStyle = isDarkMode ? "DarkTextBoxStyle" : "LightTextBoxStyle";
            var buttonStyle = isDarkMode ? "DarkButtonStyle" : "LightButtonStyle";

            Background = isDarkMode ? new SolidColorBrush(Color.FromRgb(32, 32, 32)) : Brushes.White;
            MainGrid.Background = Background;

            var textBrush = new SolidColorBrush(isDarkMode ? Colors.White : Colors.Black);
            TitleTextBlock.Foreground = textBrush;
            HostLabel.Foreground = textBrush;
            PortLabel.Foreground = textBrush;
            ServerNameLabel.Foreground = textBrush;
            BaseUrlLabel.Foreground = textBrush;
            StatusLabel.Foreground = textBrush;
            BaseUrlTextBlock.Foreground = textBrush;
            TestResultTextBlock.Foreground = textBrush;

            if (FindResource(textBoxStyle) is Style tbStyle)
            {
                HostTextBox.Style = tbStyle;
                PortTextBox.Style = tbStyle;
                ServerNameTextBox.Style = tbStyle;
            }

            if (FindResource(buttonStyle) is Style btnStyle)
            {
                SaveButton.Style = btnStyle;
                TestButton.Style = btnStyle;
                StartStopButton.Style = btnStyle;
                OpenSwaggerButton.Style = btnStyle;
            }
        }

        private static bool IsWindowsInDarkMode()
        {
            try
            {
                var value = Microsoft.Win32.Registry.GetValue(
                    @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "AppsUseLightTheme", 1);
                return (int)value == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}

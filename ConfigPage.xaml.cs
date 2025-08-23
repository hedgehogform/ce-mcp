using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CeMCP.Models;

namespace CeMCP
{
    public partial class ConfigPage : Page
    {
        private ConfigurationModel _viewModel;
        private McpPlugin _plugin;

        public ConfigPage()
        {
            InitializeComponent();
            _viewModel = new ConfigurationModel();
            DataContext = _viewModel;
            
            _viewModel.LoadFromServerConfig();
            UpdateServerStatus();
        }

        public ConfigPage(McpPlugin plugin) : this()
        {
            _plugin = plugin;
            UpdateServerStatus();
        }

        private void UpdateServerStatus()
        {
            if (_plugin?.GetServerWrapper()?.IsRunning == true)
            {
                _viewModel.ServerStatus = "Running";
            }
            else
            {
                _viewModel.ServerStatus = "Stopped";
            }
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
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var response = await client.GetAsync($"{_viewModel.BaseUrl}/api/cheatengine/health");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _viewModel.TestResult = "✓ Connection successful! Server is responding.";
                    }
                    else
                    {
                        _viewModel.TestResult = $"✗ Server responded with status: {response.StatusCode}";
                    }
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
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
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

            // Apply background colors
            if (isDarkMode)
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                MainGrid.Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                
                // Set text colors for labels and textblocks
                var lightBrush = new SolidColorBrush(Colors.White);
                TitleTextBlock.Foreground = lightBrush;
                HostLabel.Foreground = lightBrush;
                PortLabel.Foreground = lightBrush;
                ServerNameLabel.Foreground = lightBrush;
                BaseUrlLabel.Foreground = lightBrush;
                StatusLabel.Foreground = lightBrush;
                BaseUrlTextBlock.Foreground = lightBrush;
                TestResultTextBlock.Foreground = lightBrush;
            }
            else
            {
                Background = new SolidColorBrush(Colors.White);
                MainGrid.Background = new SolidColorBrush(Colors.White);
                
                // Set text colors for labels and textblocks
                var darkBrush = new SolidColorBrush(Colors.Black);
                TitleTextBlock.Foreground = darkBrush;
                HostLabel.Foreground = darkBrush;
                PortLabel.Foreground = darkBrush;
                ServerNameLabel.Foreground = darkBrush;
                BaseUrlLabel.Foreground = darkBrush;
                StatusLabel.Foreground = darkBrush;
                BaseUrlTextBlock.Foreground = darkBrush;
                TestResultTextBlock.Foreground = darkBrush;
            }

            // Apply styles to controls
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
    }
}

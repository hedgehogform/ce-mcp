using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CEMCP.Models;
using CESDK;

namespace CEMCP.Views
{
    public partial class ConfigWindow : Window
    {
        private readonly ConfigurationModel _viewModel;
        private readonly McpPlugin _plugin;

        public ConfigWindow(McpPlugin plugin)
        {
            _plugin = plugin;
            _viewModel = new ConfigurationModel();
            DataContext = _viewModel;

            _viewModel.LoadFromServerConfig();

            bool isDarkMode = ThemeHelper.IsInDarkMode();
            _viewModel.IsDarkMode = isDarkMode;

            InitializeComponent();
            ApplyTheme(isDarkMode);
            UpdateServerStatus();
            Activated += (_, _) => UpdateServerStatus();
        }

        private static bool IsServerRunning(McpPlugin? plugin)
        {
            var wrapper = plugin?.GetServerWrapper();
            return wrapper is not null && wrapper.IsRunning;
        }

        private void UpdateServerStatus()
        {
            bool running = IsServerRunning(_plugin);
            _viewModel.ServerStatus = running ? "Running" : "Stopped";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.SaveToServerConfig();
                _viewModel.TestResult = "Settings saved. Start the server to apply them.";
            }
            catch (Exception ex)
            {
                _viewModel.TestResult = $"Error saving configuration: {ex.Message}";
            }
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            testButton.IsEnabled = false;
            _viewModel.TestResult = $"Testing {_viewModel.BaseUrl} ...";
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                client.DefaultRequestHeaders.Accept.ParseAdd("text/event-stream");
                using var content = new StringContent(
                    "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2025-06-18\",\"capabilities\":{},\"clientInfo\":{\"name\":\"ce-mcp-config\",\"version\":\"1.0\"}}}",
                    System.Text.Encoding.UTF8,
                    "application/json");
                using HttpResponseMessage response = await client.PostAsync(_viewModel.BaseUrl, content);
                _viewModel.TestResult = response.IsSuccessStatusCode
                    ? "Connection successful. The MCP server responded."
                    : $"Connection failed: HTTP {(int)response.StatusCode} {response.ReasonPhrase}.";
            }
            catch (HttpRequestException ex)
            {
                _viewModel.TestResult = $"Connection failed: {ex.Message}";
            }
            catch (TaskCanceledException)
            {
                _viewModel.TestResult = "Connection timed out. Start the server and verify the host and port.";
            }
            catch (Exception ex)
            {
                _viewModel.TestResult = $"Connection test failed: {ex.Message}";
            }
            finally
            {
                testButton.IsEnabled = true;
            }
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            startStopButton.IsEnabled = false;
            try
            {
                if (IsServerRunning(_plugin))
                {
                    _viewModel.ServerStatus = "Stopping";
                    _plugin.StopServer();
                    UpdateServerStatus();
                    _viewModel.TestResult = "Server stopped.";
                }
                else
                {
                    _viewModel.SaveToServerConfig();
                    _viewModel.ServerStatus = "Starting";
                    _plugin.StartServer();
                    UpdateServerStatus();
                    _viewModel.TestResult = IsServerRunning(_plugin)
                        ? $"Server listening at {_viewModel.BaseUrl}"
                        : $"Server did not start. Check the Cheat Engine console or {PluginLogger.LogFilePath}.";
                }
            }
            catch (Exception ex)
            {
                UpdateServerStatus();
                _viewModel.TestResult = $"Server action failed: {ex.Message}";
            }
            finally
            {
                startStopButton.IsEnabled = true;
            }
        }

        private void CopyUrlButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var url = _viewModel.BaseUrl;
                Clipboard.SetText(url);
                _viewModel.TestResult = $"Copied to clipboard: {url}";
            }
            catch (Exception ex)
            {
                _viewModel.TestResult = $"Error copying URL: {ex.Message}";
            }
        }

        private void ApplyTheme(bool isDarkMode)
        {
            string prefix = isDarkMode ? "Dark" : "Light";
            ThemeBrushes brushes = ResolveThemeBrushes(prefix);

            Resources["WindowBgBrush"] = brushes.Background;
            Resources["SurfaceBrush"] = brushes.Surface;
            Resources["TextBrush"] = brushes.Foreground;
            Resources["MutedTextBrush"] = brushes.MutedForeground;
            Resources["TextBoxBgBrush"] = brushes.TextBoxBackground;
            Resources["TextBoxBorderBrush"] = brushes.TextBoxBorder;
            Resources["BtnBgBrush"] = brushes.ButtonBackground;
            Resources["BtnBorderBrush"] = brushes.ButtonBorder;
            Resources["BtnDisabledBgBrush"] = brushes.ButtonDisabledBackground;
            Resources["BtnDisabledFgBrush"] = brushes.ButtonDisabledForeground;
            Resources["AccentBrush"] = brushes.Accent;
        }

        private ThemeBrushes ResolveThemeBrushes(string prefix)
        {
            return new ThemeBrushes
            {
                Background = GetBrush($"{prefix}Bg"),
                Surface = GetBrush($"{prefix}Surface"),
                Foreground = GetBrush($"{prefix}Fg"),
                MutedForeground = GetBrush($"{prefix}MutedFg"),
                TextBoxBackground = GetBrush($"{prefix}TextBoxBg"),
                TextBoxBorder = GetBrush($"{prefix}TextBoxBorder"),
                ButtonBackground = GetBrush($"{prefix}BtnBg"),
                ButtonBorder = GetBrush($"{prefix}BtnBorder"),
                ButtonDisabledBackground = GetBrush($"{prefix}BtnDisabledBg"),
                ButtonDisabledForeground = GetBrush($"{prefix}BtnDisabledFg"),
                Accent = GetBrush($"{prefix}Accent"),
            };
        }

        private SolidColorBrush GetBrush(string resourceName) =>
            new((Color)FindResource(resourceName));

        private sealed class ThemeBrushes
        {
            public required SolidColorBrush Background { get; init; }
            public required SolidColorBrush Surface { get; init; }
            public required SolidColorBrush Foreground { get; init; }
            public required SolidColorBrush MutedForeground { get; init; }
            public required SolidColorBrush TextBoxBackground { get; init; }
            public required SolidColorBrush TextBoxBorder { get; init; }
            public required SolidColorBrush ButtonBackground { get; init; }
            public required SolidColorBrush ButtonBorder { get; init; }
            public required SolidColorBrush ButtonDisabledBackground { get; init; }
            public required SolidColorBrush ButtonDisabledForeground { get; init; }
            public required SolidColorBrush Accent { get; init; }
        }
    }
}

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using CEMCP.Models;

namespace CEMCP.Views
{
    public class ConfigWindow : Window
    {
        private readonly ConfigurationModel _viewModel;
        private readonly McpPlugin _plugin;

        // UI Controls
        private TextBox hostTextBox = null!;
        private TextBox portTextBox = null!;
        private TextBox serverNameTextBox = null!;
        private Button saveButton = null!;
        private Button testButton = null!;
        private Button startStopButton = null!;
        private Button openApiButton = null!;
        private TextBlock statusTextBlock = null!;

        public ConfigWindow(McpPlugin plugin)
        {
            _plugin = plugin;
            _viewModel = new ConfigurationModel();
            DataContext = _viewModel;

            _viewModel.LoadFromServerConfig();

            bool isDarkMode = ThemeHelper.IsInDarkMode();
            _viewModel.IsDarkMode = isDarkMode;

            BuildUI();
            ApplyTheme(isDarkMode);
            UpdateServerStatus();
        }

        private void BuildUI()
        {
            // Window properties
            Title = "MCP Server Configuration";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanResize;

            // Main Grid
            var mainGrid = new Grid { Margin = new Thickness(20) };

            // Define rows
            for (int i = 0; i < 9; i++)
            {
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = i == 8 ? GridLength.Auto : new GridLength(1, GridUnitType.Auto) });
            }

            // Define columns
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int row = 0;

            // Title
            var titleTextBlock = new TextBlock
            {
                Text = "MCP Server Configuration",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(titleTextBlock, row);
            Grid.SetColumn(titleTextBlock, 0);
            Grid.SetColumnSpan(titleTextBlock, 2);
            mainGrid.Children.Add(titleTextBlock);
            row++;

            // Host
            var hostLabel = new TextBlock { Text = "Host/IP:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) };
            Grid.SetRow(hostLabel, row);
            Grid.SetColumn(hostLabel, 0);
            mainGrid.Children.Add(hostLabel);

            hostTextBox = new TextBox { Margin = new Thickness(5, 2, 5, 2) };
            hostTextBox.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding("Host") { Mode = System.Windows.Data.BindingMode.TwoWay, UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged });
            Grid.SetRow(hostTextBox, row);
            Grid.SetColumn(hostTextBox, 1);
            mainGrid.Children.Add(hostTextBox);
            row++;

            // Port
            var portLabel = new TextBlock { Text = "Port:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) };
            Grid.SetRow(portLabel, row);
            Grid.SetColumn(portLabel, 0);
            mainGrid.Children.Add(portLabel);

            portTextBox = new TextBox { Margin = new Thickness(5, 2, 5, 2) };
            portTextBox.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding("Port") { Mode = System.Windows.Data.BindingMode.TwoWay, UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged });
            Grid.SetRow(portTextBox, row);
            Grid.SetColumn(portTextBox, 1);
            mainGrid.Children.Add(portTextBox);
            row++;

            // Server Name
            var serverNameLabel = new TextBlock { Text = "Server Name:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) };
            Grid.SetRow(serverNameLabel, row);
            Grid.SetColumn(serverNameLabel, 0);
            mainGrid.Children.Add(serverNameLabel);

            serverNameTextBox = new TextBox { Margin = new Thickness(5, 2, 5, 2) };
            serverNameTextBox.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding("ServerName") { Mode = System.Windows.Data.BindingMode.TwoWay, UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged });
            Grid.SetRow(serverNameTextBox, row);
            Grid.SetColumn(serverNameTextBox, 1);
            mainGrid.Children.Add(serverNameTextBox);
            row++;

            // Base URL
            var baseUrlLabel = new TextBlock { Text = "Base URL:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) };
            Grid.SetRow(baseUrlLabel, row);
            Grid.SetColumn(baseUrlLabel, 0);
            mainGrid.Children.Add(baseUrlLabel);

            var baseUrlTextBlock = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 2, 5, 2), FontFamily = new FontFamily("Consolas") };
            baseUrlTextBlock.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("BaseUrl"));
            Grid.SetRow(baseUrlTextBlock, row);
            Grid.SetColumn(baseUrlTextBlock, 1);
            mainGrid.Children.Add(baseUrlTextBlock);
            row++;

            // Status
            var statusLabel = new TextBlock { Text = "Status:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) };
            Grid.SetRow(statusLabel, row);
            Grid.SetColumn(statusLabel, 0);
            mainGrid.Children.Add(statusLabel);

            statusTextBlock = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 2, 5, 2), FontWeight = FontWeights.Bold, Foreground = Brushes.Gray };
            statusTextBlock.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("ServerStatus"));
            Grid.SetRow(statusTextBlock, row);
            Grid.SetColumn(statusTextBlock, 1);
            mainGrid.Children.Add(statusTextBlock);
            row++;

            // Buttons
            var buttonPanel = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 20, 0, 10) };
            Grid.SetRow(buttonPanel, row);
            Grid.SetColumn(buttonPanel, 0);
            Grid.SetColumnSpan(buttonPanel, 2);

            saveButton = new Button { Content = "Save Configuration", Padding = new Thickness(12, 8, 12, 8), Width = 140, Margin = new Thickness(2, 2, 2, 2) };
            saveButton.Click += SaveButton_Click;
            buttonPanel.Children.Add(saveButton);

            testButton = new Button { Content = "Test Connection", Padding = new Thickness(12, 8, 12, 8), Width = 140, Margin = new Thickness(2, 2, 2, 2) };
            testButton.Click += TestButton_Click;
            buttonPanel.Children.Add(testButton);

            startStopButton = new Button { Padding = new Thickness(12, 8, 12, 8), Width = 140, Margin = new Thickness(2, 2, 2, 2) };
            startStopButton.SetBinding(ContentProperty, new System.Windows.Data.Binding("StartStopButtonText"));
            startStopButton.Click += StartStopButton_Click;
            buttonPanel.Children.Add(startStopButton);

            openApiButton = new Button { Content = "Open API Docs", Padding = new Thickness(12, 8, 12, 8), Width = 140, Margin = new Thickness(2, 2, 2, 2) };
            openApiButton.Click += OpenApiButton_Click;
            buttonPanel.Children.Add(openApiButton);

            mainGrid.Children.Add(buttonPanel);
            row++;

            // Test result
            var testResultTextBlock = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0), FontFamily = new FontFamily("Consolas"), FontSize = 11 };
            testResultTextBlock.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("TestResult"));
            Grid.SetRow(testResultTextBlock, row);
            Grid.SetColumn(testResultTextBlock, 0);
            Grid.SetColumnSpan(testResultTextBlock, 2);
            mainGrid.Children.Add(testResultTextBlock);

            Content = mainGrid;
        }

        private void UpdateServerStatus()
        {
            bool isRunning = _plugin?.GetServerWrapper()?.IsRunning == true;
            _viewModel.ServerStatus = isRunning ? "Running" : "Stopped";

            // Update status text block color based on server status
            if (statusTextBlock != null)
            {
                statusTextBlock.Foreground = isRunning
                    ? new SolidColorBrush(Color.FromRgb(0, 200, 0))      // Green for Running
                    : new SolidColorBrush(Color.FromRgb(200, 0, 0));      // Red for Stopped
            }

            openApiButton.IsEnabled = isRunning;
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
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var response = await client.GetAsync($"{_viewModel.BaseUrl}/scalar/v1");
                _viewModel.TestResult = response.IsSuccessStatusCode
                    ? "✓ Connection successful! Server is responding."
                    : $"✗ Server responded with status: {response.StatusCode}";
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

        private void OpenApiButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_plugin?.GetServerWrapper()?.IsRunning == true)
                {
                    _viewModel.TestResult = "Server is not running. Please start the server first.";
                    return;
                }

                var url = _viewModel.ScalarUrl;
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                _viewModel.TestResult = $"Opening API documentation in browser: {url}";
            }
            catch (Exception ex)
            {
                _viewModel.TestResult = $"Error opening API documentation: {ex.Message}";
            }
        }

        private void ApplyTheme(bool isDarkMode)
        {
            var backgroundColor = isDarkMode
                ? new SolidColorBrush(Color.FromRgb(32, 32, 32))
                : new SolidColorBrush(Color.FromRgb(255, 255, 255));

            Background = backgroundColor;

            var textColor = isDarkMode
                ? Color.FromRgb(255, 255, 255)
                : Color.FromRgb(0, 0, 0);
            var textBrush = new SolidColorBrush(textColor);

            // Apply text colors to all text elements
            foreach (var child in ((Grid)Content).Children)
            {
                if (child is TextBlock tb)
                    tb.Foreground = textBrush;
            }

            // Apply textbox styles
            var tbBackground = isDarkMode ? new SolidColorBrush(Color.FromRgb(51, 51, 51)) : Brushes.White;
            var tbForeground = isDarkMode ? Brushes.White : Brushes.Black;
            var tbBorder = isDarkMode ? new SolidColorBrush(Color.FromRgb(85, 85, 85)) : new SolidColorBrush(Color.FromRgb(170, 170, 170));

            hostTextBox.Background = tbBackground;
            hostTextBox.Foreground = tbForeground;
            hostTextBox.BorderBrush = tbBorder;

            portTextBox.Background = tbBackground;
            portTextBox.Foreground = tbForeground;
            portTextBox.BorderBrush = tbBorder;

            serverNameTextBox.Background = tbBackground;
            serverNameTextBox.Foreground = tbForeground;
            serverNameTextBox.BorderBrush = tbBorder;

            // Apply button styles
            var btnBackground = isDarkMode ? new SolidColorBrush(Color.FromRgb(68, 68, 68)) : new SolidColorBrush(Color.FromRgb(240, 240, 240));
            var btnForeground = isDarkMode ? Brushes.White : Brushes.Black;
            var btnBorder = isDarkMode ? new SolidColorBrush(Color.FromRgb(100, 100, 100)) : new SolidColorBrush(Color.FromRgb(170, 170, 170));
            var btnHoverBackground = isDarkMode ? new SolidColorBrush(Color.FromRgb(85, 85, 85)) : new SolidColorBrush(Color.FromRgb(225, 225, 225));
            var btnDisabledBackground = isDarkMode ? new SolidColorBrush(Color.FromRgb(50, 50, 50)) : new SolidColorBrush(Color.FromRgb(245, 245, 245));
            var btnDisabledForeground = isDarkMode ? new SolidColorBrush(Color.FromRgb(100, 100, 100)) : new SolidColorBrush(Color.FromRgb(160, 160, 160));

            ApplyButtonStyle(saveButton, btnBackground, btnForeground, btnBorder, btnHoverBackground, btnDisabledBackground, btnDisabledForeground);
            ApplyButtonStyle(testButton, btnBackground, btnForeground, btnBorder, btnHoverBackground, btnDisabledBackground, btnDisabledForeground);
            ApplyButtonStyle(startStopButton, btnBackground, btnForeground, btnBorder, btnHoverBackground, btnDisabledBackground, btnDisabledForeground);
            ApplyButtonStyle(openApiButton, btnBackground, btnForeground, btnBorder, btnHoverBackground, btnDisabledBackground, btnDisabledForeground);
        }

        private void ApplyButtonStyle(Button button, SolidColorBrush background, SolidColorBrush foreground,
            SolidColorBrush border, SolidColorBrush hoverBackground, SolidColorBrush disabledBackground,
            SolidColorBrush disabledForeground)
        {
            var style = new Style(typeof(Button));

            // Default appearance
            style.Setters.Add(new Setter(BackgroundProperty, background));
            style.Setters.Add(new Setter(ForegroundProperty, foreground));
            style.Setters.Add(new Setter(BorderBrushProperty, border));
            style.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(1)));

            // Create control template to remove default WPF button styling
            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "border";
            borderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            borderFactory.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            borderFactory.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(1));

            var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);

            borderFactory.AppendChild(contentPresenterFactory);
            template.VisualTree = borderFactory;

            // Mouse over trigger
            var mouseOverTrigger = new Trigger { Property = IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(BackgroundProperty, hoverBackground, "border"));
            template.Triggers.Add(mouseOverTrigger);

            // Disabled trigger
            var disabledTrigger = new Trigger { Property = IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter(BackgroundProperty, disabledBackground, "border"));
            disabledTrigger.Setters.Add(new Setter(ForegroundProperty, disabledForeground));
            template.Triggers.Add(disabledTrigger);

            style.Setters.Add(new Setter(TemplateProperty, template));
            button.Style = style;
        }
    }
}

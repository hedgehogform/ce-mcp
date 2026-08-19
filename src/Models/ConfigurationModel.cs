using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace CEMCP.Models
{
    public class ConfigurationModel : INotifyPropertyChanged
    {
        private string _host = "127.0.0.1";
        private int _port = 6300;
        private string _serverName = "Cheat Engine MCP Server";
        private string _serverStatus = "Stopped";
        private Brush _serverStatusColor = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        private string _testResult = "";
        private bool _isServerRunning = false;
        private bool _isDarkMode = false;
        private readonly string _version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

        public string Host
        {
            get => _host;
            set
            {
                if (_host != value)
                {
                    _host = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BaseUrl));
                }
            }
        }

        public int Port
        {
            get => _port;
            set
            {
                if (_port != value)
                {
                    _port = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BaseUrl));
                }
            }
        }

        public string ServerName
        {
            get => _serverName;
            set
            {
                if (_serverName != value)
                {
                    _serverName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string BaseUrl => $"http://{Host}:{Port}/";

        public string Version => _version;
        public bool CanEditConfiguration => !IsServerRunning;

        public string StartStopButtonText => ServerStatus.Equals("running", System.StringComparison.OrdinalIgnoreCase) ? "Stop Server" : "Start Server";

        public bool IsServerRunning
        {
            get => _isServerRunning;
            set
            {
                if (_isServerRunning != value)
                {
                    _isServerRunning = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanEditConfiguration));
                }
            }
        }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    OnPropertyChanged();
                    UpdateStatusColor();
                }
            }
        }

        public string ServerStatus
        {
            get => _serverStatus;
            set
            {
                if (_serverStatus != value)
                {
                    _serverStatus = value;
                    OnPropertyChanged();

                    UpdateStatusColor();

                    IsServerRunning = value.Equals("running", System.StringComparison.CurrentCultureIgnoreCase);
                    OnPropertyChanged(nameof(StartStopButtonText));
                }
            }
        }

        public Brush ServerStatusColor
        {
            get => _serverStatusColor;
            private set
            {
                if (_serverStatusColor != value)
                {
                    _serverStatusColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TestResult
        {
            get => _testResult;
            set
            {
                if (_testResult != value)
                {
                    _testResult = value;
                    OnPropertyChanged();
                }
            }
        }

        public void LoadFromServerConfig()
        {
            Host = ServerConfig.ConfigHost;
            Port = ServerConfig.ConfigPort;
            ServerName = ServerConfig.ConfigServerName;
        }

        public void SaveToServerConfig()
        {
            string host = Host.Trim();
            string unwrappedHost = host.Length >= 2 && host[0] == '[' && host[^1] == ']'
                ? host[1..^1]
                : host;
            if (Uri.CheckHostName(unwrappedHost) == UriHostNameType.Unknown)
                throw new ArgumentException("Enter a valid host name or IP address.", nameof(Host));
            if (Port is < 1 or > 65535)
                throw new ArgumentOutOfRangeException(nameof(Port), "Port must be between 1 and 65535.");

            string serverName = ServerName.Trim();
            if (serverName.Length == 0)
                throw new ArgumentException("Server name is required.", nameof(ServerName));

            Host = Uri.CheckHostName(unwrappedHost) == UriHostNameType.IPv6
                ? $"[{unwrappedHost}]"
                : unwrappedHost;
            ServerName = serverName;
            ServerConfig.ConfigHost = Host;
            ServerConfig.ConfigPort = Port;
            ServerConfig.ConfigServerName = ServerName;
            ServerConfig.SaveToFile();
        }

        private void UpdateStatusColor()
        {
            if (_serverStatus.Equals("running", System.StringComparison.OrdinalIgnoreCase))
            {
                ServerStatusColor = _isDarkMode
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                    : new SolidColorBrush(Color.FromRgb(0, 128, 0));
            }
            else if (_serverStatus.Equals("stopped", System.StringComparison.OrdinalIgnoreCase))
            {
                ServerStatusColor = _isDarkMode
                    ? new SolidColorBrush(Color.FromRgb(244, 67, 54))
                    : new SolidColorBrush(Color.FromRgb(196, 43, 28));
            }
            else if (_serverStatus.Equals("starting", System.StringComparison.OrdinalIgnoreCase) ||
                     _serverStatus.Equals("stopping", System.StringComparison.OrdinalIgnoreCase))
            {
                ServerStatusColor = _isDarkMode
                    ? new SolidColorBrush(Color.FromRgb(255, 152, 0))
                    : new SolidColorBrush(Color.FromRgb(184, 104, 0));
            }
            else
            {
                ServerStatusColor = _isDarkMode
                    ? new SolidColorBrush(Color.FromRgb(158, 158, 158))
                    : new SolidColorBrush(Color.FromRgb(96, 96, 96));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
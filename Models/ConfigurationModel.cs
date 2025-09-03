using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace CeMCP.Models
{
    public class ConfigurationModel : INotifyPropertyChanged
    {
        private string _host = "127.0.0.1";
        private int _port = 6300;
        private string _serverName = "Cheat Engine MCP Server";
        private string _serverStatus = "Stopped";
        private Brush _serverStatusColor = Brushes.Red;
        private string _testResult = "";
        private bool _isServerRunning = false;

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

        public string BaseUrl => $"http://{Host}:{Port}";

        public string SwaggerUrl => $"{BaseUrl}/swagger";

        public string StartStopButtonText => ServerStatus.ToLower() == "running" ? "Stop Server" : "Start Server";

        public bool IsServerRunning
        {
            get => _isServerRunning;
            set
            {
                if (_isServerRunning != value)
                {
                    _isServerRunning = value;
                    OnPropertyChanged();
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

                    // Update color based on status
                    string lowerStatus = value.ToLower();
                    if (lowerStatus == "running")
                        ServerStatusColor = Brushes.Green;
                    else if (lowerStatus == "stopped")
                        ServerStatusColor = Brushes.Red;
                    else if (lowerStatus == "starting" || lowerStatus == "stopping")
                        ServerStatusColor = Brushes.Orange;
                    else
                        ServerStatusColor = Brushes.Gray;

                    IsServerRunning = value.ToLower() == "running";
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
            ServerConfig.ConfigHost = Host;
            ServerConfig.ConfigPort = Port;
            ServerConfig.ConfigServerName = ServerName;
            ServerConfig.SaveToFile();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
using CESDK;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CeMCP
{
    public class McpPlugin : CESDKPluginClass
    {
        private bool _isServerRunning = false;
        private MCPServerWrapper _mcpServer;
        private ConfigurationWindow _configWindow = null;
        private Thread _configThread = null;
        
        public bool IsServerRunning => _isServerRunning;

        public override string GetPluginName()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            return $"MCP Server for Cheat Engine v{version}";
        }

        public override bool EnablePlugin()
        {
            sdk.lua.Register("toggle_mcp_server", ToggleMCPServer);
            sdk.lua.Register("update_button_text", UpdateButtonText);
            sdk.lua.Register("show_mcp_config", ShowMCPConfig);

            sdk.lua.DoString(@"
                local m=MainForm.Menu
                local topm=createMenuItem(m)
                topm.Caption='MCP'
                m.Items.insert(MainForm.miHelp.MenuIndex,topm)

                mcpToggleMenuItem=createMenuItem(m)
                mcpToggleMenuItem.Caption='Start MCP Server'
                mcpToggleMenuItem.OnClick=function(s)
                    toggle_mcp_server()
                end
                topm.add(mcpToggleMenuItem)
                
                mcpConfigMenuItem=createMenuItem(m)
                mcpConfigMenuItem.Caption='Configure'
                mcpConfigMenuItem.OnClick=function(s)
                    show_mcp_config()
                end
                topm.add(mcpConfigMenuItem)
            ");

            return true;
        }

        public override bool DisablePlugin()
        {
            StopMCPServer().Wait();
            return true;
        }

        int ToggleMCPServer()
        {
            if (_isServerRunning)
                StopMCPServer().Wait();
            else
                StartMCPServer();
            return 1;
        }

        int UpdateButtonText()
        {
            string buttonText = _isServerRunning ? "Stop MCP Server" : "Start MCP Server";
            sdk.lua.DoString($"mcpToggleMenuItem.Caption='{buttonText}'");
            return 1;
        }

        int ShowMCPConfig()
        {
            try
            {
                // Check if window is already open
                if (_configWindow != null && _configThread != null && _configThread.IsAlive)
                {
                    // Try to bring existing window to front
                    _configWindow?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        if (_configWindow.WindowState == System.Windows.WindowState.Minimized)
                            _configWindow.WindowState = System.Windows.WindowState.Normal;
                        _configWindow.Activate();
                        _configWindow.Topmost = true;
                        _configWindow.Topmost = false;
                        _configWindow.Focus();
                    }));
                    return 1;
                }

                _configThread = new Thread(() =>
                {
                    try
                    {
                        // Create and start WPF message pump properly
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Normal,
                            new Action(() =>
                            {
                                _configWindow = new ConfigurationWindow(this);
                                
                                // Handle window closing to clean up references
                                _configWindow.Closed += (sender, e) =>
                                {
                                    _configWindow = null;
                                    System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvokeShutdown(
                                        System.Windows.Threading.DispatcherPriority.Background);
                                };
                                
                                _configWindow.Show();
                            }));
                        
                        // Start the WPF dispatcher message loop
                        System.Windows.Threading.Dispatcher.Run();
                        
                        _configThread = null;
                    }
                    catch (Exception ex)
                    {
                        sdk.lua.DoString($"print('Error in configuration window: {ex.Message}')");
                        _configWindow = null;
                        _configThread = null;
                    }
                });
                
                _configThread.SetApartmentState(ApartmentState.STA);
                _configThread.Start();
                return 1;
            }
            catch (Exception ex)
            {
                sdk.lua.DoString($"print('Error opening configuration: {ex.Message}')");
                return 0;
            }
        }

        public void RestartServer()
        {
            if (_isServerRunning)
            {
                StopMCPServer().Wait();
                StartMCPServer();
            }
        }

        public MCPServerWrapper GetServerWrapper()
        {
            return _mcpServer;
        }

        public void StartServer()
        {
            StartMCPServer();
        }

        public void StopServer()
        {
            StopMCPServer().Wait();
        }

        void StartMCPServer()
        {
            if (_isServerRunning) return;

            _mcpServer = new MCPServerWrapper(this);
            ServerConfig.LoadFromFile();
            ServerConfig.LoadFromEnvironment(); // Environment variables override config file
            _mcpServer.Start(ServerConfig.BaseUrl);

            _isServerRunning = true;
            sdk.lua.DoString($"print('MCP API Started on: {ServerConfig.BaseUrl}')");
            UpdateButtonText();
        }

        async Task StopMCPServer()
        {
            if (!_isServerRunning) return;

            if (_mcpServer != null)
                await _mcpServer.StopAsync();
            _mcpServer = null;
            _isServerRunning = false;
            sdk.lua.DoString("print('MCP API stopped')");
            UpdateButtonText();
        }
    }
}

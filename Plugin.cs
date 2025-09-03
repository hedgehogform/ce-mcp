using CESDK;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CeMCP
{
    public class McpPlugin : CheatEnginePlugin
    {
        private bool isServerRunning = false;
        private McpServer? mcpServer;
        private ConfigWindow? configWindow = null;
        private Thread? configThread = null;

        public bool IsServerRunning => isServerRunning;



        public override string Name
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
                return $"MCP Server for Cheat Engine v{version}";
            }
        }

        protected override void OnEnable()
        {
            PluginContext.Lua.RegisterFunction("toggle_mcp_server", ToggleMCPServer);
            PluginContext.Lua.RegisterFunction("update_button_text", UpdateButtonText);
            PluginContext.Lua.RegisterFunction("show_mcp_config", ShowMCPConfig);

            PluginContext.Lua.DoString(@"
                local m=MainForm.Menu
                topm=createMenuItem(m)
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
        }

        protected override void OnDisable()
        {
            StopMCPServer();

            // Remove menu items
            PluginContext.Lua.DoString(@"
                if mcpToggleMenuItem then
                    mcpToggleMenuItem.destroy()
                    mcpToggleMenuItem = nil
                end
                if mcpConfigMenuItem then
                    mcpConfigMenuItem.destroy()
                    mcpConfigMenuItem = nil
                end
                if topm then
                    topm.destroy()
                    topm = nil
                end
            ");
        }

        void ToggleMCPServer()
        {
            if (isServerRunning)
                StopMCPServer();
            else
                StartMCPServer();
        }

        void UpdateButtonText()
        {
            string buttonText = isServerRunning ? "Stop MCP Server" : "Start MCP Server";
            PluginContext.Lua.DoString($"mcpToggleMenuItem.Caption='{buttonText}'");
        }

        void ShowMCPConfig()
        {
            try
            {
                if (configWindow != null && configThread != null && configThread.IsAlive)
                {
                    // Bring existing window to front
                    configWindow.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        if (configWindow.WindowState == System.Windows.WindowState.Minimized)
                            configWindow.WindowState = System.Windows.WindowState.Normal;
                        configWindow.Activate();
                        configWindow.Topmost = true;
                        configWindow.Topmost = false;
                        configWindow.Focus();
                    }));
                    return;
                }

                // Start window in a new STA thread
                configThread = new Thread(() =>
                {
                    try
                    {
                        configWindow = new ConfigWindow(this);
                        configWindow.Closed += (sender, e) =>
                        {
                            configWindow = null;
                            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvokeShutdown(
                                System.Windows.Threading.DispatcherPriority.Background);
                        };

                        // Show the window and start WPF message loop
                        configWindow.Show();
                        System.Windows.Threading.Dispatcher.Run();
                    }
                    catch (Exception ex)
                    {
                        PluginContext.Lua.DoString($"print('Error in config window thread: {ex.Message}')");
                        configWindow = null;
                    }
                });

                configThread.SetApartmentState(ApartmentState.STA);
                configThread.Start();
            }
            catch (Exception ex)
            {
                PluginContext.Lua.DoString($"print('Error opening configuration: {ex.Message}')");
            }
        }

        public void RestartServer()
        {
            if (isServerRunning)
            {
                StopMCPServer();
                StartMCPServer();
            }
        }

        public McpServer? GetServerWrapper()
        {
            return mcpServer;
        }

        public void StartServer()
        {
            StartMCPServer();
        }

        public void StopServer()
        {
            StopMCPServer();
        }

        void StartMCPServer()
        {
            if (isServerRunning) return;

            mcpServer = new McpServer();
            ServerConfig.LoadFromFile();
            ServerConfig.LoadFromEnvironment(); // Environment variables override config file
            mcpServer.Start(ServerConfig.ConfigBaseUrl);

            isServerRunning = true;
            PluginContext.Lua.DoString($"print('MCP API Started on: {ServerConfig.ConfigBaseUrl}')");
            UpdateButtonText();
        }

        void StopMCPServer()
        {
            if (!isServerRunning) return;

            mcpServer?.Stop();
            mcpServer = null;
            isServerRunning = false;
            PluginContext.Lua.DoString("print('MCP API stopped')");
            UpdateButtonText();
        }
    }
}

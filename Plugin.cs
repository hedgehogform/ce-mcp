using CESDK;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CeMCP
{
    public class McpPlugin : CESDKPluginClass
    {
        private bool isServerRunning = false;
        private McpServer mcpServer;
        private ConfigWindow configWindow = null;
        private Thread configThread = null;

        public bool IsServerRunning => isServerRunning;

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

            return true;
        }

        public override bool DisablePlugin()
        {
            StopMCPServer();
            
            // Remove menu items
            sdk.lua.DoString(@"
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
            
            return true;
        }

        int ToggleMCPServer()
        {
            if (isServerRunning)
                StopMCPServer();
            else
                StartMCPServer();
            return 1;
        }

        int UpdateButtonText()
        {
            string buttonText = isServerRunning ? "Stop MCP Server" : "Start MCP Server";
            sdk.lua.DoString($"mcpToggleMenuItem.Caption='{buttonText}'");
            return 1;
        }

        int ShowMCPConfig()
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
                    return 1;
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
                        sdk.lua.DoString($"print('Error in config window thread: {ex.Message}')");
                        configWindow = null;
                    }
                });

                configThread.SetApartmentState(ApartmentState.STA);
                configThread.Start();

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
            if (isServerRunning)
            {
                StopMCPServer();
                StartMCPServer();
            }
        }

        public McpServer GetServerWrapper()
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
            sdk.lua.DoString($"print('MCP API Started on: {ServerConfig.ConfigBaseUrl}')");
            UpdateButtonText();
        }

        void StopMCPServer()
        {
            if (!isServerRunning) return;

            mcpServer?.Stop();
            mcpServer = null;
            isServerRunning = false;
            sdk.lua.DoString("print('MCP API stopped')");
            UpdateButtonText();
        }
    }
}

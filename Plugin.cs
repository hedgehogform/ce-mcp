using CESDK;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CeMCP
{
    class McpPlugin : CESDKPluginClass
    {
        private MCPServer? _mcpServer;

        public override string GetPluginName()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            return $"MCP Server for Cheat Engine v{version}";
        }

        public override bool DisablePlugin() //called when disabled
        {
            _mcpServer?.StopAsync();
            return true;
        }

        public override bool EnablePlugin() //called when enabled
        {
            sdk.lua.Register("start_mcp_server", StartMCPServer);
            sdk.lua.Register("stop_mcp_server", StopMCPServer);

            sdk.lua.DoString(@"
                local m=MainForm.Menu
                local topm=createMenuItem(m)
                topm.Caption='MCP'
                m.Items.insert(MainForm.miHelp.MenuIndex,topm)

                local startMenuItem=createMenuItem(m)
                startMenuItem.Caption='Start MCP Server';
                startMenuItem.OnClick=function(s)
                start_mcp_server()
                end
                topm.add(startMenuItem)

                local stopMenuItem=createMenuItem(m)
                stopMenuItem.Caption='Stop MCP Server';
                stopMenuItem.OnClick=function(s)
                stop_mcp_server()
                end
                topm.add(stopMenuItem)
                ");

            return true;
        }

        int StartMCPServer()
        {
            try
            {
                if (_mcpServer == null)
                {
                    _mcpServer = new MCPServer();
                    _ = Task.Run(async () => await _mcpServer.StartAsync());
                    sdk.lua.DoString("print('MCP Server started on http://localhost:6300/')");
                }
                else
                {
                    sdk.lua.DoString("print('MCP Server is already running')");
                }
            }
            catch (Exception ex)
            {
                sdk.lua.DoString($"print('Error starting MCP Server: {ex.Message}')");
            }
            return 1;
        }

        int StopMCPServer()
        {
            try
            {
                if (_mcpServer != null)
                {
                    _ = Task.Run(async () => await _mcpServer.StopAsync());
                    _mcpServer = null;
                    sdk.lua.DoString("print('MCP Server stopped')");
                }
                else
                {
                    sdk.lua.DoString("print('MCP Server is not running')");
                }
            }
            catch (Exception ex)
            {
                sdk.lua.DoString($"print('Error stopping MCP Server: {ex.Message}')");
            }
            return 1;
        }

    }
}

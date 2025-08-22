#nullable enable
using CESDK;
using ModelContextProtocol.Protocol;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace CeMCP
{
    public class McpPlugin : CESDKPluginClass
    {
        private bool _isServerRunning = false;

        public override string GetPluginName()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            return $"MCP Server for Cheat Engine v{version}";
        }

        public override bool DisablePlugin()
        {
            StopMCPServer().Wait();
            return true;
        }

        public override bool EnablePlugin()
        {
            sdk.lua.Register("toggle_mcp_server", ToggleMCPServer);
            sdk.lua.Register("update_button_text", UpdateButtonText);

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
            ");

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

        void StartMCPServer()
        {
            // TODO: Implement mcp
        }

        async Task StopMCPServer()
        {
            //    Stop mcp
        }
    }
}

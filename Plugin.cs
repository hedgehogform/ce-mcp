using CESDK;
using System.Reflection;
using System.Threading.Tasks;

namespace CeMCP
{
    public class McpPlugin : CESDKPluginClass
    {
        private bool _isServerRunning = false;
        private MCPServerWrapper _mcpServer;

        public override string GetPluginName()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            return $"MCP Server for Cheat Engine v{version}";
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

        void StartMCPServer()
        {
            if (_isServerRunning) return;

            _mcpServer = new MCPServerWrapper(this);
            _mcpServer.Start("http://localhost:6300");

            _isServerRunning = true;
            UpdateButtonText();
        }

        async Task StopMCPServer()
        {
            if (!_isServerRunning) return;

            if (_mcpServer != null)
                await _mcpServer.StopAsync();

            _mcpServer = null;
            _isServerRunning = false;
            UpdateButtonText();
        }
    }
}

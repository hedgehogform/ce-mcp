using System;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;

namespace CeMCP
{
    public class MCPServerWrapper(McpPlugin plugin)
    {
        private IDisposable _webApp;
        private readonly McpPlugin _plugin = plugin;
        
        public bool IsRunning => _webApp != null;

        public void Start(string url)
        {
            var startup = new Startup(_plugin);
            _webApp = WebApp.Start(url, startup.Configuration);
        }

        public async Task StopAsync()
        {
            _webApp?.Dispose();
            _webApp = null;
            await Task.CompletedTask;
        }
    }
}
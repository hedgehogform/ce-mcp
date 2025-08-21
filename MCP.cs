#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Owin.Hosting;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace CeMCP
{
    public class MCPServer
    {
        private IDisposable? _webApp;
        private CancellationTokenSource? _cancellationTokenSource;

        public async Task StartAsync()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                
                // Start OWIN web server with MCP endpoint
                _webApp = WebApp.Start<OwinStartup>("http://localhost:6300");
                
                // Keep the server running until cancelled
                await Task.Delay(-1, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to start MCP server: {ex.Message}", ex);
            }
        }

        public async Task StopAsync()
        {
            _cancellationTokenSource?.Cancel();
            _webApp?.Dispose();
            await Task.CompletedTask;
        }
    }

    [McpServerToolType]
    public static class CheatEngineTools
    {
        [McpServerTool, Description("Get information about Cheat Engine")]
        public static string GetInfo()
        {
            return "Cheat Engine MCP Server - Provides access to Cheat Engine functionality through MCP protocol";
        }

        [McpServerTool, Description("Echo a message back to the client")]
        public static string Echo(string message)
        {
            return $"Cheat Engine MCP Server echoes: {message}";
        }

        [McpServerTool, Description("Get current status of Cheat Engine")]
        public static string GetStatus()
        {
            return "Cheat Engine MCP Server is running";
        }
    }
}
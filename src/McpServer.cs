using System;
using System.Threading;
using System.Threading.Tasks;
using CESDK;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace CEMCP
{
    public class McpServer
    {
        private WebApplication? _app;
        private CancellationTokenSource? _cts;

        public void Start(string baseUrl)
        {
            if (_app != null) return; // Already running

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = [],
                ApplicationName = typeof(McpServer).Assembly.GetName().Name,
                ContentRootPath = System.IO.Path.GetTempPath(),
                WebRootPath = System.IO.Path.GetTempPath()
            });

            builder.Logging.ClearProviders();
            builder.Logging.AddNLog(
                new NLogProviderOptions { ShutdownOnDispose = false },
                _ => PluginLogger.Factory);

            // Setup MCP server with Streamable HTTP transport and all tools
            builder.Services.AddMcpServer(options =>
            {
                options.ServerInfo = new()
                {
                    Name = ServerConfig.ConfigServerName,
                    Version = System.Reflection.Assembly.GetExecutingAssembly()
                        .GetName().Version?.ToString() ?? "1.0.0"
                };
            })
            .WithHttpTransport(options =>
            {
                options.Stateless = true;
            })
            .WithToolsAndSchemaTransform<Tools.ProcessTool>()
            .WithToolsAndSchemaTransform<Tools.LuaExecutionTool>()
            .WithToolsAndSchemaTransform<Tools.MemoryTool>()
            .WithToolsAndSchemaTransform<Tools.ScanTool>()
            .WithToolsAndSchemaTransform<Tools.AssemblyTool>()
            .WithToolsAndSchemaTransform<Tools.ConversionTool>()
            .WithToolsAndSchemaTransform<Tools.AddressListTool>()
            .WithToolsAndSchemaTransform<Tools.AutoAssemblyTool>()
            .WithToolsAndSchemaTransform<Tools.MemoryViewTool>()
            .WithToolsAndSchemaTransform<Tools.SymbolTool>()
            .WithToolsAndSchemaTransform<Tools.DebuggerTool>()
            .WithToolsAndSchemaTransform<Tools.AdvancedMemoryTool>()
            .WithToolsAndSchemaTransform<Tools.PointerTool>()
            .WithToolsAndSchemaTransform<Tools.ProcessControlTool>()
            .WithToolsAndSchemaTransform<Tools.CheatTableTool>()
            .WithToolsAndSchemaTransform<Tools.StructureTool>()
            .WithToolsAndSchemaTransform<Tools.InjectionTool>()
            .WithToolsAndSchemaTransform<Tools.AnalysisTool>()
            .WithToolsAndSchemaTransform<Tools.SymbolRegistryTool>()
            .WithToolsAndSchemaTransform<Tools.DbvmTool>();

            builder.WebHost.UseUrls(baseUrl);

            // Build app
            _app = builder.Build();

            // Map MCP endpoints (Streamable HTTP)
            _app.MapMcp();

            // Start server in background
            _cts = new CancellationTokenSource();
            Task.Run(async () => await _app.RunAsync());
        }

        public void Stop()
        {
            if (_app == null) return; // Not running

            var appToStop = _app;
            var ctsToStop = _cts;
            _app = null;
            _cts = null;

            // Stop server in background (don't freeze CE)
            Task.Run(async () =>
            {
                try
                {
                    ctsToStop?.Cancel();
                    await appToStop.StopAsync();
                    await appToStop.DisposeAsync();
                    ctsToStop?.Dispose();
                }
                catch
                {
                    // Best-effort cleanup
                }
            });
        }

        public bool IsRunning => _app != null;
    }
}

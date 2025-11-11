using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scalar.AspNetCore;

namespace CEMCP
{
  public class ApiServer
  {
    private WebApplication? _app;

    public void Start(string baseUrl)
    {
      if (_app != null) return; // Already running

      var builder = WebApplication.CreateBuilder(new WebApplicationOptions
      {
        Args = [],
        ContentRootPath = System.IO.Path.GetTempPath(),
        WebRootPath = System.IO.Path.GetTempPath()
      });

      // Setup services
      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddOpenApi();
      builder.Logging.ClearProviders(); // Disable logging
      builder.WebHost.UseUrls(baseUrl);

      // Build app
      _app = builder.Build();

      // Setup API documentation
      _app.MapOpenApi();
      _app.MapScalarApiReference();

      // Setup all API endpoints
      RegisterEndpoints(_app);

      // Start server in background
      Task.Run(() => _app.RunAsync());
    }

    private static void RegisterEndpoints(WebApplication app)
    {
      // Root endpoint
      app.MapGet("/", () => new
      {
        name = "Cheat Engine API",
        version = "1.0.0",
        documentation = "/scalar/v1"
      })
      .WithName("GetRoot")
      .WithOpenApi();

      // All tool APIs
      Tools.ProcessTool.MapProcessApi(app);
      Tools.LuaExecutionTool.MapLuaApi(app);
      Tools.MemoryReadTool.MapMemoryReadApi(app);
      Tools.MemoryWriteTool.MapMemoryWriteApi(app);
      Tools.AobScanTool.MapAobScanApi(app);
      Tools.DisassembleTool.MapDisassembleApi(app);
      Tools.AddressTool.MapAddressApi(app);
      Tools.ConversionTool.MapConversionApi(app);
      Tools.ThreadListTool.MapThreadListApi(app);
      Tools.MemScanTool.MapMemScanApi(app);
    }

    public void Stop()
    {
      if (_app == null) return; // Not running

      var appToStop = _app;
      _app = null;

      // Stop server in background (don't freeze CE)
      Task.Run(async () =>
      {
        await appToStop.StopAsync();
        await appToStop.DisposeAsync();
      });
    }

    public bool IsRunning => _app != null;
  }
}

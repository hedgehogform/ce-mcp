using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace CEMCP
{
  public class McpServer : IDisposable
  {
    private WebApplication? _app;
    private Task? _runTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed = false;

    public void Start(string baseUrl)
    {
      if (_app != null) return;

      try
      {
        _cancellationTokenSource = new CancellationTokenSource();

        var builder = WebApplication.CreateBuilder();

        // Add MCP server services
        builder.Services.AddMcpServer()
          .WithHttpTransport()
          .WithTools<CheatEngineToolsProvider>();

        // Configure Kestrel to listen on specified port
        builder.WebHost.UseUrls(baseUrl);

        var app = builder.Build();

        // Configure middleware pipeline
        app.MapMcp();

        // Start the server asynchronously on a background thread
        _runTask = Task.Run(async () =>
        {
          try
          {
            await app.RunAsync();
          }
          catch (OperationCanceledException)
          {
            // Expected when cancellation is requested
          }
        });

        _app = app;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Failed to start MCP server: {ex.Message}");
        throw;
      }
    }

    public void Stop()
    {
      if (_app == null) return;

      try
      {
        _cancellationTokenSource?.Cancel();
        _runTask?.Wait(TimeSpan.FromSeconds(5));
        _app.StopAsync().Wait(TimeSpan.FromSeconds(5));
        _app.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
        _app = null;
        _runTask = null;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Error stopping MCP server: {ex.Message}");
      }
    }

    public bool IsRunning => _app != null;

    protected virtual void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          Stop();
        }
        _disposed = true;
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
  }
}

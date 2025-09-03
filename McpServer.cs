using System;
using System.Web.Http;
using Microsoft.Owin.Hosting;
using Owin;
using System.Threading.Tasks;
using Microsoft.Owin;
using Swashbuckle.Application;
using System.IO;
using System.Reflection;

namespace CeMCP
{
  public class McpServer : IDisposable
  {
    private IDisposable? _webApp;
    private bool _disposed = false;

    public void Configuration(IAppBuilder appBuilder)
    {
      try
      {
        var config = new HttpConfiguration();

        config.MapHttpAttributeRoutes();
        config.Routes.MapHttpRoute(
            name: "DefaultApi",
            routeTemplate: "api/{controller}/{id}",
            defaults: new { id = RouteParameter.Optional }
        );

        // Add Swashbuckle OpenAPI generation with UI
        config.EnableSwagger(c =>
        {
          c.SingleApiVersion("v1", "Cheat Engine MCP API");
        })
        .EnableSwaggerUi();

        appBuilder.UseWebApi(config);
      }
      catch (Exception ex)
      {
        // Log the exception details for debugging
        System.Diagnostics.Debug.WriteLine($"Configuration error: {ex}");
        throw;
      }
    }



    public void Start(string baseUrl)
    {
      if (_webApp != null) return;
      
      try
      {
        _webApp = WebApp.Start<McpServer>(url: baseUrl);
      }
      catch (System.Reflection.TargetInvocationException ex)
      {
        System.Diagnostics.Debug.WriteLine($"TargetInvocationException during server start: {ex.InnerException?.Message ?? ex.Message}");
        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.InnerException?.StackTrace ?? ex.StackTrace}");
        throw;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Exception during server start: {ex.Message}");
        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        throw;
      }
    }

    public void Stop()
    {
      _webApp?.Dispose();
      _webApp = null;
    }

    public bool IsRunning => _webApp != null;

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

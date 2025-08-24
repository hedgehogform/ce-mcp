using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Dependencies;
using CeMCP.Controllers;
using Microsoft.Owin.Hosting;
using Owin;
using Swashbuckle.Application;

namespace CeMCP
{
    public class McpServer : IDisposable
    {
        private IDisposable _webApp;
        private bool _disposed = false;

        public void Configuration(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();

            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.EnableSwagger(c => c.SingleApiVersion("v1", "Cheat Engine MCP API"))
                  .EnableSwaggerUi();

            appBuilder.UseWebApi(config);
        }

        public void Start(string baseUrl)
        {
            if (_webApp != null) return;
            _webApp = WebApp.Start<McpServer>(url: baseUrl);
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

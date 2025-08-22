using System;
using System.Web.Http;
using Owin;
using System.ComponentModel.DataAnnotations;
using Swashbuckle.Application;
using CeMCP.Controllers;

namespace CeMCP
{
    public class Startup
    {
        private readonly McpPlugin _plugin;

        public Startup(McpPlugin plugin)
        {
            _plugin = plugin;
        }

        public void Configuration(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.DependencyResolver = new CheatEngineDependencyResolver(_plugin);

            config.EnableSwagger(c =>
            {
                c.SingleApiVersion("v1", "Cheat Engine MCP API");
            })
            .EnableSwaggerUi();

            appBuilder.UseWebApi(config);
        }
    }

    public class CheatEngineDependencyResolver : System.Web.Http.Dependencies.IDependencyResolver
    {
        private readonly McpPlugin _plugin;
        private bool _disposed = false;

        public CheatEngineDependencyResolver(McpPlugin plugin)
        {
            _plugin = plugin;
        }

        public System.Web.Http.Dependencies.IDependencyScope BeginScope()
        {
            return new CheatEngineDependencyScope(_plugin);
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(CheatEngineController))
                return new CheatEngineController(new CheatEngineTools(_plugin));
            return null;
        }

        public System.Collections.Generic.IEnumerable<object> GetServices(Type serviceType)
        {
            return new object[0];
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here if any
                }
                _disposed = true;
            }
        }
    }

    public class CheatEngineDependencyScope : System.Web.Http.Dependencies.IDependencyScope
    {
        private readonly McpPlugin _plugin;
        private bool _disposed = false;

        public CheatEngineDependencyScope(McpPlugin plugin)
        {
            _plugin = plugin;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(CheatEngineController))
                return new CheatEngineController(new CheatEngineTools(_plugin));
            return null;
        }

        public System.Collections.Generic.IEnumerable<object> GetServices(Type serviceType)
        {
            return new object[0];
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here if any
                }
                _disposed = true;
            }
        }
    }
}
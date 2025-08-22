using System;
using System.Web.Http;
using Owin;
using System.ComponentModel.DataAnnotations;
using Swashbuckle.Application;

namespace CeMCP
{
    [RoutePrefix("api/cheatengine")]
    public class CheatEngineController : ApiController
    {
        private readonly CheatEngineTools _tools;

        public CheatEngineController(CheatEngineTools tools)
        {
            _tools = tools;
        }

        [HttpPost]
        [Route("execute-lua")]
        public LuaResponse ExecuteLua([FromBody] LuaRequest request)
        {
            return _tools.ExecuteLua(request);
        }
    }

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
        }
    }

    public class CheatEngineDependencyScope : System.Web.Http.Dependencies.IDependencyScope
    {
        private readonly McpPlugin _plugin;

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
        }
    }
}
#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Owin;
using ModelContextProtocol.Server;
using Owin;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CeMCP
{
    public class OwinStartup
    {
        public void Configuration(IAppBuilder app)
        {
            app.Use(async (context, next) =>
            {
                // Add CORS headers
                context.Response.Headers.Add("Access-Control-Allow-Origin", new[] { "*" });
                context.Response.Headers.Add("Access-Control-Allow-Methods", new[] { "POST, GET, OPTIONS" });
                context.Response.Headers.Add("Access-Control-Allow-Headers", new[] { "Content-Type" });

                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.StatusCode = 200;
                    return;
                }

                if (context.Request.Method == "POST")
                {
                    using var reader = new StreamReader(context.Request.Body);
                    var requestBody = await reader.ReadToEndAsync();

                    // For now, return basic MCP responses - the SDK doesn't expose ProcessRequestAsync publicly
                    var response = new
                    {
                        jsonrpc = "2.0",
                        result = new
                        {
                            message = "MCP Server received request",
                            request = requestBody
                        }
                    };

                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                }
                else
                {
                    var serverInfo = new
                    {
                        name = "Cheat Engine MCP Server",
                        version = "1.0.0",
                        status = "running",
                        protocol = "Model Context Protocol",
                        transport = "HTTP"
                    };

                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(serverInfo));
                }
            });
        }
    }
}
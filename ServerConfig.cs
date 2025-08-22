#nullable enable
using System;

namespace CeMCP
{
    public static class ServerConfig
    {
        public static string Host { get; set; } = "localhost";
        public static int Port { get; set; } = 6300;
        public static string BaseUrl => $"http://{Host}:{Port}";
        public static string ServerName { get; set; } = "Cheat Engine MCP Server";
        public static string Version { get; set; } = "1.0.0";
        public static string Protocol { get; set; } = "Model Context Protocol";
        public static string Transport { get; set; } = "HTTP";

        public static void LoadFromEnvironment()
        {
            var hostEnv = Environment.GetEnvironmentVariable("MCP_HOST");
            if (!string.IsNullOrEmpty(hostEnv))
                Host = hostEnv;

            var portEnv = Environment.GetEnvironmentVariable("MCP_PORT");
            if (!string.IsNullOrEmpty(portEnv) && int.TryParse(portEnv, out int port))
                Port = port;
        }
    }
}
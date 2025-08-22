#nullable enable
using System;
using System.IO;
using System.Text.Json;

namespace CeMCP
{
    public static class ServerConfig
    {
        public static string Host { get; set; } = "127.0.0.1";
        public static int Port { get; set; } = 6300;
        public static string BaseUrl => $"http://{Host}:{Port}";
        public static string ServerName { get; set; } = "Cheat Engine MCP Server";

        private static string ConfigFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "CeMCP", "config.json");

        public static void LoadFromEnvironment()
        {
            var hostEnv = Environment.GetEnvironmentVariable("MCP_HOST");
            if (!string.IsNullOrEmpty(hostEnv))
                Host = hostEnv;

            var portEnv = Environment.GetEnvironmentVariable("MCP_PORT");
            if (!string.IsNullOrEmpty(portEnv) && int.TryParse(portEnv, out int port))
                Port = port;
        }

        public static void LoadFromFile()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<ConfigData>(json);
                    if (config != null)
                    {
                        Host = config.Host ?? Host;
                        Port = config.Port > 0 ? config.Port : Port;
                        ServerName = config.ServerName ?? ServerName;
                    }
                }
            }
            catch
            {
                // If loading fails, use defaults
            }
        }

        public static void SaveToFile()
        {
            try
            {
                var configDir = Path.GetDirectoryName(ConfigFilePath);
                if (configDir != null && !Directory.Exists(configDir))
                    Directory.CreateDirectory(configDir);

                var config = new ConfigData
                {
                    Host = Host,
                    Port = Port,
                    ServerName = ServerName
                };

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch
            {
                // Ignore save errors for now
            }
        }

        private class ConfigData
        {
            public string? Host { get; set; }
            public int Port { get; set; }
            public string? ServerName { get; set; }
        }
    }
}
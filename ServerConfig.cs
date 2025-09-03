using System;
using System.IO;
using System.Text.Json;

namespace CeMCP
{
    public static class ServerConfig
    {
        public static string ConfigHost { get; set; } = "127.0.0.1";
        public static int ConfigPort { get; set; } = 6300;
        public static string ConfigBaseUrl => $"http://{ConfigHost}:{ConfigPort}";
        public static string ConfigServerName { get; set; } = "Cheat Engine MCP Server";

        private static string ConfigFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CeMCP", "config.json");

        public static void LoadFromEnvironment()
        {
            var hostEnv = Environment.GetEnvironmentVariable("MCP_HOST");
            if (!string.IsNullOrEmpty(hostEnv))
                ConfigHost = hostEnv;

            var portEnv = Environment.GetEnvironmentVariable("MCP_PORT");
            if (!string.IsNullOrEmpty(portEnv) && int.TryParse(portEnv, out int port))
                ConfigPort = port;
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
                        ConfigHost = config.Host ?? ConfigHost;
                        ConfigPort = config.Port > 0 ? config.Port : ConfigPort;
                        ConfigServerName = config.ServerName ?? ConfigServerName;
                    }
                }
            }
            catch
            {
                // Hi error
                // If loading fails, use defaults
                // Goodbye error
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
                    Host = ConfigHost,
                    Port = ConfigPort,
                    ServerName = ConfigServerName
                };

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch
            {
                // Ignore save errors for now
            }
        }

        private sealed class ConfigData
        {
            public string? Host { get; set; }
            public int Port { get; set; }
            public string? ServerName { get; set; }
        }
    }
}
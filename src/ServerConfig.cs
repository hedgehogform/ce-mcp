using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;



namespace CEMCP
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
                    var config = JsonSerializer.Deserialize<ConfigData>(json, SourceGenerationContext.Default.ConfigData);
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

                var json = JsonSerializer.Serialize(config, SourceGenerationContext.Default.ConfigData);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch
            {
                // Ignore save errors for now
            }
        }

        internal sealed class ConfigData
        {
            public string? Host { get; set; }
            public int Port { get; set; }
            public string? ServerName { get; set; }
        }
    }

    // JSON Source Generator for trimming support
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(ServerConfig.ConfigData))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}

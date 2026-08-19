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
            if (!string.IsNullOrEmpty(portEnv) &&
                int.TryParse(portEnv, out int port) &&
                port is >= 1 and <= 65535)
            {
                ConfigPort = port;
            }
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
                        ConfigPort = config.Port is >= 1 and <= 65535 ? config.Port : ConfigPort;
                        ConfigServerName = config.ServerName ?? ConfigServerName;
                    }
                }
            }
            catch
            {
                // Keep the current defaults when the persisted file cannot be read.
            }
        }

        public static void SaveToFile()
        {
            string configDir = Path.GetDirectoryName(ConfigFilePath)
                ?? throw new InvalidOperationException("Could not resolve the configuration directory.");
            Directory.CreateDirectory(configDir);

            var config = new ConfigData
            {
                Host = ConfigHost,
                Port = ConfigPort,
                ServerName = ConfigServerName
            };

            string json = JsonSerializer.Serialize(config, SourceGenerationContext.Default.ConfigData);
            File.WriteAllText(ConfigFilePath, json);
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

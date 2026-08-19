using CEMCP;
using CESDK;

namespace CeMCP.Tests;

[TestClass]
[DoNotParallelize]
public sealed class PluginLoggerTests
{
    [TestMethod]
    public void PluginLogger_InfoAndException_WritesCanonicalNLogFile()
    {
        string expectedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CeMCP",
            "ce-mcp.log");
        Assert.AreEqual(expectedPath, PluginLogger.LogFilePath, "Logger path should match the canonical CeMCP log path.");

        string infoMarker = $"plugin-logger-info-{Guid.NewGuid():N}";
        string exceptionMarker = $"plugin-logger-exception-{Guid.NewGuid():N}";
        PluginLogger.Log(infoMarker);
        PluginLogger.LogException(new InvalidOperationException(exceptionMarker));
        PluginLogger.Flush();

        Assert.IsTrue(File.Exists(expectedPath), $"Expected NLog to create '{expectedPath}'.");
        string log = File.ReadAllText(expectedPath);
        StringAssert.Contains(log, infoMarker, "Info entry was not written to the canonical log.");
        StringAssert.Contains(log, exceptionMarker, "Exception entry was not written to the canonical log.");
        StringAssert.Contains(log, "InvalidOperationException", "Exception type was not included in the NLog entry.");
    }

    [TestMethod]
    public async Task McpServer_Start_RoutesAspNetCoreLogsToCanonicalNLogFile()
    {
        int port;
        using (var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0))
        {
            listener.Start();
            port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        }

        string url = $"http://127.0.0.1:{port}";
        var server = new McpServer();
        try
        {
            server.Start(url);

            bool foundStartupEntry = false;
            for (int attempt = 0; attempt < 50 && !foundStartupEntry; attempt++)
            {
                await Task.Delay(100);
                PluginLogger.Flush();
                if (File.Exists(PluginLogger.LogFilePath))
                {
                    string log = File.ReadAllText(PluginLogger.LogFilePath);
                    foundStartupEntry = log.Contains(
                        $"Now listening on: {url}",
                        StringComparison.Ordinal);
                }
            }

            Assert.IsTrue(foundStartupEntry, "ASP.NET Core startup logs were not routed through NLog.");
            await Task.Delay(100);
            PluginLogger.Flush();
            string startupLog = File.ReadAllText(PluginLogger.LogFilePath);
            int startupIndex = startupLog.LastIndexOf($"Now listening on: {url}", StringComparison.Ordinal);
            Assert.IsTrue(startupIndex >= 0, "Could not locate this server instance's startup entry.");
            Assert.IsFalse(
                startupLog.Substring(startupIndex).Contains("Hosting startup assembly exception", StringComparison.Ordinal),
                "ASP.NET Core should receive ce-mcp's application name instead of probing an empty startup assembly.");
        }
        finally
        {
            server.Stop();
            await Task.Delay(250);
        }
    }
}

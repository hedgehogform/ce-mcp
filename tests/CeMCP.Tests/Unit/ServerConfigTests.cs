using CEMCP;
using CEMCP.Models;

namespace CeMCP.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ServerConfigTests
{
    [TestInitialize]
    public void Reset()
    {
        Environment.SetEnvironmentVariable("MCP_HOST", null);
        Environment.SetEnvironmentVariable("MCP_PORT", null);
        ServerConfig.ConfigHost = "127.0.0.1";
        ServerConfig.ConfigPort = 6300;
        ServerConfig.ConfigServerName = "Cheat Engine MCP Server";
    }

    [TestCleanup]
    public void Cleanup() => Reset();

    [TestMethod]
    public void ConfigBaseUrl_UsesConfiguredHostAndPort()
    {
        ServerConfig.ConfigHost = "0.0.0.0";
        ServerConfig.ConfigPort = 7777;

        Assert.AreEqual("http://0.0.0.0:7777", ServerConfig.ConfigBaseUrl);
    }

    [TestMethod]
    public void LoadFromEnvironment_OverridesHostAndValidPort()
    {
        Environment.SetEnvironmentVariable("MCP_HOST", "localhost");
        Environment.SetEnvironmentVariable("MCP_PORT", "6400");

        ServerConfig.LoadFromEnvironment();

        Assert.AreEqual("localhost", ServerConfig.ConfigHost);
        Assert.AreEqual(6400, ServerConfig.ConfigPort);
    }

    [TestMethod]
    public void LoadFromEnvironment_IgnoresInvalidPort()
    {
        ServerConfig.ConfigPort = 6300;
        Environment.SetEnvironmentVariable("MCP_PORT", "not-a-port");

        ServerConfig.LoadFromEnvironment();

        Assert.AreEqual(6300, ServerConfig.ConfigPort);
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("65536")]
    [DataRow("-1")]
    public void LoadFromEnvironment_OutOfRangePort_KeepsCurrentValue(string value)
    {
        ServerConfig.ConfigPort = 6300;
        Environment.SetEnvironmentVariable("MCP_PORT", value);

        ServerConfig.LoadFromEnvironment();

        Assert.AreEqual(6300, ServerConfig.ConfigPort);
    }

    [TestMethod]
    public void ConfigurationModel_BaseUrl_UsesCanonicalRootEndpoint()
    {
        var model = new ConfigurationModel
        {
            Host = "localhost",
            Port = 6400,
        };

        Assert.AreEqual("http://localhost:6400/", model.BaseUrl);
    }

    [TestMethod]
    [DataRow("", 6300, "server", "Host")]
    [DataRow("not/a/host", 6300, "server", "Host")]
    [DataRow("localhost", 0, "server", "Port")]
    [DataRow("localhost", 65536, "server", "Port")]
    [DataRow("localhost", 6300, " ", "ServerName")]
    public void ConfigurationModel_InvalidSettings_RejectsBeforeSaving(
        string host,
        int port,
        string serverName,
        string expectedParameter)
    {
        var model = new ConfigurationModel
        {
            Host = host,
            Port = port,
            ServerName = serverName,
        };

        ArgumentException exception = Assert.Throws<ArgumentException>(
            model.SaveToServerConfig);

        Assert.AreEqual(expectedParameter, exception.ParamName);
    }
}

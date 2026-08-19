using System.Text.Json.Nodes;
using ModelContextProtocol.Client;

namespace CeMCP.Tests;

[TestClass]
[TestCategory("Live")]
[DoNotParallelize]
public sealed class LiveMcpTests
{
    private static string ServerUrl =>
        Environment.GetEnvironmentVariable("CE_MCP_URL") ?? "http://localhost:6300/";

    [TestInitialize]
    public void RequireLiveTestsEnabled()
    {
        if (Environment.GetEnvironmentVariable("CE_MCP_LIVE") != "1")
        {
            Assert.Inconclusive(
                "Live CE MCP tests are opt-in. Load ce-mcp.dll in Cheat Engine, start the MCP server, set CE_MCP_LIVE=1, optionally set CE_MCP_URL, then run dotnet test --filter TestCategory=Live.");
        }
    }

    [TestMethod]
    public async Task LiveServer_InitializesAndListsExpectedTools()
    {
        await using LiveMcpClient client = await LiveMcpClient.ConnectAsync(ServerUrl);

        IList<McpClientTool> tools = await client.ListToolsAsync();
        List<string> toolNames = tools
            .Select(tool => tool.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToList();

        Assert.AreEqual(LiveMcpClient.ProtocolVersion, client.NegotiatedProtocolVersion);
        CollectionAssert.Contains(toolNames, "get_plugin_version");
        CollectionAssert.Contains(toolNames, "execute_lua");
        CollectionAssert.Contains(toolNames, "memory_scan");
    }

    [TestMethod]
    public async Task LiveServer_GetPluginVersionReportsLoadedPlugin()
    {
        await using LiveMcpClient client = await LiveMcpClient.ConnectAsync(ServerUrl);

        JsonNode? payload = await client.CallToolAsync("get_plugin_version");

        Assert.IsTrue(payload?["success"]?.GetValue<bool>());
        Assert.IsFalse(string.IsNullOrWhiteSpace(payload?["version"]?.GetValue<string>()));
        StringAssert.Contains(payload?["location"]?.GetValue<string>() ?? "", "ce-mcp.dll");
    }

    [TestMethod]
    public async Task LiveServer_ExecuteLuaRunsOnCeMainThread()
    {
        await using LiveMcpClient client = await LiveMcpClient.ConnectAsync(ServerUrl);

        JsonNode? payload = await client.CallToolAsync("execute_lua", new Dictionary<string, object?>
        {
            ["script"] = "return { mainThread = inMainThread(), marker = 'ce-mcp-live' }"
        });

        Assert.IsTrue(payload?["success"]?.GetValue<bool>());
        Assert.IsTrue(payload?["result"]?["mainThread"]?.GetValue<bool>());
        Assert.AreEqual("ce-mcp-live", payload?["result"]?["marker"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task LiveServer_GetCurrentProcessIsSafeWithoutTarget()
    {
        await using LiveMcpClient client = await LiveMcpClient.ConnectAsync(ServerUrl);

        JsonNode? payload = await client.CallToolAsync("get_current_process");

        Assert.IsTrue(payload?["success"]?.GetValue<bool>());
        Assert.IsNotNull(payload?["isOpen"]);
    }

    [TestMethod]
    public async Task LiveServer_AobScanPreservesEmptyAndZeroOptionalArguments()
    {
        await using LiveMcpClient client = await LiveMcpClient.ConnectAsync(ServerUrl);
        (ulong address, byte[] bytes) = await GetReadableModuleSampleAsync(client, 16);
        string pattern = string.Join(" ", bytes.Select(value => value.ToString("X2")));

        JsonNode? payload = await client.CallToolAsync("aob_scan", new Dictionary<string, object?>
        {
            ["pattern"] = pattern,
            ["protectionFlags"] = "",
            ["alignmentType"] = 0,
            ["alignmentParam"] = ""
        });

        Assert.IsTrue(payload?["success"]?.GetValue<bool>());
        JsonArray? addresses = payload?["addresses"] as JsonArray;
        Assert.IsNotNull(addresses);
        Assert.IsTrue(
            addresses.Any(node => string.Equals(
                node?.GetValue<string>(),
                $"0x{address:X}",
                StringComparison.OrdinalIgnoreCase)),
            $"AOB scan did not return the sampled module address 0x{address:X}.");
    }

    [TestMethod]
    public async Task LiveServer_NamedMemoryScanPreservesEmptyZeroAndFalseArguments()
    {
        await using LiveMcpClient client = await LiveMcpClient.ConnectAsync(ServerUrl);
        (ulong address, byte[] bytes) = await GetReadableModuleSampleAsync(client, 1);
        const string scannerName = "ce-mcp-live-empty-defaults";

        await client.CallToolAsync("reset_memory_scan", new Dictionary<string, object?>
        {
            ["scannerName"] = scannerName
        });

        try
        {
            JsonNode? payload = await client.CallToolAsync("memory_scan", new Dictionary<string, object?>
            {
                ["scanOption"] = "soExactValue",
                ["varType"] = "vtByte",
                ["input1"] = bytes[0].ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["input2"] = "",
                ["startAddress"] = address,
                ["stopAddress"] = address + 1,
                ["protectionFlags"] = "",
                ["alignmentType"] = "fsmNotAligned",
                ["alignmentParam"] = "",
                ["isHexadecimalInput"] = false,
                ["isUnicodeScan"] = false,
                ["isCaseSensitive"] = false,
                ["isPercentageScan"] = false,
                ["scannerName"] = scannerName
            });

            Assert.IsTrue(payload?["success"]?.GetValue<bool>());
            Assert.IsTrue(payload?["count"]?.GetValue<int>() > 0);
            JsonArray? results = payload?["results"] as JsonArray;
            Assert.IsNotNull(results);
            Assert.IsTrue(
                results.Any(result => string.Equals(
                    result?["address"]?.GetValue<string>(),
                    $"0x{address:X}",
                    StringComparison.OrdinalIgnoreCase)),
                $"Memory scan did not return the sampled module address 0x{address:X}.");
        }
        finally
        {
            await client.CallToolAsync("reset_memory_scan", new Dictionary<string, object?>
            {
                ["scannerName"] = scannerName
            });
        }
    }

    [TestMethod]
    public async Task LiveServer_NamedMemoryNextScanReusesFoundListSafely()
    {
        await using LiveMcpClient client = await LiveMcpClient.ConnectAsync(ServerUrl);
        (ulong address, byte[] bytes) = await GetReadableModuleSampleAsync(client, 1);
        const string scannerName = "ce-mcp-live-next-scan-lifecycle";
        var arguments = new Dictionary<string, object?>
        {
            ["scanOption"] = "soExactValue",
            ["varType"] = "vtByte",
            ["input1"] = bytes[0].ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["input2"] = "",
            ["startAddress"] = address,
            ["stopAddress"] = address + 1,
            ["protectionFlags"] = "",
            ["alignmentType"] = "fsmNotAligned",
            ["alignmentParam"] = "",
            ["isHexadecimalInput"] = false,
            ["isUnicodeScan"] = false,
            ["isCaseSensitive"] = false,
            ["isPercentageScan"] = false,
            ["scannerName"] = scannerName
        };

        await client.CallToolAsync("reset_memory_scan", new Dictionary<string, object?>
        {
            ["scannerName"] = scannerName
        });

        try
        {
            JsonNode? first = await client.CallToolAsync("memory_scan", arguments);
            Assert.IsTrue(first?["success"]?.GetValue<bool>());
            Assert.IsTrue(first?["count"]?.GetValue<int>() > 0);

            JsonNode? next = await client.CallToolAsync("memory_scan", arguments);
            Assert.IsTrue(next?["success"]?.GetValue<bool>());
            Assert.IsTrue(next?["count"]?.GetValue<int>() > 0);
            JsonArray? results = next?["results"] as JsonArray;
            Assert.IsNotNull(results);
            Assert.IsTrue(
                results.Any(result => string.Equals(
                    result?["address"]?.GetValue<string>(),
                    $"0x{address:X}",
                    StringComparison.OrdinalIgnoreCase)),
                $"Next scan did not retain sampled module address 0x{address:X}.");
        }
        finally
        {
            await client.CallToolAsync("reset_memory_scan", new Dictionary<string, object?>
            {
                ["scannerName"] = scannerName
            });
        }
    }

    [TestMethod]
    [TestCategory("DebuggerLive")]
    [Timeout(180_000)]
    public async Task LiveServer_DebuggerReattachesRepeatedlyToStableTarget()
    {
        if (Environment.GetEnvironmentVariable("CE_MCP_DEBUGGER_LIVE") != "1")
            Assert.Inconclusive("Set CE_MCP_DEBUGGER_LIVE=1 and attach a disposable classic target.");

        await using LiveMcpClient client = await LiveMcpClient.ConnectAsync(ServerUrl);
        JsonNode? process = await client.CallToolAsync("get_current_process");
        Assert.IsTrue(process?["isOpen"]?.GetValue<bool>());
        int processId = process?["processId"]?.GetValue<int>()
            ?? throw new AssertFailedException("Current process response did not include a PID.");

        JsonNode? initialStatus = await client.CallToolAsync("dbg_is_debugging");
        if (initialStatus?["is_debugging"]?.GetValue<bool>() == true)
            await client.CallToolAsync("dbg_exit");

        string? allocationAddress = null;

        try
        {
            for (int cycle = 1; cycle <= 5; cycle++)
            {
                JsonNode? attached = await client.CallToolAsync(
                    "dbg_start",
                    new Dictionary<string, object?> { ["debugInterface"] = 2 });
                Assert.IsTrue(attached?["success"]?.GetValue<bool>(), $"Attach cycle {cycle} failed: {attached}");
                Assert.AreEqual(processId, attached?["processId"]?.GetValue<int>());
                Assert.AreEqual(2, attached?["requestedDebuggerInterface"]?.GetValue<int>());
                Assert.AreEqual(2, attached?["debuggerInterface"]?.GetValue<int>());
                Assert.IsFalse(attached?["usedFallback"]?.GetValue<bool>());

                JsonNode? idempotent = await client.CallToolAsync(
                    "dbg_start",
                    new Dictionary<string, object?> { ["debugInterface"] = 2 });
                Assert.IsTrue(idempotent?["alreadyAttached"]?.GetValue<bool>());
                Assert.AreEqual(processId, idempotent?["processId"]?.GetValue<int>());

                JsonNode? detached = await client.CallToolAsync("dbg_exit");
                Assert.IsTrue(detached?["success"]?.GetValue<bool>(), $"Detach cycle {cycle} failed: {detached}");
                Assert.AreEqual(processId, detached?["processId"]?.GetValue<int>());
                Assert.IsFalse(detached?["isDebugging"]?.GetValue<bool>());

                JsonNode? current = await client.CallToolAsync("get_current_process");
                Assert.AreEqual(processId, current?["processId"]?.GetValue<int>());
                Assert.IsTrue(current?["isOpen"]?.GetValue<bool>());
            }

            JsonNode? allocation = await client.CallToolAsync(
                "allocate_memory",
                new Dictionary<string, object?> { ["size"] = 64UL });
            allocationAddress = allocation?["address"]?.GetValue<string>()
                ?? throw new AssertFailedException("Debugger smoke allocation returned no address.");

            JsonNode? smokeAttach = await client.CallToolAsync(
                "dbg_start",
                new Dictionary<string, object?> { ["debugInterface"] = 2 });
            Assert.AreEqual(2, smokeAttach?["debuggerInterface"]?.GetValue<int>());

            JsonNode? breakpoint = await client.CallToolAsync(
                "dbg_add_bp",
                new Dictionary<string, object?>
                {
                    ["address"] = allocationAddress,
                    ["size"] = 1,
                    ["trigger"] = "execute",
                    ["trackHits"] = false
                });
            Assert.IsTrue(breakpoint?["success"]?.GetValue<bool>());

            JsonNode? breakpoints = await client.CallToolAsync("dbg_bps");
            JsonArray? addresses = breakpoints?["breakpoints"] as JsonArray;
            Assert.IsNotNull(addresses);
            Assert.IsTrue(addresses.Any(value => string.Equals(
                value?.GetValue<string>(),
                allocationAddress,
                StringComparison.OrdinalIgnoreCase)));

            JsonNode? deleted = await client.CallToolAsync(
                "dbg_delete_bp",
                new Dictionary<string, object?> { ["address"] = allocationAddress });
            Assert.IsTrue(deleted?["success"]?.GetValue<bool>());

            JsonNode? smokeExit = await client.CallToolAsync("dbg_exit");
            Assert.IsFalse(smokeExit?["isDebugging"]?.GetValue<bool>());
        }
        finally
        {
            JsonNode? status = await client.CallToolAsync("dbg_is_debugging");
            if (status?["is_debugging"]?.GetValue<bool>() == true)
                await client.CallToolAsync("dbg_exit");
            if (allocationAddress is not null)
            {
                await client.CallToolAsync(
                    "dbg_delete_bp",
                    new Dictionary<string, object?> { ["address"] = allocationAddress });
                await client.CallToolAsync(
                    "free_memory",
                    new Dictionary<string, object?> { ["address"] = allocationAddress });
            }
        }
    }

    private static async Task<(ulong Address, byte[] Bytes)> GetReadableModuleSampleAsync(
        LiveMcpClient client,
        int byteCount)
    {
        JsonNode? process = await client.CallToolAsync("get_current_process");
        if (process?["isOpen"]?.GetValue<bool>() != true)
        {
            Assert.Inconclusive(
                "Scan regression tests require Cheat Engine to have a readable target process attached.");
        }

        JsonNode? modulesPayload = await client.CallToolAsync("enum_modules");
        JsonArray? modules = modulesPayload?["modules"] as JsonArray;
        if (modules is not null)
        {
            foreach (JsonNode? module in modules)
            {
                string? addressText = module?["address"]?.GetValue<string>();
                if (!TryParseHexAddress(addressText, out ulong address))
                    continue;

                JsonNode? readPayload = await client.CallToolAsync("read_memory", new Dictionary<string, object?>
                {
                    ["address"] = addressText,
                    ["dataType"] = "bytes",
                    ["byteCount"] = byteCount
                });

                if (readPayload?["success"]?.GetValue<bool>() != true)
                    continue;

                byte[] bytes = ExtractBytes(readPayload["value"]);
                if (bytes.Length == byteCount)
                    return (address, bytes);
            }
        }

        Assert.Inconclusive("No readable module base was available for the scan regression tests.");
        return default;
    }

    private static bool TryParseHexAddress(string? value, out ulong address)
    {
        string text = value?.StartsWith("0x", StringComparison.OrdinalIgnoreCase) == true
            ? value[2..]
            : value ?? "";
        return ulong.TryParse(
            text,
            System.Globalization.NumberStyles.HexNumber,
            System.Globalization.CultureInfo.InvariantCulture,
            out address);
    }

    private static byte[] ExtractBytes(JsonNode? value)
    {
        if (value is JsonArray array)
            return array.Select(node => node?.GetValue<byte>() ?? 0).ToArray();

        if (value is JsonValue)
            return Convert.FromBase64String(value.GetValue<string>());

        return [];
    }
}

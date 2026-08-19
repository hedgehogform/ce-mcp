using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using ModelContextProtocol.Client;

namespace CeMCP.Tests;

[TestClass]
[TestCategory("Live")]
[TestCategory("NotepadLive")]
[DoNotParallelize]
public sealed class NotepadMcpTests
{
    private const uint WmNull = 0x0000;

    [DllImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);

    private static string ServerUrl =>
        Environment.GetEnvironmentVariable("CE_MCP_URL") ?? "http://localhost:6300/";

    private static readonly IReadOnlyDictionary<string, string> ExcludedTools =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["open_foreground_process"] = "Would retarget Cheat Engine away from the disposable Notepad process.",
            ["enable_symbols"] = "May download Windows PDBs or enable kernel symbols; not process-local.",
            ["inject_library"] = "Requires a purpose-built trusted native test payload.",
            ["inject_dotnet_assembly"] = "Requires a purpose-built trusted managed test payload.",
            ["dbvm_initialize"] = "May offload the host OS and is not Notepad-scoped.",
            ["dbvm_read_physical"] = "Physical-memory access is host-wide, not Notepad-scoped.",
            ["dbvm_write_physical"] = "Physical-memory writes are host-wide, not Notepad-scoped.",
            ["dbvm_watch"] = "DBVM physical watches are host-wide, not Notepad-scoped.",
            ["dbvm_watch_log"] = "Requires a host-wide DBVM watch.",
            ["dbvm_watch_stop"] = "Requires a host-wide DBVM watch."
        };

    private readonly HashSet<string> attemptedTools = new(StringComparer.Ordinal);
    private readonly HashSet<ulong> allocations = [];
    private readonly List<string> ownedFiles = [];
    private LiveMcpClient? client;
    private int notepadProcessId;
    private string? baselineTablePath;
    private string? testTablePath;
    private string? dumpPath;
    private string? symbolName;
    private string? structureName;
    private readonly string valueScanner = $"ce-mcp-notepad-value-{Guid.NewGuid():N}";
    private readonly string stringScanner = $"ce-mcp-notepad-string-{Guid.NewGuid():N}";

    [TestInitialize]
    public void RequireNotepadLiveTestsEnabled()
    {
        if (Environment.GetEnvironmentVariable("CE_MCP_LIVE") != "1" ||
            Environment.GetEnvironmentVariable("CE_MCP_NOTEPAD_LIVE") != "1")
        {
            Assert.Inconclusive(
                "Dedicated Notepad tests require CE_MCP_LIVE=1 and CE_MCP_NOTEPAD_LIVE=1, a freshly loaded ce-mcp.dll, and the MCP server running.");
        }
    }

    [TestMethod]
    [Timeout(300_000)]
    public async Task AllFeasibleTools_RunAgainstDisposableNotepad()
    {
        client = await LiveMcpClient.ConnectAsync(ServerUrl);
        IList<McpClientTool> listedTools = await client.ListToolsAsync();

        try
        {
            await RunProcessAndUtilityScenarios();
            await RunMemoryAndScanScenarios();
            await RunSymbolAndAnalysisScenarios();
            await RunCeStateScenarios();
            await RunInjectionGenerationScenarios();
            await RunDebuggerScenarios();
            await CallSuccess("dbvm_status");
        }
        finally
        {
            await CleanupOwnedState();
        }

        AssertCompleteCoverage(listedTools);
    }

    private async Task RunProcessAndUtilityScenarios()
    {
        await CallSuccess("get_plugin_version");
        await CallSuccess("get_process_list");

        await CallSuccess("create_process", Args(
            ("path", @"C:\Windows\System32\notepad.exe"),
            ("parameters", ""),
            ("debug", false),
            ("breakOnEntryPoint", false)));

        int launchedProcessId = await WaitForNotepadWindowProcessId();
        await CallSuccess("open_process", Args(("process", launchedProcessId.ToString(CultureInfo.InvariantCulture))));
        JsonNode process = await WaitForCurrentProcess();
        notepadProcessId = Required<int>(process, "processId");
        Assert.AreEqual(launchedProcessId, notepadProcessId);
        Assert.IsTrue(
            Required<string>(process, "processName").Contains("notepad", StringComparison.OrdinalIgnoreCase));
        await CallSuccess("get_current_process");
        await CallSuccess("get_process_state");

        await CallSuccess("pause_process");
        JsonNode paused = await CallSuccess("get_process_state");
        Assert.IsTrue(Required<bool>(paused, "paused"));
        await CallSuccess("resume_process");

        JsonNode lua = await CallSuccess("execute_lua", Args(
            ("script", "return { pid=getOpenedProcessID(), marker='ce-mcp-notepad-live', mainThread=inMainThread() }")));
        Assert.AreEqual(notepadProcessId, Required<int>(lua["result"]!, "pid"));
        Assert.IsTrue(Required<bool>(lua["result"]!, "mainThread"));

        await CallSuccess("convert_string", Args(("input", "ce-mcp"), ("conversionType", "md5")));
        await CallSuccess("convert_string", Args(("input", "ce-mcp"), ("conversionType", "ansitoutf8")));
        await CallSuccess("convert_string", Args(("input", "ce-mcp"), ("conversionType", "utf8toansi")));

        baselineTablePath = OwnTempFile("baseline", ".CT");
        await CallSuccess("save_cheat_table", Args(("filename", baselineTablePath)));
    }

    private async Task RunMemoryAndScanScenarios()
    {
        ulong blockA = await Allocate(4096);
        ulong blockB = await Allocate(4096);
        ulong shared = RequiredAddress(await CallSuccess("allocate_shared_memory", Args(
            ("name", $"ce-mcp-notepad-{Guid.NewGuid():N}"),
            ("size", 4096UL))));
        allocations.Add(shared);

        string a = Hex(blockA);
        string b = Hex(blockB);
        string sharedAddress = Hex(shared);

        await CallSuccess("enum_memory_regions", Args(("filter", "committed")));
        await CallSuccess("get_memory_protection", Args(("address", a)));
        await CallSuccess("set_memory_protection", Args(
            ("address", a), ("size", 4096UL), ("read", true), ("write", true), ("execute", true)));

        await RoundTrip("byte", blockA + 0, "171");
        await RoundTrip("int16", blockA + 8, "-12345");
        await RoundTrip("int32", blockA + 16, "305419896");
        await RoundTrip("int64", blockA + 24, "1234605616436508552");
        await RoundTrip("float", blockA + 40, "123.5");
        await RoundTrip("double", blockA + 48, "9876.125");
        await RoundTrip("string", blockA + 64, "CE_MCP_NOTEPAD_STRING_MARKER_7B4F", maxLength: 64);
        await RoundTrip("bytes", blockA + 160, "D3 4A 91 C7 2E 68 B5 F0 19 AC 73 E1 56 8D 02 BF", byteCount: 16);

        await CallSuccess("write_memory", Args(
            ("address", Hex(blockA + 256)),
            ("dataType", "int64"),
            ("value", blockB.ToString(CultureInfo.InvariantCulture))));
        JsonNode chain = await CallSuccess("read_pointer_chain", Args(
            ("baseAddress", Hex(blockA + 256)),
            ("offsets", new long[] { 0 })));
        Assert.IsTrue(Required<bool>(chain, "valid"));
        Assert.AreEqual(b, Required<string>(chain, "address"));

        JsonNode references = await CallSuccess("find_pointer_references", Args(
            ("targetAddress", b),
            ("startAddress", a),
            ("stopAddress", Hex(blockA + 4096)),
            ("pointerSize", 8),
            ("protectionFlags", "*W*X*C"),
            ("maxResults", 100)));
        Assert.IsTrue(Required<int>(references, "count") > 0);

        await CallSuccess("hash_memory", Args(("address", a), ("size", 512UL)));
        await CallSuccess("copy_memory", Args(
            ("sourceAddress", a), ("size", 512UL), ("destinationAddress", b), ("method", 0)));
        JsonNode comparison = await CallSuccess("compare_memory", Args(
            ("firstAddress", a), ("secondAddress", b), ("size", 512UL), ("method", 0)));
        Assert.IsTrue(Required<bool>(comparison, "equal"));

        dumpPath = OwnTempFile("memory", ".bin");
        await CallSuccess("dump_memory", Args(("filename", dumpPath), ("address", a), ("size", 512UL)));
        await CallSuccess("write_memory", Args(
            ("address", b), ("dataType", "bytes"), ("value", "00 00 00 00 00 00 00 00")));
        await CallSuccess("load_memory", Args(("filename", dumpPath), ("destinationAddress", b)));
        comparison = await CallSuccess("compare_memory", Args(
            ("firstAddress", a), ("secondAddress", b), ("size", 512UL)));
        Assert.IsTrue(Required<bool>(comparison, "equal"));

        string pattern = "D3 4A 91 C7 2E 68 B5 F0 19 AC 73 E1 56 8D 02 BF";
        JsonNode aob = await CallSuccess("aob_scan", Args(
            ("pattern", pattern), ("protectionFlags", ""), ("alignmentType", 0), ("alignmentParam", "")));
        AssertArrayContains(aob["addresses"] as JsonArray, Hex(blockA + 160));

        JsonNode unique = await CallSuccess("aob_scan_unique", Args(
            ("pattern", pattern), ("protectionFlags", ""), ("alignmentType", 0), ("alignmentParam", "")));
        Assert.IsTrue(Required<bool>(unique, "found"));

        JsonNode modules = await CallSuccess("enum_modules");
        JsonNode mainModule = RequiredArray(modules, "modules")
            .First(node => (node?["name"]?.GetValue<string>() ?? "").Contains("notepad", StringComparison.OrdinalIgnoreCase))!;
        string moduleName = Required<string>(mainModule, "name");
        ulong moduleBase = ParseAddress(Required<string>(mainModule, "address"));
        JsonNode moduleBytes = await CallSuccess("read_memory", Args(
            ("address", Hex(moduleBase)), ("dataType", "bytes"), ("byteCount", 16)));
        string modulePattern = string.Join(" ", ExtractBytes(moduleBytes["value"]).Select(value => value.ToString("X2")));
        JsonNode moduleUnique = await CallSuccess("aob_scan_module_unique", Args(
            ("moduleName", moduleName), ("pattern", modulePattern),
            ("protectionFlags", ""), ("alignmentType", 0), ("alignmentParam", "")));
        Assert.IsTrue(Required<bool>(moduleUnique, "found"));

        await CallSuccess("reset_memory_scan", Args(("scannerName", stringScanner)));
        JsonNode stringScan = await CallSuccess("string_scan", Args(
            ("value", "CE_MCP_NOTEPAD_STRING_MARKER_7B4F"),
            ("startAddress", blockA), ("stopAddress", blockA + 4096),
            ("protectionFlags", ""), ("unicode", false), ("caseSensitive", true),
            ("scannerName", stringScanner)));
        Assert.IsTrue(Required<int>(stringScan, "count") > 0);

        await CallSuccess("reset_memory_scan", Args(("scannerName", valueScanner)));
        JsonNode valueScan = await CallSuccess("memory_scan", Args(
            ("scanOption", "soExactValue"), ("varType", "vtDword"),
            ("input1", "305419896"), ("input2", ""),
            ("startAddress", blockA), ("stopAddress", blockA + 4096),
            ("protectionFlags", ""), ("alignmentType", "fsmNotAligned"),
            ("alignmentParam", ""), ("isHexadecimalInput", false),
            ("isUnicodeScan", false), ("isCaseSensitive", false),
            ("isPercentageScan", false), ("scannerName", valueScanner)));
        Assert.IsTrue(Required<int>(valueScan, "count") > 0);

        await CallSuccess("read_memory", Args(("address", sharedAddress), ("dataType", "byte")));
    }

    private async Task RunSymbolAndAnalysisScenarios()
    {
        ulong block = allocations.First();
        JsonNode modules = await CallSuccess("enum_modules");
        JsonNode mainModule = RequiredArray(modules, "modules")
            .First(node => (node?["name"]?.GetValue<string>() ?? "").Contains("notepad", StringComparison.OrdinalIgnoreCase))!;
        string moduleName = Required<string>(mainModule, "name");
        ulong moduleBase = ParseAddress(Required<string>(mainModule, "address"));

        await CallSuccess("get_module_size", Args(("moduleName", moduleName)));
        await CallSuccess("resolve_address", Args(("addressString", moduleName), ("local", false)));
        await CallSuccess("get_name_from_address", Args(("address", Hex(moduleBase))));
        await CallSuccess("get_rtti_class_name", Args(("address", Hex(block))));
        await CallSuccess("reinitialize_symbols", Args(("waitTillDone", true)));
        await CallSuccess("wait_for_symbols", Args(("level", "exports")));
        await CallSuccess("get_pointer_size");

        symbolName = $"ce_mcp_notepad_{Guid.NewGuid():N}";
        await CallSuccess("register_symbol", Args(
            ("name", symbolName), ("address", Hex(block)), ("doNotSave", true)));
        await CallSuccess("enum_registered_symbols");
        JsonNode? symbolInfo = null;
        foreach (string candidate in new[]
        {
            "kernel32.CreateFileW",
            "kernel32.dll.CreateFileW",
            "kernel32.dll!CreateFileW",
            "ntdll.RtlAllocateHeap",
            "ntdll.dll.RtlAllocateHeap",
            "ntdll.dll!RtlAllocateHeap"
        })
        {
            JsonNode result = await CallSuccess(
                "get_symbol_info",
                Args(("symbolName", candidate)),
                requireSuccess: false);
            if (result["success"]?.GetValue<bool>() == true)
            {
                symbolInfo = result;
                break;
            }
        }
        Assert.IsNotNull(symbolInfo, "No loaded Windows export produced getSymbolInfo metadata.");
        JsonNode resolved = await CallSuccess("resolve_address", Args(("addressString", symbolName)));
        Assert.AreEqual(Hex(block), Required<string>(resolved, "address"));
        await CallSuccess("unregister_symbol", Args(("name", symbolName)));
        symbolName = null;

        string codeExpression = moduleName;
        JsonNode disassembly = await CallSuccess("disassemble", Args(
            ("address", Hex(moduleBase)), ("requestType", "disassemble")));
        await CallSuccess("disassemble", Args(
            ("address", Hex(moduleBase)), ("requestType", "get-instruction-size")));
        await CallSuccess("disassemble_range", Args(("address", codeExpression), ("count", 16)));
        await CallSuccess("disassemble_bytes", Args(("hexBytes", "90 90 C3"), ("address", Hex(block))));
        await CallSuccess("get_previous_opcodes", Args(("address", Hex(moduleBase + 32)), ("count", 3)));
        await CallSuccess("get_function_range", Args(("address", codeExpression)));
        await CallSuccess("assemble", Args(
            ("instruction", "nop"), ("address", Hex(block)),
            ("assemblePreference", 0), ("skipRangeCheck", false)));
        await CallSuccess("analyze_code_range", Args(
            ("startAddress", Hex(moduleBase)), ("stopAddress", Hex(moduleBase + 128)),
            ("maxInstructions", 64)));
        await CallSuccess("search_disassembly", Args(
            ("startAddress", Hex(moduleBase)), ("stopAddress", Hex(moduleBase + 256)),
            ("query", "mov"), ("maxResults", 16), ("maxInstructions", 128)));
        await CallSuccess("set_comment", Args(
            ("address", Hex(moduleBase)), ("comment", "ce-mcp Notepad live test")));
        await CallSuccess("set_comment", Args(("address", Hex(moduleBase)), ("comment", "")));

        Assert.IsNotNull(disassembly);
    }

    private async Task RunCeStateScenarios()
    {
        ulong blockA = allocations.First();
        ulong blockB = allocations.Skip(1).First();

        await CallSuccess("clear_address_list");
        JsonNode added = await CallSuccess("add_memory_record", Args(
            ("description", "ce-mcp Notepad live record"),
            ("address", Hex(blockA)), ("varType", "vtDword"),
            ("value", "305419896"), ("offsets", ""), ("active", false)));
        int recordId = Required<int>(added["record"]!, "id");
        await CallSuccess("get_address_list");
        await CallSuccess("update_memory_record", Args(
            ("id", recordId), ("newDescription", "ce-mcp Notepad updated record"),
            ("newAddress", Hex(blockA + 16)), ("newVarType", "vtDword"),
            ("newValue", "305419896"), ("active", false), ("newOffsets", "")));
        await CallSuccess("delete_memory_record", Args(("id", recordId)));
        await CallSuccess("clear_address_list");

        structureName = $"ce_mcp_notepad_{Guid.NewGuid():N}";
        object[] elements =
        [
            new Dictionary<string, object?>
            {
                ["offset"] = 0L, ["name"] = "first", ["variableType"] = 2,
                ["byteSize"] = 4, ["childClassName"] = ""
            },
            new Dictionary<string, object?>
            {
                ["offset"] = 16L, ["name"] = "second", ["variableType"] = 2,
                ["byteSize"] = 4, ["childClassName"] = ""
            }
        ];
        await CallSuccess("create_structure", Args(
            ("name", structureName), ("elements", elements), ("isInternal", false)));
        await CallSuccess("list_structures");
        await CallSuccess("get_structure", Args(("name", structureName)));
        await CallSuccess("add_structure_element", Args(
            ("structureName", structureName),
            ("element", new Dictionary<string, object?>
            {
                ["offset"] = 24L, ["name"] = "third", ["variableType"] = 3,
                ["byteSize"] = 8, ["childClassName"] = ""
            })));
        await CallSuccess("compare_structures", Args(
            ("structureName", structureName),
            ("firstAddress", Hex(blockA)), ("secondAddress", Hex(blockB)),
            ("maxDifferences", 100)));
        await CallSuccess("autoguess_structure", Args(
            ("structureName", structureName), ("baseAddress", Hex(blockA)),
            ("offset", 32), ("size", 64)));
        await CallSuccess("remove_structure_element", Args(
            ("structureName", structureName), ("index", 0)));
        await CallSuccess("delete_structure", Args(("name", structureName)));
        structureName = null;

        string aaScript = $"""
            [ENABLE]
            alloc(ce_mcp_notepad_aa,64)
            registersymbol(ce_mcp_notepad_aa)
            ce_mcp_notepad_aa:
            db 90 90 C3
            [DISABLE]
            unregistersymbol(ce_mcp_notepad_aa)
            dealloc(ce_mcp_notepad_aa)
            """;
        JsonNode checkedScript = await CallSuccess("auto_assemble_check", Args(
            ("script", aaScript), ("enable", true), ("targetSelf", false)));
        Assert.IsTrue(Required<bool>(checkedScript, "syntaxValid"));
        JsonNode enabled = await CallSuccess("auto_assemble", Args(
            ("script", aaScript), ("targetSelf", false)));
        string disableId = Required<string>(enabled, "disableId");
        await CallSuccess("auto_assemble", Args(
            ("script", aaScript), ("targetSelf", false), ("disableId", disableId)));

        testTablePath = OwnTempFile("table", ".CT");
        await CallSuccess("save_cheat_table", Args(("filename", testTablePath)));
        await CallSuccess("load_cheat_table", Args(("filename", testTablePath), ("merge", false)));
    }

    private async Task RunInjectionGenerationScenarios()
    {
        ulong block = allocations.First();
        await CallSuccess("generate_code_injection_script", Args(
            ("address", Hex(block)), ("farJump", false)));
        await CallSuccess("generate_api_hook_script", Args(
            ("address", Hex(block)), ("jumpTarget", Hex(block + 32)),
            ("newCallAddress", null), ("extension", null), ("targetSelf", false)));

        JsonNode compiled = await CallSuccess("compile_c", Args(
            ("source", "long long ce_mcp_notepad_identity(long long value) { return value + 1; }"),
            ("address", null), ("targetSelf", false), ("kernelMode", false), ("noDebug", true)));
        JsonNode? symbols = compiled["symbols"];
        ulong function = FindAddressValue(symbols, "ce_mcp_notepad_identity");
        JsonNode result = await CallSuccess("execute_remote_function", Args(
            ("address", Hex(function)), ("parameter", 41L), ("timeout", 5000)));
        Assert.AreEqual(42L, Required<long>(result, "result"));
        JsonNode extended = await CallSuccess("execute_remote_function_ex", Args(
            ("address", Hex(function)), ("parameters", new long[] { 99 }),
            ("callMethod", 0), ("timeout", 5000)));
        Assert.AreEqual(100L, Required<long>(extended, "result"));
    }

    private async Task RunDebuggerScenarios()
    {
        ulong block = allocations.First();
        JsonNode process = await CallSuccess("get_current_process");
        int threadId = RequiredArray(process, "threads")[0]!.GetValue<int>();

        for (int cycle = 1; cycle <= 2; cycle++)
        {
            JsonNode attached = await CallSuccess(
                "dbg_start",
                Args(("debugInterface", 1)));
            Assert.AreEqual(notepadProcessId, Required<int>(attached, "processId"));
            Assert.AreEqual(1, Required<int>(attached, "debuggerInterface"));

            JsonNode idempotent = await CallSuccess(
                "dbg_start",
                Args(("debugInterface", 1)));
            Assert.AreEqual(notepadProcessId, Required<int>(idempotent, "processId"));

            JsonNode detached = await CallSuccess("dbg_exit");
            Assert.AreEqual(notepadProcessId, Required<int>(detached, "processId"));
            Assert.IsFalse(Required<bool>(detached, "isDebugging"));

            JsonNode current = await CallSuccess("get_current_process");
            Assert.AreEqual(notepadProcessId, Required<int>(current, "processId"));
        }

        await CallSuccess("dbg_start", Args(("debugInterface", 1)));
        await CallSuccess("dbg_is_debugging");
        await CallSuccess("dbg_is_broken");
        await CallSuccess("dbg_bps");

        await CallSuccess("dbg_add_bp", Args(
            ("address", Hex(block)), ("size", 1), ("trigger", "execute"), ("trackHits", false)));
        await CallSuccess("dbg_get_bp_hits", Args(("address", Hex(block))));
        await CallSuccess("dbg_clear_bp_hits", Args(("address", Hex(block))));
        await CallSuccess("dbg_delete_bp", Args(("address", Hex(block))));
        await CallSuccess("dbg_toggle_bp", Args(("address", Hex(block + 8))));
        await CallSuccess("dbg_toggle_bp", Args(("address", Hex(block + 8))));

        await CallSuccess("dbg_exclude_thread", Args(("threadId", threadId)));
        await CallSuccess("dbg_include_thread", Args(("threadId", threadId)));
        await CallSuccess("dbg_add_thread_bp", Args(
            ("threadId", threadId), ("address", Hex(block + 16)),
            ("size", 1), ("trigger", "execute")));
        await CallSuccess("dbg_delete_bp", Args(("address", Hex(block + 16))));

        ulong breakAddress = await ResolveFirstAddress(
            "user32.DispatchMessageW",
            "user32.dll.DispatchMessageW",
            "user32.GetMessageW",
            "user32.dll.GetMessageW");
        await CallSuccess("dbg_add_bp", Args(
            ("address", Hex(breakAddress)), ("size", 1), ("trigger", "execute"), ("trackHits", false)));
        WakeNotepadMessageLoop();
        await WaitForDebuggerBreak();

        JsonNode registers = await CallSuccess("dbg_gpregs");
        await CallSuccess("dbg_gpregs_remote");
        await CallSuccess("dbg_regs");
        await CallSuccess("dbg_regs_all");
        await CallSuccess("dbg_regs_named", Args(("registerNames", "RAX,RBX,RIP")));
        await CallSuccess("dbg_regs_named_remote", Args(("registerNames", "RAX,RBX,RIP")));
        await CallSuccess("dbg_regs_remote");
        await CallSuccess("dbg_context_table", Args(("extraRegisters", true)));
        await CallSuccess("dbg_read_xmm", Args(("register", 0)));
        await CallSuccess("dbg_stacktrace", Args(("depth", 16)));
        await CallSuccess("dbg_read", Args(("address", Hex(block)), ("dataType", "bytes"), ("length", 16)));
        await CallSuccess("dbg_write", Args(
            ("address", Hex(block + 320)), ("value", "2468"), ("dataType", "int32")));
        await CallSuccess("dbg_lbr_enable", Args(("enabled", false)));
        await CallSuccess("dbg_lbr_records", Args(("maxRecords", 16)));

        await CallSuccess("dbg_step_into");
        await WaitForDebuggerBreak();
        await CallSuccess("dbg_step_over");
        await WaitForDebuggerBreak();

        string instructionPointer = Required<string>(registers, "ip");
        await CallSuccess("dbg_delete_bp", Args(("address", Hex(breakAddress))));
        await CallSuccess("dbg_run_to", Args(("address", instructionPointer)));
        WakeNotepadMessageLoop();
        await WaitForDebuggerBreak();
        await CallSuccess("dbg_delete_bp", Args(("address", instructionPointer)), requireSuccess: false);
        await CallSuccess("dbg_continue", Args(("method", "run")));

        await CallSuccess("dbg_break_thread", Args(("threadId", threadId)));
        WakeNotepadMessageLoop();
        await Task.Delay(250);
        if (await IsDebuggerBroken())
            await CallSuccess("dbg_continue", Args(("method", "run")));
        await CallSuccess("dbg_exit");
    }

    private async Task CleanupOwnedState()
    {
        if (client is null)
            return;

        await TryCall("dbg_exit");
        await TryCall("reset_memory_scan", Args(("scannerName", valueScanner)));
        await TryCall("reset_memory_scan", Args(("scannerName", stringScanner)));

        if (symbolName is not null)
            await TryCall("unregister_symbol", Args(("name", symbolName)));
        if (structureName is not null)
            await TryCall("delete_structure", Args(("name", structureName)));
        if (baselineTablePath is not null && File.Exists(baselineTablePath))
            await TryCall("load_cheat_table", Args(("filename", baselineTablePath), ("merge", false)));

        foreach (ulong address in allocations.Reverse())
            await TryCall("free_memory", Args(("address", Hex(address))));
        allocations.Clear();

        if (notepadProcessId > 0)
        {
            try
            {
                using global::System.Diagnostics.Process process = global::System.Diagnostics.Process.GetProcessById(notepadProcessId);
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
        }

        foreach (string file in ownedFiles)
        {
            try { File.Delete(file); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        await client.DisposeAsync();
        client = null;
    }

    private async Task<ulong> Allocate(ulong size)
    {
        JsonNode payload = await CallSuccess("allocate_memory", Args(("size", size)));
        ulong address = RequiredAddress(payload);
        allocations.Add(address);
        return address;
    }


    private async Task RoundTrip(
        string dataType,
        ulong address,
        string value,
        int? byteCount = null,
        int? maxLength = null)
    {
        await CallSuccess("write_memory", Args(
            ("address", Hex(address)), ("dataType", dataType), ("value", value)));
        var arguments = Args(("address", Hex(address)), ("dataType", dataType));
        if (byteCount.HasValue)
            arguments["byteCount"] = byteCount.Value;
        if (maxLength.HasValue)
            arguments["maxLength"] = maxLength.Value;
        await CallSuccess("read_memory", arguments);
    }

    private static async Task<int> WaitForNotepadWindowProcessId()
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        do
        {
            foreach (global::System.Diagnostics.Process process in
                global::System.Diagnostics.Process.GetProcessesByName("Notepad"))
            {
                using (process)
                {
                    process.Refresh();
                    if (!process.HasExited && process.MainWindowHandle != 0)
                        return process.Id;
                }
            }
            await Task.Delay(200);
        } while (DateTime.UtcNow < deadline);

        Assert.Fail("The disposable Notepad process did not expose a main window within 15 seconds.");
        return 0;
    }

    private async Task<JsonNode> WaitForCurrentProcess()
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        do
        {
            JsonNode payload = await CallSuccess("get_current_process");
            if (payload["isOpen"]?.GetValue<bool>() == true &&
                (payload["processName"]?.GetValue<string>() ?? "").Contains("notepad", StringComparison.OrdinalIgnoreCase))
                return payload;
            await Task.Delay(200);
        } while (DateTime.UtcNow < deadline);

        Assert.Fail("Cheat Engine did not open the disposable Notepad process within 15 seconds.");
        return null!;
    }

    private async Task<ulong> ResolveFirstAddress(params string[] expressions)
    {
        foreach (string expression in expressions)
        {
            JsonNode payload = await CallSuccess(
                "resolve_address",
                Args(("addressString", expression), ("local", false)),
                requireSuccess: false);
            if (payload["success"]?.GetValue<bool>() == true)
                return ParseAddress(Required<string>(payload, "address"));
        }

        Assert.Fail($"None of the debugger breakpoint symbols resolved: {string.Join(", ", expressions)}");
        return 0;
    }

    private void WakeNotepadMessageLoop()
    {
        using global::System.Diagnostics.Process process =
            global::System.Diagnostics.Process.GetProcessById(notepadProcessId);
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        nint window;
        do
        {
            process.Refresh();
            window = process.MainWindowHandle;
            if (window != 0)
                break;
            Thread.Sleep(50);
        } while (DateTime.UtcNow < deadline);

        Assert.AreNotEqual(0, window, "The disposable Notepad process did not expose a main window.");
        Assert.IsTrue(PostMessage(window, WmNull, 0, 0), "PostMessageW could not wake Notepad's message loop.");
    }

    private async Task WaitForDebuggerBreak()
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        do
        {
            if (await IsDebuggerBroken())
                return;
            await Task.Delay(100);
        } while (DateTime.UtcNow < deadline);

        Assert.Fail("The Notepad debugger did not reach a broken state within 15 seconds.");
    }

    private async Task<bool> IsDebuggerBroken()
    {
        JsonNode payload = await CallSuccess("dbg_is_broken");
        return Required<bool>(payload, "is_broken");
    }

    private async Task<JsonNode> CallSuccess(
        string name,
        IReadOnlyDictionary<string, object?>? arguments = null,
        bool requireSuccess = true)
    {
        Assert.IsNotNull(client);
        attemptedTools.Add(name);
        JsonNode? payload = await client.CallToolAsync(name, arguments);
        Assert.IsNotNull(payload, $"Tool '{name}' returned no payload.");
        if (requireSuccess)
        {
            bool success = payload["success"]?.GetValue<bool>() == true;
            Assert.IsTrue(success, $"Tool '{name}' failed: {payload}");
        }
        return payload;
    }

    private async Task TryCall(string name, IReadOnlyDictionary<string, object?>? arguments = null)
    {
        try { await CallSuccess(name, arguments, requireSuccess: false); }
        catch (Exception) { }
    }

    private void AssertCompleteCoverage(IList<McpClientTool> tools)
    {
        string[] liveNames = tools.Select(tool => tool.Name).Order(StringComparer.Ordinal).ToArray();
        string[] uncovered = liveNames
            .Where(name => !attemptedTools.Contains(name) && !ExcludedTools.ContainsKey(name))
            .ToArray();
        Assert.AreEqual(
            0,
            uncovered.Length,
            $"Live tools without a Notepad scenario or explicit exclusion: {string.Join(", ", uncovered)}");

        string[] staleExclusions = ExcludedTools.Keys.Except(liveNames, StringComparer.Ordinal).ToArray();
        Assert.AreEqual(
            0,
            staleExclusions.Length,
            $"Notepad exclusions reference tools not exposed by the live server: {string.Join(", ", staleExclusions)}");

        Assert.IsTrue(
            ExcludedTools.All(item => !string.IsNullOrWhiteSpace(item.Value)),
            "Every excluded tool must have a documented reason.");
    }

    private string OwnTempFile(string label, string extension)
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"ce-mcp-notepad-{label}-{Guid.NewGuid():N}{extension}");
        ownedFiles.Add(path);
        return path;
    }

    private static Dictionary<string, object?> Args(params (string Name, object? Value)[] values) =>
        values.ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);

    private static T Required<T>(JsonNode node, string property)
    {
        JsonNode? value = node[property];
        if (value is null)
            throw new AssertFailedException($"Missing '{property}' in payload: {node}");
        return value.GetValue<T>();
    }

    private static JsonArray RequiredArray(JsonNode node, string property) =>
        node[property] as JsonArray
        ?? throw new AssertFailedException($"Missing array '{property}' in payload: {node}");

    private static ulong RequiredAddress(JsonNode node) => ParseAddress(Required<string>(node, "address"));

    private static ulong ParseAddress(string text) =>
        ulong.Parse(text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? text[2..] : text,
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture);

    private static string Hex(ulong address) => $"0x{address:X}";

    private static void AssertArrayContains(JsonArray? array, string expected)
    {
        Assert.IsNotNull(array);
        Assert.IsTrue(
            array.Any(node => string.Equals(node?.GetValue<string>(), expected, StringComparison.OrdinalIgnoreCase)),
            $"Array did not contain '{expected}': {array}");
    }

    private static byte[] ExtractBytes(JsonNode? value)
    {
        if (value is JsonArray array)
            return array.Select(node => node!.GetValue<byte>()).ToArray();
        if (value is JsonValue scalar)
            return Convert.FromBase64String(scalar.GetValue<string>());
        throw new AssertFailedException($"Unsupported byte payload: {value}");
    }

    private static ulong FindAddressValue(JsonNode? node, string preferredKey)
    {
        if (node is JsonObject obj)
        {
            if (obj[preferredKey] is JsonNode preferred)
                return NumericAddress(preferred);
            foreach ((_, JsonNode? value) in obj)
            {
                if (value is not null)
                {
                    try { return NumericAddress(value); }
                    catch (Exception) when (value is JsonObject or JsonArray) { }
                }
            }
        }
        throw new AssertFailedException($"Compiled symbol '{preferredKey}' was not returned: {node}");
    }

    private static ulong NumericAddress(JsonNode node)
    {
        if (node is JsonValue value)
        {
            if (value.TryGetValue(out long signed))
                return (ulong)signed;
            if (value.TryGetValue(out ulong unsigned))
                return unsigned;
            if (value.TryGetValue(out string? text) && text is not null)
                return ParseAddress(text);
        }
        throw new AssertFailedException($"Value is not an address: {node}");
    }
}

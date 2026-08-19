using Tools;

namespace CeMCP.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ToolBehaviorTests
{
    [TestInitialize]
    public void UseInlineMainThreadRunner()
    {
        mainThreadRunner = ToolThread.UseMainThreadRunnerForTests(body => body());
    }

    [TestCleanup]
    public void RestoreMainThreadRunner()
    {
        mainThreadRunner?.Dispose();
    }

    private IDisposable? mainThreadRunner;

    [TestMethod]
    public void ProcessTool_GetPluginVersion_ReturnsLoadedAssemblyMetadata()
    {
        object result = ProcessTool.GetPluginVersion();

        ToolResultAssert.IsSuccess(result);
        StringAssert.Contains(ToolResultAssert.GetProperty<string>(result, "location"), "ce-mcp.dll");
    }

    [TestMethod]
    public void ConversionTool_ValidatesRequiredInput()
    {
        object result = ConversionTool.ConvertString("", "md5");

        ToolResultAssert.IsFailure(result, "Input is required");
    }

    [TestMethod]
    public void ConversionTool_ValidatesRequiredConversionType()
    {
        object result = ConversionTool.ConvertString("abc", "");

        ToolResultAssert.IsFailure(result, "Conversion type is required");
    }

    [TestMethod]
    public void ConversionTool_NormalizesUnsupportedConversionType()
    {
        object result = ConversionTool.ConvertString("abc", "sha9000");

        ToolResultAssert.IsFailure(result, "Unsupported conversion type: sha9000");
    }

    [TestMethod]
    public void AssemblyTool_Disassemble_ValidatesAddress()
    {
        object result = AssemblyTool.Disassemble("");

        ToolResultAssert.IsFailure(result, "Address parameter is required");
    }

    [TestMethod]
    public void AssemblyTool_Disassemble_RejectsInvalidAddress()
    {
        object result = AssemblyTool.Disassemble("not-hex");

        ToolResultAssert.IsFailure(result, "Invalid address format");
    }

    [TestMethod]
    public void AssemblyTool_Disassemble_NormalizesUnsupportedRequestType()
    {
        object result = AssemblyTool.Disassemble("0x401000", "decode-magic");

        ToolResultAssert.IsFailure(result, "Unsupported request type: decode-magic");
    }

    [TestMethod]
    public void AutoAssemblyTool_Assemble_ValidatesInstruction()
    {
        object result = AutoAssemblyTool.Assemble("");

        ToolResultAssert.IsFailure(result, "Instruction is required");
    }

    [TestMethod]
    public void AutoAssemblyTool_Assemble_RejectsInvalidAddressBeforeCeCall()
    {
        object result = AutoAssemblyTool.Assemble("nop", "not-hex");

        ToolResultAssert.IsFailure(result, "Invalid address format");
    }

    [TestMethod]
    public void AutoAssemblyTool_AutoAssemble_ValidatesScript()
    {
        object result = AutoAssemblyTool.AutoAssemble("");

        ToolResultAssert.IsFailure(result, "Script is required");
    }

    [TestMethod]
    public void AutoAssemblyTool_AutoAssembleCheck_ValidatesScript()
    {
        object result = AutoAssemblyTool.AutoAssembleCheck("");

        ToolResultAssert.IsFailure(result, "Script is required");
    }

    [TestMethod]
    public void MemoryViewTool_DisassembleRange_ValidatesAddress()
    {
        object result = MemoryViewTool.DisassembleRange("");

        ToolResultAssert.IsFailure(result, "Address is required");
    }

    [TestMethod]
    public void MemoryViewTool_GetFunctionRange_ValidatesAddress()
    {
        object result = MemoryViewTool.GetFunctionRange("");

        ToolResultAssert.IsFailure(result, "Address is required");
    }

    [TestMethod]
    public void MemoryViewTool_DisassembleBytes_ValidatesBytes()
    {
        object result = MemoryViewTool.DisassembleBytes("");

        ToolResultAssert.IsFailure(result, "Hex bytes are required");
    }

    [TestMethod]
    public void MemoryViewTool_GetPreviousOpcodes_ValidatesAddress()
    {
        object result = MemoryViewTool.GetPreviousOpcodes("");

        ToolResultAssert.IsFailure(result, "Address is required");
    }

    [TestMethod]
    public void MemoryViewTool_GetMemoryProtection_ValidatesAddress()
    {
        object result = MemoryViewTool.GetMemoryProtection("");

        ToolResultAssert.IsFailure(result, "Address is required");
    }

    [TestMethod]
    public void MemoryViewTool_SetComment_ValidatesAddress()
    {
        object result = MemoryViewTool.SetComment("", "note");

        ToolResultAssert.IsFailure(result, "Address is required");
    }

    [TestMethod]
    public void AdvancedMemoryTool_AllocateMemory_RejectsZeroSize()
    {
        object result = AdvancedMemoryTool.AllocateMemory(0);

        ToolResultAssert.IsFailure(result, "Size must be greater than zero");
    }

    [TestMethod]
    public void PointerTool_ReadPointerChain_RejectsInvalidAddress()
    {
        object result = PointerTool.ReadPointerChain("not-hex", [0]);

        ToolResultAssert.IsFailure(result, "Invalid base address format");
    }

    [TestMethod]
    public void ProcessControlTool_CreateProcess_ValidatesPath()
    {
        object result = ProcessControlTool.CreateProcess("");

        ToolResultAssert.IsFailure(result, "Executable path is required");
    }

    [TestMethod]
    public void CheatTableTool_LoadCheatTable_ValidatesFilename()
    {
        object result = CheatTableTool.LoadCheatTable("");

        ToolResultAssert.IsFailure(result, "Filename is required");
    }

    [TestMethod]
    public void StructureTool_CreateStructure_ValidatesName()
    {
        object result = StructureTool.CreateStructure("");

        ToolResultAssert.IsFailure(result, "Name is required");
    }

    [TestMethod]
    public void InjectionTool_CompileC_ValidatesSource()
    {
        object result = InjectionTool.CompileC("");

        ToolResultAssert.IsFailure(result, "Source is required");
    }

    [TestMethod]
    public void AnalysisTool_SearchDisassembly_ValidatesQuery()
    {
        object result = AnalysisTool.SearchDisassembly("", "", "");

        ToolResultAssert.IsFailure(result, "Query is required");
    }

    [TestMethod]
    public void DbvmTool_ReadPhysical_RejectsInvalidAddressBeforeDbvmCall()
    {
        object result = DbvmTool.ReadPhysical("not-hex", 1);

        ToolResultAssert.IsFailure(result, "Invalid address format");
    }

    [TestMethod]
    public void SymbolRegistryTool_GetRttiClassName_RejectsInvalidAddress()
    {
        object result = SymbolRegistryTool.GetRttiClassName("not-hex");

        ToolResultAssert.IsFailure(result, "Invalid address format");
    }

    [TestMethod]
    public void ScanTool_AobScanUnique_ValidatesPattern()
    {
        object result = ScanTool.AobScanUnique("");

        ToolResultAssert.IsFailure(result, "AOB pattern is required");
    }

    [TestMethod]
    public void ScanTool_StringScan_RejectsOversizedScannerName()
    {
        object result = ScanTool.StringScan("value", scannerName: new string('s', 65));

        ToolResultAssert.IsFailure(result, "scannerName is limited to 64 characters");
    }

    [TestMethod]
    public void ScanTool_StringScan_RejectsReversedRange()
    {
        object result = ScanTool.StringScan("value", startAddress: 2, stopAddress: 1);

        ToolResultAssert.IsFailure(result, "Start address must not exceed stop address");
    }

    [TestMethod]
    public void MemoryViewTool_EnumMemoryRegions_RejectsUnknownFilter()
    {
        object result = MemoryViewTool.EnumMemoryRegions("mystery");

        ToolResultAssert.IsFailure(result, "Filter must be committed, reserved, free, or all");
    }

    [TestMethod]
    public void StructureTool_CompareStructures_ValidatesName()
    {
        object result = StructureTool.CompareStructures("", "0", "1");

        ToolResultAssert.IsFailure(result, "Structure name is required");
    }

    [TestMethod]
    public void MemoryTool_ReadMemory_RejectsOversizedByteCount()
    {
        object result = MemoryTool.ReadMemory("0", "bytes", byteCount: 1_048_577);

        ToolResultAssert.IsFailure(result, "ByteCount must be between 1 and 1048576");
    }

    [TestMethod]
    public void MemoryTool_WriteMemory_RejectsUnsupportedDataType()
    {
        object result = MemoryTool.WriteMemory("0", "mystery", "1");

        ToolResultAssert.IsFailure(result, "Unsupported data type: mystery");
    }

    [TestMethod]
    public void DebuggerTool_DbgBreakThread_RejectsInvalidThreadId()
    {
        object result = DebuggerTool.DbgBreakThread(0);

        ToolResultAssert.IsFailure(result, "threadId must be greater than zero");
    }
}

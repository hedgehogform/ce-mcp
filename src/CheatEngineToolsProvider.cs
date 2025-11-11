using System;
using System.ComponentModel;
using ModelContextProtocol.Server;
using Tools;


namespace CEMCP
{
  [McpServerToolType]
  public class CheatEngineToolsProvider
  {

    [McpServerTool, Description("Execute Lua code in Cheat Engine")]
    public static object ExecuteLua([Description("The Lua script to execute")] string code)
    {
      return LuaExecutionTool.ExecuteLua(code);
    }

    [McpServerTool, Description("Get list of running processes")]
    public static object GetProcessList()
    {
      return ProcessTool.GetProcessList();
    }

    [McpServerTool, Description("Open a process by ID or name")]
    public static object OpenProcess([Description("Process ID or name to open")] string process)
    {
      try
      {
        ProcessTool.OpenProcess(process);
        return "Process opened successfully.";
      }
      catch (Exception ex)
      {
        return $"Error: {ex.Message}";
      }
    }

    [McpServerTool, Description("Get list of threads in the current process")]
    public static object GetThreadList()
    {
      return ThreadListTool.GetThreadList();
    }

    [McpServerTool, Description("Get status of the currently opened process")]
    public static object GetProcessStatus()
    {
      return ProcessTool.GetProcessStatus();
    }

    [McpServerTool, Description("Read memory from a specific address")]
    public static object ReadMemory(
      [Description("Memory address to read from")] string address,
      [Description("Data type (byte, word, dword, qword, float, double, string, aobstring)")] string dataType,
      [Description("Maximum length for string types")] int? maxLength = null,
      [Description("Number of bytes to read")] int? byteCount = null,
      [Description("Whether value is signed")] bool signed = false)
    {
      return MemoryReadTool.ReadMemory(address, dataType, maxLength, byteCount, signed);
    }

    [McpServerTool, Description("Write a value to a specific memory address")]
    public static object WriteMemory(
      [Description("Memory address to write to")] string address,
      [Description("Value to write")] string value,
      [Description("Data type (byte, word, dword, qword, float, double, string, aobstring)")] string dataType,
      [Description("Maximum length for string types")] int? maxLength = null,
      [Description("Whether value is signed")] bool signed = false)
    {
      return MemoryWriteTool.WriteMemory(address, value, dataType, maxLength, signed);
    }

    [McpServerTool, Description("Convert between different number formats")]
    public static object Convert(
      [Description("Value to convert")] string input,
      [Description("Conversion type (hex2dec, dec2hex, hex2bin, bin2hex, dec2bin, bin2dec)")] string conversionType)
    {
      return ConversionTool.Convert(input, conversionType);
    }

    [McpServerTool, Description("Search for an array of bytes pattern in memory")]
    public static object AobScan(
      [Description("AOB pattern (e.g., '48 8B ?? ?? 90')")] string aobString,
      [Description("Protection flags (+W for writable, +X for executable)")] string? protectionFlags = null,
      [Description("Alignment type (0=byte, 1=2bytes, 2=4bytes, 3=8bytes)")] int? alignmentType = null,
      [Description("Alignment parameter")] string? alignmentParam = null)
    {
      return AobScanTool.AobScan(aobString, protectionFlags, alignmentType, alignmentParam);
    }

    [McpServerTool, Description("Disassemble instructions at a memory address")]
    public static object Disassemble(
      [Description("Memory address to disassemble")] string address)
    {
      return DisassembleTool.Disassemble(address);
    }

    [McpServerTool, Description("Scan memory for specific values")]
    public static object MemScan(
    [Description("Scan option (0=exact value, 1=between)")] int scanOption,
    [Description("Variable type (0=dword, 1=qword, 2=float, 3=double)")] int varType,
    [Description("Input value 1")] string input1,
    [Description("Input value 2 (for between scans)")] string input2 = "",
    [Description("Start address (0 for default)")] ulong startAddress = 0,
    [Description("Stop address (max for default)")] ulong stopAddress = ulong.MaxValue,
    [Description("Protection flags (e.g., +W-C)")] string protectionFlags = "+W-C",
    [Description("Alignment type (0=aligned, 1=unaligned)")] int alignmentType = 0,
    [Description("Is hexadecimal input")] bool isHexadecimalInput = false)
    {
      // Cast integers to enums
      var scanOptionEnum = (CESDK.Classes.ScanOption)scanOption;
      var varTypeEnum = (CESDK.Classes.VariableType)varType;
      var alignmentEnum = (CESDK.Classes.AlignmentType)alignmentType;

      return MemScanTool.Scan(
          scanOptionEnum,
          varTypeEnum,
          input1,
          input2,
          startAddress,
          stopAddress,
          protectionFlags,
          alignmentEnum,
          alignmentParam: "4", // default alignment param
          isHexadecimalInput: isHexadecimalInput
      );
    }

    [McpServerTool, Description("Reset the memory scanner state")]
    public static object MemScanReset()
    {
      MemScanTool.ResetScan();
      return "Memory scanner reset.";
    }

    [McpServerTool, Description("Safely resolve a symbolic address")]
    public static object GetAddressSafe(
      [Description("Address to resolve (can be symbolic like 'module.dll+offset')")] string addressString,
      [Description("Whether to treat as local address")] bool local = false)
    {
      return AddressTool.GetAddressSafe(addressString, local);
    }
  }
}
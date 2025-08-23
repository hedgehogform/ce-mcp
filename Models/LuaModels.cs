using System;
using CESDK;

namespace CeMCP.Models
{
    public class BaseResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    public class LuaRequest
    {
        public string Code { get; set; }
    }

    public class LuaResponse : BaseResponse
    {
        public string Result { get; set; }
    }

    public class ProcessInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
    }

    public class ProcessListResponse : BaseResponse
    {
        public ProcessInfo[] ProcessList { get; set; }
    }

    public class OpenProcessRequest
    {
        public string Process { get; set; }
    }

    public class ThreadListResponse : BaseResponse
    {
        public string[] ThreadList { get; set; }
    }

    public class ProcessStatusResponse : BaseResponse
    {
        public int ProcessId { get; set; }
        public bool IsOpen { get; set; }
        public string ProcessName { get; set; }
    }

    public class MemoryReadRequest
    {
        public string Address { get; set; }
        public string DataType { get; set; }
        public bool? Signed { get; set; }
        public int? ByteCount { get; set; }
        public bool? ReturnAsTable { get; set; }
        public int? MaxLength { get; set; }
        public bool? WideChar { get; set; }
    }

    public class MemoryReadResponse : BaseResponse
    {
        public object Value { get; set; }
    }

    public class ByteCount
    {
        // Represents the number of bytes to write
        public int Value { get; set; }
    }

    public class MemoryWriteRequest
    {
        // Memory address to write to
        public string Address { get; set; }

        // Value to write (can be int, long, float, string, etc.)
        public object Value { get; set; }

        // Data type to write ("bytes", "integer", "float", "string", etc.)
        public string DataType { get; set; }

        // Only used when writing bytes
        public ByteCount ByteCount { get; set; }

        // Only used when writing strings
        public int? MaxLength { get; set; }
        public bool? WideChar { get; set; }

        // Optional for integer writes
        public bool? Signed { get; set; }
    }


    public class MemoryWriteResponse : BaseResponse
    {
        public object Value { get; set; }
    }

    public class ConversionRequest
    {
        public string Input { get; set; }
        public string ConversionType { get; set; }
    }

    public class ConversionResponse : BaseResponse
    {
        public string Output { get; set; }
    }

    public class AobScanRequest
    {
        public string AOBString { get; set; }
        public string ProtectionFlags { get; set; }
        public int? AlignmentType { get; set; }
        public string AlignmentParam { get; set; }
    }

    public class AobScanResponse : BaseResponse
    {
        public string[] Addresses { get; set; }
    }

    public class DisassemblerRequest
    {
        public string RequestType { get; set; } // disassemble, get-instruction-size
        public string Address { get; set; }
    }

    public class DisassemblerResponse : BaseResponse
    {
        public string Output { get; set; }
    }

    public class GetInstructionSizeResponse : BaseResponse
    {
        public int Size { get; set; }
    }

    public class MemScanScanRequest
    {
        public ScanOptions ScanOption { get; set; } = ScanOptions.soExactValue;
        public VarTypes VarType { get; set; } = VarTypes.vtDword;
        public RoundingTypes RoundingType { get; set; } = RoundingTypes.rtExtremerounded;
        public string Input1 { get; set; } = string.Empty; // Primary search value
        public string Input2 { get; set; } = string.Empty; // Secondary search value (for between scans)
        public ulong StartAddress { get; set; } = 0; // Default start
        public ulong StopAddress { get; set; } = ulong.MaxValue; // Default stop
        public string ProtectionFlags { get; set; } = "+W-C"; // e.g., "+W+X"
        public FastScanMethods AlignmentType { get; set; } = FastScanMethods.fsmAligned;
        public string AlignmentParam { get; set; } = "4"; // Alignment parameter
        public bool IsHexadecimalInput { get; set; } = false;
        public bool IsNotABinaryString { get; set; } = false;
        public bool IsUnicodeScan { get; set; } = false;
        public bool IsCaseSensitive { get; set; } = false;
        public bool IsPercentageScan { get; set; } = false; // next scan
    }

    public class MemScanResponse : BaseResponse
    {
        public FoundList FoundList { get; set; } = new FoundList(IntPtr.Zero);

    }
}
using System;
using System.Collections.Generic;
using CESDK;

namespace CeMCP.Models
{
    // Base response class
    public class BaseResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    // Lua request/response
    public class LuaRequest
    {
        public string Code { get; set; }
    }

    public class LuaResponse : BaseResponse
    {
        public string Result { get; set; }
    }

    // Process info classes
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

    // Memory read/write
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
        public int Value { get; set; }
    }

    public class MemoryWriteRequest
    {
        public string Address { get; set; }
        public object Value { get; set; }
        public string DataType { get; set; }
        public ByteCount ByteCount { get; set; }
        public int? MaxLength { get; set; }
        public bool? WideChar { get; set; }
        public bool? Signed { get; set; }
    }

    public class MemoryWriteResponse : BaseResponse
    {
        public object Value { get; set; }
    }

    // Conversion
    public class ConversionRequest
    {
        public string Input { get; set; }
        public string ConversionType { get; set; }
    }

    public class ConversionResponse : BaseResponse
    {
        public string Output { get; set; }
    }

    // AOB scan
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

    // Disassembler
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

    // Missing enums (define here for SDK usage without DLL)
    public enum ScanOptions
    {
        soExactValue,
        soBetween
    }

    public enum VarTypes
    {
        vtDword,
        vtQword,
        vtFloat,
        vtDouble
    }

    public enum RoundingTypes
    {
        rtExtremerounded,
        rtTruncated,
        rtNearest
    }

    public enum FastScanMethods
    {
        fsmAligned,
        fsmUnaligned
    }

    // MemScan
    public class MemScanScanRequest
    {
        public ScanOptions ScanOption { get; set; } = ScanOptions.soExactValue;
        public VarTypes VarType { get; set; } = VarTypes.vtDword;
        public RoundingTypes RoundingType { get; set; } = RoundingTypes.rtExtremerounded;
        public string Input1 { get; set; } = string.Empty;
        public string Input2 { get; set; } = string.Empty;
        public ulong StartAddress { get; set; } = 0;
        public ulong StopAddress { get; set; } = ulong.MaxValue;
        public string ProtectionFlags { get; set; } = "+W-C";
        public FastScanMethods AlignmentType { get; set; } = FastScanMethods.fsmAligned;
        public string AlignmentParam { get; set; } = "4";
        public bool IsHexadecimalInput { get; set; } = false;
        public bool IsNotABinaryString { get; set; } = false;
        public bool IsUnicodeScan { get; set; } = false;
        public bool IsCaseSensitive { get; set; } = false;
        public bool IsPercentageScan { get; set; } = false;
    }

    public class ResultItem
    {
        public string Address { get; set; }
        public string Value { get; set; }

        public ResultItem(string address, string value)
        {
            Address = address;
            Value = value;
        }
    }

    public class ResultList
    {
        private readonly List<ResultItem> _items = new List<ResultItem>();
        private const int MaxStored = 1000;

        public int TotalCount { get; set; }
        public int StoredCount => _items.Count;
        public List<ResultItem> Items => _items;

        public string this[int index] => _items[index].Address;
        public string GetAddress(int index) => _items[index].Address;
        public string GetValue(int index) => _items[index].Value;
        public ResultItem GetResult(int index) => _items[index];

        public void Add(string address, string value)
        {
            if (_items.Count < MaxStored)
            {
                _items.Add(new ResultItem(address, value));
            }
        }
    }

    public class MemScanResponse : BaseResponse
    {
        public ResultList Results { get; set; } = new ResultList();
    }

    // Safe address requests
    public class GetAddressSafeRequest
    {
        public string AddressString { get; set; }
        public bool? Local { get; set; }
    }

    public class GetAddressSafeResponse : BaseResponse
    {
        public string Address { get; set; }
    }

    // Name from address
    public class GetNameFromAddressRequest
    {
        public string Address { get; set; }
        public bool? ModuleNames { get; set; }
        public bool? Symbols { get; set; }
        public bool? Sections { get; set; }
    }

    public class GetNameFromAddressResponse : BaseResponse
    {
        public string Name { get; set; }
    }

    // In module checks
    public class InModuleRequest
    {
        public string Address { get; set; }
    }

    public class InModuleResponse : BaseResponse
    {
        public bool InModule { get; set; }
    }

    public class InSystemModuleRequest
    {
        public string Address { get; set; }
    }

    public class InSystemModuleResponse : BaseResponse
    {
        public bool InSystemModule { get; set; }
    }
}

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

    public class MemoryWriteRequest
    {
        public string Address { get; set; }
        public string DataType { get; set; }
        public object Value { get; set; }
        public int[] ByteValues { get; set; }
        public bool? WideChar { get; set; }
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

    public class AOBScanRequest
    {
        public string AOBString { get; set; }
        public string ProtectionFlags { get; set; }
        public int? AlignmentType { get; set; }
        public string AlignmentParam { get; set; }
    }

    public class AOBScanResponse : BaseResponse
    {
        public string[] Addresses { get; set; }
    }

    public class DisassembleRequest
    {
        public string Address { get; set; }
    }

    public class DisassembleResponse : BaseResponse
    {
        public string Disassembly { get; set; }
    }

    public class GetInstructionSizeRequest
    {
        public string Address { get; set; }
    }

    public class GetInstructionSizeResponse : BaseResponse
    {
        public int Size { get; set; }
    }
}
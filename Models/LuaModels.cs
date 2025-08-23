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
}
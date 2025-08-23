namespace CeMCP.Models
{
    public class LuaRequest
    {
        public string Code { get; set; }
    }

    public class LuaResponse
    {
        public string Result { get; set; }
        public bool Success { get; set; }
    }

    public class ProcessInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
    }

    public class ProcessListResponse
    {
        public ProcessInfo[] ProcessList { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    public class OpenProcessRequest
    {
        public string Process { get; set; }
    }

    public class OpenProcessResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}
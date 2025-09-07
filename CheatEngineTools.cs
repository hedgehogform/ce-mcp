using CeMCP.Models;
using CeMCP.Tools;

namespace CeMCP
{
    public class CheatEngineTools
    {
        private LuaExecutionTool? _luaExecutionTool;
        private ProcessTool? _processTool;
        private ThreadListTool? _threadListTool;
        private MemoryReadTool? _memoryReadTool;
        private MemoryWriteTool? _memoryWriteTool;
        private ConversionTool? _conversionTool;
        private AobScanTool? _aobScanTool;
        private DisassembleTool? _disassembleTool;
        private AddressTool? _addressTool;

        private LuaExecutionTool LuaExecutionTool => _luaExecutionTool ??= new();
        private ProcessTool ProcessTool => _processTool ??= new();
        private ThreadListTool ThreadListTool => _threadListTool ??= new();
        private MemoryReadTool MemoryReadTool => _memoryReadTool ??= new();
        private MemoryWriteTool MemoryWriteTool => _memoryWriteTool ??= new();
        private ConversionTool ConversionTool => _conversionTool ??= new();
        private AobScanTool AobScanTool => _aobScanTool ??= new();
        private DisassembleTool DisassembleTool => _disassembleTool ??= new();
        private AddressTool AddressTool => _addressTool ??= new();

        public LuaResponse ExecuteLua(LuaRequest request)
        {
            return LuaExecutionTool.ExecuteLua(request);
        }

        public ProcessListResponse GetProcessList()
        {
            return ProcessTool.GetProcessList();
        }

        public BaseResponse OpenProcess(OpenProcessRequest request)
        {
            return ProcessTool.OpenProcess(request);
        }

        public ThreadListResponse GetThreadList()
        {
            return ThreadListTool.GetThreadList();
        }

        public ProcessStatusResponse GetProcessStatus()
        {
            return ProcessTool.GetProcessStatus();
        }

        public MemoryReadResponse ReadMemory(MemoryReadRequest request)
        {
            return MemoryReadTool.ReadMemory(request);
        }

        public BaseResponse WriteMemory(MemoryWriteRequest request)
        {
            return MemoryWriteTool.WriteMemory(request);
        }

        public ConversionResponse Convert(ConversionRequest request)
        {
            return ConversionTool.Convert(request);
        }

        public AobScanResponse AOBScan(AobScanRequest request)
        {
            return AobScanTool.AOBScan(request);
        }

        public DisassemblerResponse Disassemble(DisassemblerRequest request)
        {
            return DisassembleTool.Disassemble(request);
        }

        public MemScanResponse Scan(MemScanScanRequest request)
        {
            return MemScanTool.Scan(request);
        }

        public MemScanResponse ResetScan()
        {
            return MemScanTool.ResetScan();
        }

        public GetAddressSafeResponse GetAddressSafe(GetAddressSafeRequest request)
        {
            return AddressTool.GetAddressSafe(request);
        }

        public GetNameFromAddressResponse GetNameFromAddress(GetNameFromAddressRequest request)
        {
            return AddressTool.GetNameFromAddress(request);
        }

        public InModuleResponse InModule(InModuleRequest request)
        {
            return AddressTool.InModule(request);
        }

        public InSystemModuleResponse InSystemModule(InSystemModuleRequest request)
        {
            return AddressTool.InSystemModule(request);
        }
    }
}
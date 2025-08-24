using CeMCP.Models;
using CeMCP.Tools;
using CESDK;

namespace CeMCP
{
    public class CheatEngineTools
    {
        private readonly LuaExecutionTool _luaExecutionTool = new();
        private readonly ProcessTool _processTool = new();
        private readonly ThreadListTool _threadListTool = new();
        private readonly MemoryReadTool _memoryReadTool = new();
        private readonly MemoryWriteTool _memoryWriteTool = new();
        private readonly ConversionTool _conversionTool = new();
        private readonly AobScanTool _aobScanTool = new();
        private readonly DisassembleTool _disassembleTool = new();
        private readonly MemScanTool _memScanTool = new();

        public LuaResponse ExecuteLua(LuaRequest request)
        {
            return _luaExecutionTool.ExecuteLua(request);
        }

        public ProcessListResponse GetProcessList()
        {
            return _processTool.GetProcessList();
        }

        public BaseResponse OpenProcess(OpenProcessRequest request)
        {
            return _processTool.OpenProcess(request);
        }

        public ThreadListResponse GetThreadList()
        {
            return _threadListTool.GetThreadList();
        }

        public ProcessStatusResponse GetProcessStatus()
        {
            return _processTool.GetProcessStatus();
        }

        public MemoryReadResponse ReadMemory(MemoryReadRequest request)
        {
            return _memoryReadTool.ReadMemory(request);
        }

        public BaseResponse WriteMemory(MemoryWriteRequest request)
        {
            return _memoryWriteTool.WriteMemory(request);
        }

        public ConversionResponse Convert(ConversionRequest request)
        {
            return _conversionTool.Convert(request);
        }

        public AobScanResponse AOBScan(AobScanRequest request)
        {
            return _aobScanTool.AOBScan(request);
        }

        public DisassemblerResponse Disassemble(DisassemblerRequest request)
        {
            return _disassembleTool.Disassemble(request);
        }

        public MemScanResponse Scan(MemScanScanRequest request)
        {
            _memScanTool.StartScan(request);
            _memScanTool.WaitForScan();


            return new MemScanResponse
            {
                FoundList = _memScanTool.GetFoundList()
            };
        }
    }
}
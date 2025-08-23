using CeMCP.Models;
using CeMCP.Tools;

namespace CeMCP
{
    public class CheatEngineTools
    {
        private readonly LuaExecutionTool _luaExecutionTool;
        private readonly ProcessListTool _processListTool;
        private readonly OpenProcessTool _openProcessTool;
        private readonly ThreadListTool _threadListTool;
        private readonly ProcessStatusTool _processStatusTool;
        private readonly MemoryReadTool _memoryReadTool;
        private readonly MemoryWriteTool _memoryWriteTool;
        private readonly ConversionTool _conversionTool;
        private readonly AOBScanTool _aobScanTool;
        private readonly DisassembleTool _disassembleTool;
        private readonly GetInstructionSizeTool _getInstructionSizeTool;

        public CheatEngineTools(McpPlugin plugin)
        {
            _luaExecutionTool = new LuaExecutionTool(plugin);
            _processListTool = new ProcessListTool(plugin);
            _openProcessTool = new OpenProcessTool(plugin);
            _threadListTool = new ThreadListTool(plugin);
            _processStatusTool = new ProcessStatusTool(plugin);
            _memoryReadTool = new MemoryReadTool(plugin);
            _memoryWriteTool = new MemoryWriteTool(plugin);
            _conversionTool = new ConversionTool(plugin);
            _aobScanTool = new AOBScanTool(plugin);
            _disassembleTool = new DisassembleTool(plugin);
            _getInstructionSizeTool = new GetInstructionSizeTool(plugin);
        }

        public LuaResponse ExecuteLua(LuaRequest request)
        {
            return _luaExecutionTool.ExecuteLua(request);
        }

        public ProcessListResponse GetProcessList()
        {
            return _processListTool.GetProcessList();
        }

        public BaseResponse OpenProcess(OpenProcessRequest request)
        {
            return _openProcessTool.OpenProcess(request);
        }

        public ThreadListResponse GetThreadList()
        {
            return _threadListTool.GetThreadList();
        }

        public ProcessStatusResponse GetProcessStatus()
        {
            return _processStatusTool.GetProcessStatus();
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

        public AOBScanResponse AOBScan(AOBScanRequest request)
        {
            return _aobScanTool.AOBScan(request);
        }

        public DisassembleResponse Disassemble(DisassembleRequest request)
        {
            return _disassembleTool.Disassemble(request);
        }

        public GetInstructionSizeResponse GetInstructionSize(GetInstructionSizeRequest request)
        {
            return _getInstructionSizeTool.GetInstructionSize(request);
        }
    }
}
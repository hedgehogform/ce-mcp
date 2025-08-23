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

        public CheatEngineTools(McpPlugin plugin)
        {
            _luaExecutionTool = new LuaExecutionTool(plugin);
            _processListTool = new ProcessListTool(plugin);
            _openProcessTool = new OpenProcessTool(plugin);
            _threadListTool = new ThreadListTool(plugin);
            _processStatusTool = new ProcessStatusTool(plugin);
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
    }
}
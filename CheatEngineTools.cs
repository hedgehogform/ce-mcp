using CeMCP.Models;
using CeMCP.Tools;

namespace CeMCP
{
    public class CheatEngineTools
    {
        private readonly LuaExecutionTool _luaExecutionTool;
        private readonly ProcessListTool _processListTool;
        private readonly OpenProcessTool _openProcessTool;

        public CheatEngineTools(McpPlugin plugin)
        {
            _luaExecutionTool = new LuaExecutionTool(plugin);
            _processListTool = new ProcessListTool(plugin);
            _openProcessTool = new OpenProcessTool(plugin);
        }

        public LuaResponse ExecuteLua(LuaRequest request)
        {
            return _luaExecutionTool.ExecuteLua(request);
        }

        public ProcessListResponse GetProcessList()
        {
            return _processListTool.GetProcessList();
        }

        public OpenProcessResponse OpenProcess(OpenProcessRequest request)
        {
            return _openProcessTool.OpenProcess(request);
        }
    }
}
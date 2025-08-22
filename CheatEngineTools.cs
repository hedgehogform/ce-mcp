using CeMCP.Models;
using CeMCP.Tools;

namespace CeMCP
{
    public class CheatEngineTools
    {
        private readonly LuaExecutionTool _luaExecutionTool;
        private readonly ProcessListTool _processListTool;

        public CheatEngineTools(McpPlugin plugin)
        {
            _luaExecutionTool = new LuaExecutionTool(plugin);
            _processListTool = new ProcessListTool(plugin);
        }

        public LuaResponse ExecuteLua(LuaRequest request)
        {
            return _luaExecutionTool.ExecuteLua(request);
        }

        public ProcessListResponse GetProcessList()
        {
            return _processListTool.GetProcessList();
        }
    }
}
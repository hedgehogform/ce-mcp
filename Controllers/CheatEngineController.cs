using System.Web.Http;
using CeMCP.Models;

namespace CeMCP.Controllers
{
    [RoutePrefix("api/cheatengine")]
    public class CheatEngineController : ApiController
    {
        private readonly CheatEngineTools _tools;

        public CheatEngineController(CheatEngineTools tools)
        {
            _tools = tools;
        }

        [HttpPost]
        [Route("execute-lua")]
        public LuaResponse ExecuteLua([FromBody] LuaRequest request)
        {
            return _tools.ExecuteLua(request);
        }

        [HttpGet]
        [Route("process-list")]
        public ProcessListResponse GetProcessList()
        {
            return _tools.GetProcessList();
        }

        [HttpPost]
        [Route("open-process")]
        public BaseResponse OpenProcess([FromBody] OpenProcessRequest request)
        {
            return _tools.OpenProcess(request);
        }

        [HttpGet]
        [Route("thread-list")]
        public ThreadListResponse GetThreadList()
        {
            return _tools.GetThreadList();
        }

        [HttpGet]
        [Route("process-status")]
        public ProcessStatusResponse GetProcessStatus()
        {
            return _tools.GetProcessStatus();
        }

        [HttpPost]
        [Route("read-memory")]
        public MemoryReadResponse ReadMemory([FromBody] MemoryReadRequest request)
        {
            return _tools.ReadMemory(request);
        }

        [HttpPost]
        [Route("write-memory")]
        public BaseResponse WriteMemory([FromBody] MemoryWriteRequest request)
        {
            return _tools.WriteMemory(request);
        }

        [HttpPost]
        [Route("convert")]
        public ConversionResponse Convert([FromBody] ConversionRequest request)
        {
            return _tools.Convert(request);
        }

        [HttpPost]
        [Route("aob-scan")]
        public AOBScanResponse AOBScan([FromBody] AOBScanRequest request)
        {
            return _tools.AOBScan(request);
        }

        [HttpPost]
        [Route("disassemble")]
        public DisassembleResponse Disassemble([FromBody] DisassembleRequest request)
        {
            return _tools.Disassemble(request);
        }

        [HttpPost]
        [Route("get-instruction-size")]
        public GetInstructionSizeResponse GetInstruction([FromBody] GetInstructionSizeRequest request)
        {
            return _tools.GetInstructionSize(request);
        }

        [HttpGet]
        [Route("health")]
        public IHttpActionResult GetHealth()
        {
            return Ok(new { 
                status = "healthy", 
                server = ServerConfig.ServerName,
                version = typeof(CheatEngineController).Assembly.GetName().Version?.ToString(),
                timestamp = System.DateTime.UtcNow
            });
        }
    }
}
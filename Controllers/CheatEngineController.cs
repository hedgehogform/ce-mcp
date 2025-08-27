using System.Web.Http;
using CeMCP.Models;

namespace CeMCP.Controllers
{
    [RoutePrefix("api/cheatengine")]
    public class CheatEngineController : ApiController
    {
        private readonly CheatEngineTools tools = new CheatEngineTools();


        [HttpPost]
        [Route("execute-lua")]
        public LuaResponse ExecuteLua([FromBody] LuaRequest request)
        {
            return tools.ExecuteLua(request);
        }

        [HttpGet]
        [Route("process-list")]
        public ProcessListResponse GetProcessList()
        {
            return tools.GetProcessList();
        }

        [HttpPost]
        [Route("open-process")]
        public BaseResponse OpenProcess([FromBody] OpenProcessRequest request)
        {
            return tools.OpenProcess(request);
        }

        [HttpGet]
        [Route("thread-list")]
        public ThreadListResponse GetThreadList()
        {
            return tools.GetThreadList();
        }

        [HttpGet]
        [Route("process-status")]
        public ProcessStatusResponse GetProcessStatus()
        {
            return tools.GetProcessStatus();
        }

        [HttpPost]
        [Route("read-memory")]
        public MemoryReadResponse ReadMemory([FromBody] MemoryReadRequest request)
        {
            return tools.ReadMemory(request);
        }

        [HttpPost]
        [Route("write-memory")]
        public BaseResponse WriteMemory([FromBody] MemoryWriteRequest request)
        {
            return tools.WriteMemory(request);
        }

        [HttpPost]
        [Route("convert")]
        public ConversionResponse Convert([FromBody] ConversionRequest request)
        {
            return tools.Convert(request);
        }

        [HttpPost]
        [Route("aob-scan")]
        public AobScanResponse AOBScan([FromBody] AobScanRequest request)
        {
            return tools.AOBScan(request);
        }

        [HttpPost]
        [Route("disassemble")]
        public DisassemblerResponse Disassemble([FromBody] DisassemblerRequest request)
        {
            return tools.Disassemble(request);
        }

        [HttpPost]
        [Route("memscan")]
        public MemScanResponse MemScan([FromBody] MemScanScanRequest request)
        {
            return tools.Scan(request);
        }

        [HttpPost]
        [Route("memscan-reset")]
        public MemScanResponse MemScanReset()
        {
            return tools.ResetScan();
        }

        [HttpPost]
        [Route("get-address-safe")]
        public GetAddressSafeResponse GetAddressSafe([FromBody] GetAddressSafeRequest request)
        {
            return tools.GetAddressSafe(request);
        }

        [HttpPost]
        [Route("get-name-from-address")]
        public GetNameFromAddressResponse GetNameFromAddress([FromBody] GetNameFromAddressRequest request)
        {
            return tools.GetNameFromAddress(request);
        }

        [HttpPost]
        [Route("in-module")]
        public InModuleResponse InModule([FromBody] InModuleRequest request)
        {
            return tools.InModule(request);
        }

        [HttpPost]
        [Route("in-system-module")]
        public InSystemModuleResponse InSystemModule([FromBody] InSystemModuleRequest request)
        {
            return tools.InSystemModule(request);
        }

        [HttpGet]
        [Route("health")]
        public IHttpActionResult GetHealth()
        {
            return Ok(new
            {
                status = "healthy",
                server = ServerConfig.ConfigServerName,
                version = typeof(CheatEngineController).Assembly.GetName().Version?.ToString(),
                timestamp = System.DateTime.UtcNow
            });
        }
    }
}
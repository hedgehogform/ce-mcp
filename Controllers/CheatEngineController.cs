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
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace JayTom.Dws.PostInApi.Controllers {

    [Route("FjjService/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase {

        //CommFJJ
        [HttpGet("CommFJJ")]
        public Task<string> Test(string? wsdl, CancellationToken cancellationToken) {
            NLog.LogManager.GetCurrentClassLogger().Info(wsdl);

            return Task.FromResult($"#HEAD::0::null::||#END");
        }
    }
}
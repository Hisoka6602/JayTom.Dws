using System.Net;
using System.Linq;
using System.Net.Sockets;
using JayTom.Dws.CloudApi.Vo;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JayTom.Dws.CloudApi.Filter {

    public class IpAddressFilterAttribute : ActionFilterAttribute {
        private readonly string[] _allowedIps;

        public IpAddressFilterAttribute(params string[] allowedIps) {
            _allowedIps = allowedIps;
            _allowedIps = allowedIps.Concat(GetLocalIpAddresses()).ToArray();
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext) {
            var requestIp = filterContext.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            if (!_allowedIps.Contains(requestIp)) {
                filterContext.Result = JsonResultVo.Fail("您无权执行该操作");
            }
            base.OnActionExecuting(filterContext);
        }

        private List<string> GetLocalIpAddresses() {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            return (from ip in host.AddressList where ip.AddressFamily == AddressFamily.InterNetwork select ip.ToString()).ToList();
        }
    }
}
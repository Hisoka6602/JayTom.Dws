using System;
using System.Threading;
using System.Threading.Tasks;

namespace JayTom.Dws.Client.Service.BackgroundService
{

    public class AutoUploadAndOutPutBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private DateTime _starTime = DateTime.Now;

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //补输出和上传(未上传的内容继续上传,从数据库获取),只上传时间小于_starTime的数据
            throw new NotImplementedException();
        }
    }
}
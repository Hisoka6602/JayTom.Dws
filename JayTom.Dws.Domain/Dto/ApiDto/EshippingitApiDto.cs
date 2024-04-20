using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.ApiDto {

    public class EshippingitApiDto {

        /// <summary>
        /// 域名
        /// </summary>
        public string Domain { get; set; } = "https://qa.gateway.eshippingit.com";

        /// <summary>
        /// 超时时间
        /// </summary>
        public int TimeOut { get; set; } = 1000;

        public string Authorization { get; set; } = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJwYXlsb2FkIjoie1wiaWRcIjpcIjE0MTM2ODQwMzE0NTQ4NDI5MTlcIixcIm5hbWVcIjpcIuiUoeWuuOabplwiLFwidElkXCI6MSxcIm1JZFwiOjEsXCJtTmFtZVwiOlwi5rex5Zyz5LiA5rW36YCa5YWo55CD5L6b5bqU6ZO-566h55CG5pyJ6ZmQ5YWs5Y-4XCIsXCJhSWRcIjoxfSIsImlzcyI6IlNFUlZJQ0UiLCJleHAiOjE3MTM2MDIwMzQsImlhdCI6MTcxMjczODAzNH0.Zee9jgBJdouBAR3R3G1utcFLOt98UZAeaWbMy0VeViw";
        public string Endpoint { get; set; } = "oss-cn-shanghai.aliyuncs.com";
        public string BucketName { get; set; } = "esit-open-qa";
        public int RetryCount { get; set; } = 2;
        public int RetryInterval { get; set; } = 1;

        /// <summary>
        /// 设备名称
        /// </summary>
        public string Machine { get; set; } = string.Empty;
    }
}
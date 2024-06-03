using System;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PostSoapCoreService.Service {

    [ServiceContract(Namespace = "http://serverNs.webservice.pcs.jdpt.chinapost.cn/")]
    public interface ICommFjjService {

        /// <summary>
        /// 路由格口
        /// </summary>
        /// <param name="arg0"></param>
        /// <returns></returns>
        [OperationContract, XmlSerializerFormat]
        Task<string> getLXGK(string arg0);

        /// <summary>
        /// 格口查询
        /// </summary>
        /// <param name="arg0"></param>
        /// <returns></returns>
        [OperationContract, XmlSerializerFormat]
        Task<string> getGKCX(string arg0);

        /// <summary>
        /// 锁格/解锁
        /// </summary>
        /// <param name="arg0"></param>
        /// <returns></returns>
        [OperationContract, XmlSerializerFormat]
        Task<string> getGKSG(string arg0);
    }
}
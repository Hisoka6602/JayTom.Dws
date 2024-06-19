using System;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PostSoapCoreService.Service {

    [ServiceContract(Namespace = "http://serverNs.webservice.pcs.jdpt.chinapost.cn/")]
    public interface ICommFjjService {

        /// <summary>
        /// 路由格口
        /// </summary>
        /// <param name="arg0"></param>
        /// <returns></returns>
        [OperationContract(Name = "getLXGK")]
        [return: MessageParameter(Name = "return")]
        Task<string> GetLxgk(string arg0);

        /// <summary>
        /// 格口查询
        /// </summary>
        /// <param name="arg0"></param>
        /// <returns></returns>
        [OperationContract(Name = "getGKCX")]
        [return: MessageParameter(Name = "return")]
        Task<string> GetGkcx(string arg0);

        /// <summary>
        /// 锁格/解锁
        /// </summary>
        /// <param name="arg0"></param>
        /// <returns></returns>
        [OperationContract(Name = "getGKSG")]
        [return: MessageParameter(Name = "return")]
        Task<string> GetGksg(string arg0);

        /// <summary>
        /// 格口状态
        /// </summary>
        /// <param name="arg0"></param>
        /// <returns></returns>
        [OperationContract(Name = "getGkzt")]
        [return: MessageParameter(Name = "return")]
        public Task<string> GetGkzt(string arg0);
    }

    [DataContract(Namespace = "http://serverNs.webservice.pcs.jdpt.chinapost.cn/")]
    public class CommFjjResponse {

        [DataMember(Name = "return")]
        public string Result { get; set; } = string.Empty;
    }
}
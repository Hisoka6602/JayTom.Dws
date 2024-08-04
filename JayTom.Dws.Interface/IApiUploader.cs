using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Interface {

    public interface IApiUploader<T> where T : BaseApiParameters, new() {
        //上传信息请求格口
        //扫描包裹
        //发送分拣报告
        //发送揽件报告
        //发送集包报告
    }
}
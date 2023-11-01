using System;
using JayTom.Dws.Client.Models;

namespace JayTom.Dws.Client.Service {

    public interface IComputerInfoReporter {

        /// <summary>
        /// 获取到电脑信息
        /// </summary>
        event EventHandler<ComputerInfoModel> ComputerInfoReceived;

        /// <summary>
        /// 提交信息
        /// </summary>
        /// <param name="e"></param>
        void OnComputerInfoReceived(ComputerInfoModel e);

        //设置风扇转速
    }
}
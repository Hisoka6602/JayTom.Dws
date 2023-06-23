using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Repository.Logs {

    public interface IInstructionLogRepository : IRepository<InstructionLogInfoModel> {
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wms.Domain.Enums.Inbound
{
    public enum GRIStatus
    {
        Pending = 1,      // Chưa nhận gì
        Partial = 2,      // Đã nhận 1 phần
        Complete = 3      // Đã nhận đủ
    }

}


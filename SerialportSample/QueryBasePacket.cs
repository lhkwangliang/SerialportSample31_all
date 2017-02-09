using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerialportSample
{
    class QueryBasePacket : Packet
    {
        public override byte[] pack(List<ParamItem> paramList)
        {
            base.command = 0x40;
            return base.pack(paramList);
        }
    }
}

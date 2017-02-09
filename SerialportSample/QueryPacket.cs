using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialportSample
{
    class QueryPacket : Packet
    {
        public override byte[] pack(List<ParamItem> paramList)
        {
            base.command = 0xC2;
            return base.pack(paramList);
        }

    }
}

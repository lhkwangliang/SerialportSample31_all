using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialportSample
{
    class SpecSetPacket : Packet
    {
        public override byte[] pack(List<ParamItem> paramList)
        {
            base.command = 0xC5;
            return base.pack(paramList);
        }
    }
}

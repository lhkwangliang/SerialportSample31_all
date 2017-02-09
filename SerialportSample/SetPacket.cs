using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialportSample
{
    class SetPacket : Packet
    {

        public override byte[] pack(List<ParamItem> paramList)
        {
            base.command = 0xC1;
            return base.pack(paramList);
        }

        /*
        public Dictionary<string, ParamItem> parse(byte[] package)
        {
            Dictionary<string, ParamItem> map = new Dictionary<string, ParamItem>();
            
            List<byte> ls = new List<byte>(package.Length);
            ls.AddRange(package);
            ls.RemoveRange(0, 7);
            ls.RemoveRange(package.Length - 10, 3);

            while (ls.Count > 0)
            {
                ParamItem p = newParamItem(ls);
                if (p != null)
                {
                    map.Add(p.ParamName, p);
                }
            }

            return map;
        }

        private ParamItem newParamItem(List<byte> buffer)
        {
            try
            {
                string paramId = getParamId(buffer, 0, 2).ToUpper();
                int paramLength = getParamLength(buffer, 2);
                byte[] pv = getParamValue(buffer, 3, paramLength);
                buffer.RemoveRange(0, paramLength + 3);
                
                if (!Program.paramsMap.ContainsKey(paramId))
                {
                    return null;
                }
                ParamItem paramItem = Program.paramsMap[paramId];
                string paramType = paramItem.Type;
                int paramScale = paramItem.Scale;
                string paramName = paramItem.ParamName;
                string paramUnit = paramItem.Unit;
                string paramValue = "";

                if (paramType == "string")
                {
                    paramValue = System.Text.Encoding.Default.GetString(pv);
                }
                else if (paramType == "table")
                {

                }
                else
                {
                    float value = 0;
                    if (paramType == "sint2")
                    {
                        value = System.BitConverter.ToInt16(pv, 0);
                    }
                    else if (paramType == "uint2")
                    {
                        value = System.BitConverter.ToUInt16(pv, 0);
                    }
                    else if (paramType == "sint1")
                    {
                        int t = pv[0];
                        if (t <= 127)
                        {
                            value = t;
                        }
                        else
                        {
                            value = 127 - t;
                        }
                    }
                    else if (paramType == "uint1")
                    {
                        value = pv[0];
                    }
                    if (paramScale <= 0)
                    {
                        paramValue = value.ToString();
                    }
                    else
                    {
                        paramValue = (value / paramScale).ToString();
                    }
                }

                return new ParamItem(paramId, paramName, paramLength, paramValue, paramScale, paramType, paramUnit);
            }
            catch (Exception e)
            {
                //解析参数失败
                throw new Exception("解析参数失败: "+e.Message);
            }
        }

        private string getParamId(List<byte> buffer, int index, int count)
        {
            byte[] data = new byte[2];
            buffer.CopyTo(index, data, 0, count);

            //int res = System.BitConverter.ToInt16(data, 0);
            //return res.ToString("X2");
            //return Convert.ToString(res, 16);
            StringBuilder sb = new StringBuilder();
            sb.Append(data[1].ToString("x2"));
            sb.Append(data[0].ToString("x2"));
            return sb.ToString();
        }

        private int getParamLength(List<byte> buffer, int index)
        {
            byte[] data = new byte[2];
            buffer.CopyTo(index, data, 0, 1);
            return System.BitConverter.ToInt16(data, 0);
        }

        private byte[] getParamValue(List<byte> buffer, int index, int count)
        {
            byte[] data = new byte[count];
            buffer.CopyTo(index, data, 0, count);
            return data;
        }
         */
    }
}

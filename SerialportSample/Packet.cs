using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialportSample
{
    class Packet
    {
        public int stx = 0x4E;//起始标志
        public int type = 0x01;//协议类型
        public int version = 0x11;//协议版本
        public int deviceId = 0xFF;//设备编号:本机设备号为0，表示是带了从机的主机；为0XFF，表示是不带从机的主机；0X01~0XFE表示从机
        public int moduleAddress = 0;//模块地址
        public int command = 0xC5;

        public virtual byte[] pack(List<ParamItem> paramList)
        {
            List<byte> buffer = new List<byte>();
            buffer.Add((byte)this.stx);
            buffer.Add((byte)this.type);
            buffer.Add((byte)this.version);
            buffer.Add((byte)this.deviceId);
            buffer.Add((byte)this.moduleAddress);
            buffer.Add((byte)this.command);
            buffer.Add(255); //应答标志
            foreach (ParamItem paramItem in paramList)
            {
                buffer.AddRange(hexStringToByteArray(paramItem.ParamId));
                buffer.Add(Convert.ToByte(paramItem.Length.ToString("X2"), 16));
                if (paramItem.Type == "string")
                {
                    if (paramItem.ParamValue == "0")//默认值
                    {
                        for (int i = 0; i < paramItem.Length; i++)
                        {
                            buffer.Add(0);
                        }
                    }
                    else
                    {
                        byte[] vals = Encoding.ASCII.GetBytes(paramItem.ParamValue);
                        if (vals.Length <= paramItem.Length)
                        {
                            buffer.AddRange(vals);
                            for (int j = 0; j < paramItem.Length - vals.Length; j++)
                            {
                                buffer.Add(0);
                            }
                        }
                    }
                }
                else if (paramItem.Type.IndexOf("table") > -1)
                {
                    if (paramItem.Type == "table type:sint2+uint2*10;scale:10,0")
                    {
                        if (paramItem.ParamValue == "0")//默认值
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                buffer.AddRange(BitConverter.GetBytes(short.Parse("0")));
                                buffer.AddRange(BitConverter.GetBytes(ushort.Parse("0")));
                            }
                        }
                        else
                        {
                            string[] values = paramItem.ParamValue.Split(' ');
                            for (int j = 0; j < values.Length; j++)
                            {
                                string[] val = values[j].Split(',');
                                buffer.AddRange(BitConverter.GetBytes(short.Parse((float.Parse(val[0]) * 10).ToString())));
                                buffer.AddRange(BitConverter.GetBytes(ushort.Parse(val[1])));
                            }
                        }
                        
                    }
                    else if (paramItem.Type == "table type:uint1+uint1+sint1*3;scale:0,0,2")
                    {
                        if (paramItem.ParamValue == "0")//默认值
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                buffer.Add(byte.Parse("0"));
                                buffer.Add(byte.Parse("0"));
                                buffer.Add(byte.Parse("0"));
                            }
                        }
                        else
                        {
                            string[] values = paramItem.ParamValue.Split(' ');
                            for (int j = 0; j < values.Length; j++)
                            {
                                string[] val = values[j].Split(',');
                                buffer.Add(byte.Parse(val[0]));
                                buffer.Add(byte.Parse(val[1]));
                                float value3 = float.Parse(val[2]) * 2;
                                if (value3 < 0)
                                {
                                    value3 = 127 - value3;
                                }
                                buffer.Add(byte.Parse(value3.ToString()));
                            }
                        }
                    }
                }
                else
                {
                    if (paramItem.Type == "sint2")
                    {
                        buffer.AddRange(BitConverter.GetBytes(short.Parse(calcScale(paramItem))));
                    }
                    else if (paramItem.Type == "uint2")
                    {
                        buffer.AddRange(BitConverter.GetBytes(ushort.Parse(calcScale(paramItem))));
                    }
                    else if (paramItem.Type == "sint1")
                    {
                        int value = int.Parse(calcScale(paramItem));
                        if (value < 0)
                        {
                            value = 127 - value;
                        }
                        buffer.Add(byte.Parse(value.ToString()));
                    }
                    else if (paramItem.Type == "uint1")
                    {
                        buffer.Add(byte.Parse(calcScale(paramItem)));
                    }
                }
            }

            byte[] data = new byte[buffer.Count - 1];
            buffer.CopyTo(1, data, 0, data.Length);

            CRC c = new CRC(InitialCrcValue.Zeros);
            byte[] res = c.ComputeChecksumBytes(data);
            buffer.AddRange(res);
            buffer.Add((byte)stx);

            //StringBuilder sb = new StringBuilder();
            //foreach (byte b in buffer)
            //{
            //    sb.Append(b.ToString("X2"));
            //}

            //return buffer.ToArray<byte>();
            return escapeCode(buffer.ToArray<byte>());
        }

        private string calcScale(ParamItem paramItem)
        {
            if (paramItem.Scale != 0)
            {
                return (float.Parse(paramItem.ParamValue) * paramItem.Scale).ToString();
            }
            return paramItem.ParamValue;
        }

        private byte[] hexStringToByteArray(string arg)
        {
            byte[] b = new byte[2];
            //逐个字符变为16进制字节数据
            b[0] = Convert.ToByte(arg.Substring(2, 2), 16);
            b[1] = Convert.ToByte(arg.Substring(0, 2), 16);
            return b;
        }

        //转码
        private byte[] escapeCode(byte[] b)
        {
            List<byte> ls = new List<byte>(b.Length);
            ls.AddRange(b);
            byte[] data = new byte[b.Length - 2];//去掉起始位和结束位的包
            ls.RemoveRange(0, 1);
            ls.CopyTo(0, data, 0, data.Length);//取出包
            ls.Clear();

            string l = "";
            for (int i = 0; i < data.Length; i++)
            {
                l = data[i].ToString("X2");
                if (l == "4E")
                {
                    ls.Add((int)0x5E);
                    ls.Add((int)0x4D);
                }
                else if (l == "5E")
                {
                    ls.Add((int)0x5E);
                    ls.Add((int)0x5D);
                }
                else
                {
                    ls.Add(data[i]);
                }
            }
            //解码完成，重新添加头尾
            data = new byte[ls.Count];
            ls.CopyTo(0, data, 0, data.Length);
            ls.Clear();
            ls.Add((int)0x4E);
            ls.AddRange(data);
            ls.Add((int)0x4E);

            return ls.ToArray<byte>();
        }



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
                    /*List<byte> ls = new List<byte>();
                    ls.AddRange(pv);
                    for (int m = pv.Length - 1; m >= 0; m--)
                    {
                        if (pv[m] == 0)
                            ls.RemoveAt(m);
                        else
                            break;
                    }
                    paramValue = System.Text.Encoding.Default.GetString(ls.ToArray<byte>());*/
                    paramValue = System.Text.Encoding.ASCII.GetString(pv);
                }
                else if (paramType.IndexOf("table") > -1)
                {
                    if (paramType == "table type:sint2+uint2*10;scale:10,0")
                    {
                        StringBuilder sb = new StringBuilder();
                        List<byte> ls = new List<byte>();
                        ls.AddRange(pv);
                        for (int i = 0; i < 10; i++)
                        {
                            byte[] v1 = new byte[2];
                            byte[] v2 = new byte[2];
                            ls.CopyTo(0, v1, 0, 2);
                            ls.CopyTo(2, v2, 0, 2);
                            ls.RemoveRange(0, 4);
                            float value1 = System.BitConverter.ToInt16(v1, 0);
                            value1 = value1 / 10;
                            float value2 = System.BitConverter.ToUInt16(v2, 0);
                            sb.Append(value1);
                            sb.Append(",");
                            sb.Append(value2);
                            sb.Append(" ");
                        }
                        sb.Remove(sb.Length - 1, 1);
                        paramValue = sb.ToString();
                    }
                    else if (paramType == "table type:uint1+uint1+sint1*3;scale:0,0,2")
                    {
                        StringBuilder sb = new StringBuilder();
                        List<byte> ls = new List<byte>();
                        ls.AddRange(pv);
                        for (int i = 0; i < 3; i++)
                        {
                            byte[] data = new byte[3];
                            ls.CopyTo(0, data, 0, 3);
                            ls.RemoveRange(0, 3);
                            int value1 = data[0];
                            int value2 = data[1];
                            float value3 = 0;
                            int t = data[2];
                            if (t <= 127)
                            {
                                value3 = t;
                            }
                            else
                            {
                                value3 = 127 - t;
                            }
                            value3 = value3 / 2;
                            sb.Append(value1);
                            sb.Append(",");
                            sb.Append(value2);
                            sb.Append(",");
                            sb.Append(value3);
                            sb.Append(" ");
                        }
                        sb.Remove(sb.Length - 1, 1);
                        paramValue = sb.ToString();
                    }
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
                throw new Exception("解析参数失败: " + e.Message);
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialportSample
{
    public class ParamItem
    {

        public ParamItem()
        {
        }

        public ParamItem(string paramId, string paramName, int length, string paramValue, int scale, string type, string unit)
        {
            this.paramId = paramId;
            this.paramName = paramName;
            this.length = length;
            this.paramValue = paramValue;
            this.scale = scale;
            this.type = type;
            this.unit = unit;
        }

        private string paramId;
        public string ParamId
        {
            get { return paramId; }
            set { paramId = value; }
        }

        private string paramName;
        public string ParamName
        {
            get { return paramName; }
            set { paramName = value; }
        }

        private int length;
        public int Length
        {
            get { return length; }
            set { length = value; }
        }

        private string paramValue; //type=table时，"value11,value12 value21,value22 value31,value32 ..."
        public string ParamValue
        {
            get
            {
                if (this.paramValue == "" && this.type != "string")
                {
                    return "0";
                }
                else if (this.unit != "" && this.paramValue.IndexOf(this.unit) > -1)
                {
                    return this.paramValue.Substring(0, this.paramValue.Length - this.unit.Length);
                }
                else
                {
                    return paramValue;
                }
            }
            set { paramValue = value; }
        }

        private int scale;
        public int Scale
        {
            get { return scale; }
            set { scale = value; }
        }

        private string type;
        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        private string unit;
        public string Unit
        {
            get { return unit; }
            set { unit = value; }
        }
    }
}

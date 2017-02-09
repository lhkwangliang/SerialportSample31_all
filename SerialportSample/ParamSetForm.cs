using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SerialportSample
{
    public partial class ParamSetForm : Form
    {
        public string type;
        public string value;
        public string unit;
        public int scale;
        public float maximum;
        public float minimum;
        public string paramId;
        public ParamSetForm()
        {
            InitializeComponent();
        }

        private void ParamSetForm_Load(object sender, EventArgs e)
        {
            this.Show();
            if (type == "string")
            {
                pvParamValueNum.Visible = false;
                pvParamValueStr.Visible = true;
                pvParamValueStr.Location = new Point(37, 12);
                lblParamUnit.Text = unit;
                pvParamValueStr.Focus();
            }
            else if (type == "float")
            {
                pvParamValueNum.Visible = true;
                pvParamValueStr.Visible = false;
                pvParamValueNum.Location = new Point(37, 12);
                lblParamUnit.Text = unit;
                pvParamValueNum.Maximum = decimal.Parse(maximum.ToString());
                pvParamValueNum.Minimum = decimal.Parse(minimum.ToString());
                pvParamValueNum.DecimalPlaces = 1;
                pvParamValueNum.Focus();
            }
            else if (type == "int")
            {
                pvParamValueNum.Visible = true;
                pvParamValueStr.Visible = false;
                pvParamValueNum.Location = new Point(37, 12);
                lblParamUnit.Text = unit;
                pvParamValueNum.Maximum = decimal.Parse(maximum.ToString());
                pvParamValueNum.Minimum = decimal.Parse(minimum.ToString());
                pvParamValueNum.DecimalPlaces = 0;
                pvParamValueNum.Focus();
            }

            if (scale == 10)
            {
                pvParamValueNum.Increment = decimal.Parse("0.1");
            }
            else if (scale == 2)
            {
                pvParamValueNum.Increment = decimal.Parse("0.5");
            }
            else
            {
                pvParamValueNum.Increment = decimal.Parse("1");
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.value = null;
            this.Dispose();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (type == "string")
            {
                this.value = pvParamValueStr.Text.ToString();
            }
            else if (type == "float" || type == "int")
            {
                this.value = pvParamValueNum.Value.ToString();
            }
            this.Dispose();
        }

        private void pvParamValueNum_ValueChanged(object sender, EventArgs e)
        {
            MainForm main = (MainForm)this.Owner;
            if (paramId == "F002")
            {
                List<ParamItem> paramList = new List<ParamItem>();
                ParamItem pm = null;
                byte[] data = new byte[2];//特殊处理(高位在前，低位在后)
                data[0] = 8;
                data[1] = byte.Parse(pvParamValueNum.Value.ToString());
                int value = System.BitConverter.ToUInt16(data, 0);
                pm = new ParamItem("F002", "pvModuleAddr", 2, value.ToString(), 0, "uint2", "");
                paramList.Add(pm);
                main.serialSetBase(paramList);
                main.logger("设置模块地址: " + pvParamValueNum.Value);
            }
            else if (paramId == "0024")
            {
                List<ParamItem> paramList = new List<ParamItem>();
                ParamItem pm = null;
                pm = new ParamItem("0024", "pvDigitalAtt", 1, pvParamValueNum.Value.ToString(), 2, "uint1", "dB");
                paramList.Add(pm);
                main.serialSetBase(paramList);
                main.logger("设置ATT: " + pvParamValueNum.Value);
            }
            else if (paramId == "0018")
            {
                List<ParamItem> paramList = new List<ParamItem>();
                ParamItem pm = null;
                pm = new ParamItem("0018", "pvAlcControl", 2, pvParamValueNum.Value.ToString(), 10, "sint2", "dBm");
                paramList.Add(pm);

                main.serialSetBase(paramList);
                main.logger("设置ALC: " + pvParamValueNum.Value);
            }
        }

        private void PressEnter_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSave_Click(sender, null);
            }
        }
    }
}

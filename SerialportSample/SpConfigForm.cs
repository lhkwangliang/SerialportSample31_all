using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SerialportSample
{
    public partial class SpConfigForm : Form
    {
        public SpConfigForm()
        {
            InitializeComponent();
            SpConfigEntity entry = getSpConfigEntity();
            //初始化下拉串口名称列表框  
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            cbPortName.Items.AddRange(ports);
            if(cbPortName.Items.Count <= 0)
            {
                cbPortName.SelectedIndex = -1;
            }else{
                if (entry.PortName == "")
                {
                    cbPortName.SelectedIndex = (cbPortName.Items.Count - 1);
                }
                else if (cbPortName.Text != entry.PortName)
                {
                    cbPortName.Text = entry.PortName;
                }
            }
            if (int.Parse(cbBaudrate.Text) != entry.Baudrate)
            {
                cbBaudrate.Text = entry.Baudrate.ToString();
            }
            if (cbParityBit.Text != entry.ParityBit)
            {
                cbParityBit.Text = entry.ParityBit;
            }
            if (int.Parse(cbDataBit.Text) != entry.DataBit)
            {
                cbDataBit.Text = entry.DataBit.ToString();
            }
            if (int.Parse(cbStopBit.Text) != entry.StopBit)
            {
                cbStopBit.Text = entry.StopBit.ToString();
            }
        }

        private SpConfigEntity getSpConfigEntity()
        {
            SpConfigEntity entry = new SpConfigEntity();
            try
            {
                entry.PortName = OperateIniFile.ReadIniData("SerialPort", "PortName", "", OperateIniFile.getFilePath());
                entry.Baudrate = Convert.ToInt32(OperateIniFile.ReadIniData("SerialPort", "Baudrate", "9600", OperateIniFile.getFilePath()));
                entry.ParityBit = OperateIniFile.ReadIniData("SerialPort", "ParityBit", "无", OperateIniFile.getFilePath());
                entry.DataBit = Convert.ToInt32(OperateIniFile.ReadIniData("SerialPort", "DataBit", "8", OperateIniFile.getFilePath()));
                entry.StopBit = Convert.ToInt32(OperateIniFile.ReadIniData("SerialPort", "StopBit", "1", OperateIniFile.getFilePath()));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return entry;
        }

        private void setSpConfigEntity(SpConfigEntity entry)
        {
            try
            {
                OperateIniFile.WriteIniData("SerialPort", "PortName", entry.PortName, OperateIniFile.getFilePath());
                OperateIniFile.WriteIniData("SerialPort", "Baudrate", entry.Baudrate.ToString(), OperateIniFile.getFilePath());
                OperateIniFile.WriteIniData("SerialPort", "ParityBit", entry.ParityBit, OperateIniFile.getFilePath());
                OperateIniFile.WriteIniData("SerialPort", "DataBit", entry.DataBit.ToString(), OperateIniFile.getFilePath());
                OperateIniFile.WriteIniData("SerialPort", "StopBit", entry.StopBit.ToString(), OperateIniFile.getFilePath());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (cbPortName.Text == "")
            {
                cbPortName.Focus();
            }
            else if (int.Parse(cbBaudrate.Text) <= 0)
            {
                cbBaudrate.Focus();
            }
            else
            {
                Program.globalPortName = cbPortName.Text;
                Program.globalBaudrate = int.Parse(cbBaudrate.Text);

                SpConfigEntity entry = new SpConfigEntity();
                entry.PortName = cbPortName.Text;
                entry.Baudrate = Convert.ToInt32(cbBaudrate.Text);
                entry.ParityBit = cbParityBit.Text;
                entry.DataBit = Convert.ToInt32(cbDataBit.Text);
                entry.StopBit = Convert.ToInt32(cbStopBit.Text);
                setSpConfigEntity(entry);

                this.Dispose();
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}

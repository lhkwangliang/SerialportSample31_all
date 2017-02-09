using HMIControls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SerialportSample
{
    public partial class MainForm : Form
    {
        private bool connected;//确认是否连接上功放
        private int currentEditRowIndex;
        private string currentEditCellValue = "0";
        private System.Timers.Timer onlineTimer = new System.Timers.Timer(20000);
        private System.Timers.Timer pollingTimer = new System.Timers.Timer(3000);   //实例化Timer类，设置间隔时间为3000毫秒
        private System.Timers.Timer delayTimer = new System.Timers.Timer(500); //解决连续点击微调按钮，反应迟钝的问题
        private SerialPort comm = new SerialPort();
        private StringBuilder builder = new StringBuilder();//避免在事件处理方法中反复的创建，定义到外面。
        private long received_count = 0;//接收计数
        private bool Listening = false;//是否没有执行完invoke相关操作
        private List<byte> buffer = new List<byte>(4096);//默认分配1页内存，并始终限制不允许超过
        private byte[] binary_data_1 = new byte[11];//AA 44 05 01 02 03 04 05 EA

        private List<byte> sendBuffer = new List<byte>(4096);//发送

        public MainForm()
        {
            InitializeComponent();
        }

        public void logger(string data)
        {
            try
            {
                //更新界面  
                //因为要访问ui资源，所以需要使用invoke方式同步ui。  
                this.Invoke((EventHandler)(delegate
                {
                    if (this.txGet.TextLength > 1048576) //1024 * 1024 = 1048576
                    {
                        this.txGet.Text = "";
                    }
                    //追加的形式添加到文本框末端，并滚动到最后。
                    this.txGet.AppendText(data + System.Environment.NewLine);
                }));
            }
            catch (Exception e)
            {
            }
        }

        public void error(string data)
        {
            try
            {
                //更新界面  
                //因为要访问ui资源，所以需要使用invoke方式同步ui
                this.Invoke((EventHandler)(delegate
                {
                    //追加的形式添加到文本框末端，并滚动到最后
                    txError.AppendText(data + System.Environment.NewLine);
                }));
            }
            catch (Exception e)
            {
            }
        }

        private string toHex(byte[] data)
        {
            builder.Clear();
            foreach (byte b in data)
            {
                builder.Append(b.ToString("X2"));
            }

            return builder.ToString();
        }

        private void delayOneSeconds()
        {
            try
            {
                System.Threading.Thread.Sleep(300);
            }
            catch (Exception e) { }
        }

        private void btnSpConfig_Click(object sender, EventArgs e)
        {
            Form spConfig = new SpConfigForm();
            spConfig.ShowDialog();
        }

        private TabPage loggerTabPage;
        private void MainForm_Load(object sender, EventArgs e)
        {
            initComm();
            initOnlineTimer();
            queryUnitParamsTimer();
            initDelayTimer();

            ShowTable(pvInputPowerAd, null);
            ShowTable(pvOutputPowerAd, null);
            ShowTable(pvIncidentPowerAd, null);
            ShowTable(pvAlcPowerVoltage, null);
            ShowAttTable(pvAttCompensation, null);

            loggerTabPage = tabControl1.TabPages[4];
            tabControl1.TabPages.RemoveAt(4);
        }

        public void ShowTable(MyDataGridView myDataGridView, List<string[]> list)
        {
            //创建表
            DataTable dt = new DataTable();
            //添加列
            dt.Columns.Add("采样电压", typeof(string)); //数据类型为 文本
            dt.Columns.Add("定标点", typeof(float)); //数据类型为 整形
            //通过行框架添加数据
            if (list == null)
            {
                for (int i = 0; i < 10; i++)
                {
                    dt.Rows.Add(0, 0);
                }
            }
            else
            {
                for (int j = 0; j < list.Count; j++)
                {
                    dt.Rows.Add(list[j][1], list[j][0]);
                }
            }
            //设置属性
            myDataGridView.DataSource = dt;
            myDataGridView.AllowUserToAddRows = false;
            myDataGridView.Columns[0].ReadOnly = true;
            myDataGridView.Columns[0].Width = 80;
            myDataGridView.Columns[1].Width = 80;
        }

        public void ShowAttTable(MyDataGridView myDataGridView, List<string[]> list)
        {
            //创建表
            DataTable dt = new DataTable();
            //添加列
            dt.Columns.Add("起始值", typeof(float)); //数据类型为 整形
            dt.Columns.Add("结束值", typeof(float)); //数据类型为 整形
            dt.Columns.Add("补偿值", typeof(float)); //数据类型为 整形
            //通过行框架添加数据
            if (list == null)
            {
                for (int i = 0; i < 3; i++)
                {
                    dt.Rows.Add(0, 0, 0);
                }
            }
            else
            {
                for (int j = 0; j < list.Count; j++)
                {
                    dt.Rows.Add(list[j][0], list[j][1], list[j][2]);
                }
            }
            //设置属性
            myDataGridView.DataSource = dt;
            myDataGridView.AllowUserToAddRows = false;
            myDataGridView.Columns[0].Width = 80;
            myDataGridView.Columns[1].Width = 80;
            myDataGridView.Columns[2].Width = 80;
        }

        private void initComm()
        {
            //初始化SerialPort对象  
            comm.NewLine = "/r/n";
            comm.RtsEnable = true;//根据实际情况吧。  
            //添加事件注册  
            comm.DataReceived += comm_DataReceived;
        }

        private void initOnlineTimer()
        {
            onlineTimer.Elapsed += new System.Timers.ElapsedEventHandler(unitOffline);
        }

        private void queryUnitParamsTimer() 
        {
            pollingTimer.Elapsed += new System.Timers.ElapsedEventHandler(polling); //到达时间的时候执行事件
            pollingTimer.AutoReset = true;   //设置是执行一次（false）还是一直执行(true)
            pollingTimer.Enabled = false;     //是否执行System.Timers.Timer.Elapsed事件
        }

        private void initDelayTimer()
        {
            delayTimer.Elapsed += new System.Timers.ElapsedEventHandler(setDelay); //到达时间的时候执行事件
            delayTimer.AutoReset = false;   //设置是执行一次（false）还是一直执行(true)
            delayTimer.Enabled = false;     //是否执行System.Timers.Timer.Elapsed事件
        }

        public void setDelay(object source, System.Timers.ElapsedEventArgs e)
        {
            delayTimer.Stop();
            if (pvGainVoltage1NumericEnabled)
            {
                pvGainVoltage1NumericEnabled = false;
                setGainVoltage();
            }
            if (pvAlcVoltageNumericEnabled)
            {
                pvAlcVoltageNumericEnabled = false;
                setAlcVoltage();
            }
            if (pvVgVoltage1NumericEnabled)
            {
                pvVgVoltage1NumericEnabled = false;
                setVgVoltage1();
            }
            if (pvVgVoltage2NumericEnabled)
            {
                pvVgVoltage2NumericEnabled = false;
                setVgVoltage2();
            }
            if (pvVgVoltage3NumericEnabled)
            {
                pvVgVoltage3NumericEnabled = false;
                setVgVoltage3();
            }
            if (pvVgVoltage4NumericEnabled)
            {
                pvVgVoltage4NumericEnabled = false;
                setVgVoltage4();
            }
            if (pvVgVoltage5NumericEnabled)
            {
                pvVgVoltage5NumericEnabled = false;
                setVgVoltage5();
            }
            if (pvVgVoltage6NumericEnabled)
            {
                pvVgVoltage6NumericEnabled = false;
                setVgVoltage6();
            }
            if (pvVgVoltage7NumericEnabled)
            {
                pvVgVoltage7NumericEnabled = false;
                setVgVoltage7();
            }
            if (pvVgVoltage8NumericEnabled)
            {
                pvVgVoltage8NumericEnabled = false;
                setVgVoltage8();
            }
            if (pvVgVoltage9NumericEnabled)
            {
                pvVgVoltage9NumericEnabled = false;
                setVgVoltage9();
            }
            if (pvVgVoltage10NumericEnabled)
            {
                pvVgVoltage10NumericEnabled = false;
                setVgVoltage10();
            }
            if (pvVgVoltage11NumericEnabled)
            {
                pvVgVoltage11NumericEnabled = false;
                setVgVoltage11();
            }
            if (pvVgVoltage12NumericEnabled)
            {
                pvVgVoltage12NumericEnabled = false;
                setVgVoltage12();
            }

        }

        private void changeUnitState(bool isOpen)
        {
            this.Invoke((EventHandler)(delegate
                    {
                        if (isOpen)
                        {
                            lbLedUnitState.ButtonColor = Color.Green;
                            btnConn.Text = "断开功放";
                        }
                        else
                        {
                            lbLedUnitState.ButtonColor = Color.Red;
                            btnConn.Text = "连接功放";
                        }
                    }));
        }
        public void unitOffline(object sender, System.Timers.ElapsedEventArgs e)
        {
            changeUnitState(false);
            connected = false;
        }
        
        public void polling(object source, System.Timers.ElapsedEventArgs e)
        {
            queryUnitParams();
            delayOneSeconds();
            queryBaseParams();
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

        private void btnOpenClose_Click(object sender, EventArgs e)
        {
            //根据当前串口对象，来判断操作  
            if (comm.IsOpen)
            {
                //Closing = true;
                while (Listening) Application.DoEvents();
                //打开时点击，则关闭串口  
                comm.Close();
                lbLedSp.ButtonColor = Color.Red;
            }
            else
            {
                SpConfigEntity entry = getSpConfigEntity();
                //关闭时点击，则设置好端口，波特率后打开  
                if (entry.PortName == null || entry.PortName == "")
                {
                    MessageBox.Show("请设置串口");
                    return;
                }
                comm.PortName = entry.PortName;
                comm.BaudRate = entry.Baudrate;
                /*if (entry.ParityBit == "偶")
                {
                   comm.Parity = Parity.Even;
                }
                else if (entry.ParityBit == "奇")
                {
                    comm.Parity = Parity.Odd;
                }
                else
                {
                    comm.Parity = Parity.None;
                }
                comm.DataBits = entry.DataBit;
                if (entry.StopBit == 1)
                {
                    comm.StopBits = StopBits.One;
                }
                else if (entry.StopBit == 2)
                {
                    comm.StopBits = StopBits.Two;
                }
                else
                {
                    comm.StopBits = StopBits.None;
                }*/
                try
                {
                    comm.Open();
                    lbLedSp.ButtonColor = Color.Green;
                }
                catch (Exception ex)
                {

                    changeUnitState(false);
                    //捕获到异常信息，创建一个新的comm对象，之前的不能用了。
                    comm = new SerialPort();
                    initComm();
                    //显示异常信息给客户。  
                    MessageBox.Show(ex.Message);
                }
            }
            //设置按钮的状态  
            btnOpenClose.Text = comm.IsOpen ? "关闭串口" : "打开串口";
            //buttonSend.Enabled = comm.IsOpen;
        }

        void comm_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //if (Closing) return;//如果正在关闭，忽略操作，直接返回，尽快的完成串口监听线程的一次循环  
            try
            {
                Listening = true;//设置标记，说明我已经开始处理数据，一会儿要使用系统UI的。  
                int n = comm.BytesToRead;//先记录下来，避免某种原因，人为的原因，操作几次之间时间长，缓存不一致  
                byte[] buf = new byte[n];//声明一个临时数组存储当前来的串口数据  
                received_count += n;//增加接收计数  
                comm.Read(buf, 0, n);//读取缓冲数据  
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////  
                //<协议解析>  
                bool data_1_catched = false;//缓存记录数据是否捕获到  
                //1.缓存数据  
                buffer.AddRange(buf);
                //2.完整性判断  
                while (buffer.Count >= 11)//至少要包含起始位（1字节）+命令头（6字节）+命令体（至少1字节）+校验（2字节）+结束位（1字节）  
                {
                    //2.1 查找数据头
                    if (buffer[0] == 0x4E)
                    {
                        int oneLen = 0;//一条包含头尾的数据的长度
                        //2.2 查找数据结尾
                        for (int j = 1; j < buffer.Count; j++)
                        {
                            if (buffer[j] == 0x4E)
                            {
                                oneLen = j + 1;
                                break;
                            }
                        }
                        if (oneLen == 0) break;//数据不够的时候什么都不做

                        byte[] one = new byte[oneLen];
                        buffer.CopyTo(0, one, 0, oneLen);//复制一条包含头尾的数据到具体的数据缓存
                        //2.3 对除头尾的所有数据进行转义
                        byte[] real = escapeCode(one);
                        //2.4 探测缓存数据是否有一条数据的字节，如果不够，就不用费劲的做其他验证了
                        if (real.Length < 11)
                        {
                            if (real.Length == 2) //如果是前一个包的结尾与后一个包的开头组成的2字节数据包
                            {
                                buffer.RemoveAt(0);
                                continue;
                            }
                            buffer.RemoveRange(0, oneLen);//从缓存中删除错误数据
                            continue;//继续下一次循环
                        }
                        //这里确保数据长度足够，数据头标志找到，我们开始计算校验
                        //2.5 校验数据，确认数据正确
                        //CRC16校验
                        if (!crc16(real)) //如果数据校验失败，丢弃这一包数据
                        {
                            logger("CRC校验失败: " + toHex(real));
                            buffer.RemoveRange(0, oneLen);//从缓存中删除错误数据
                            continue;//继续下一次循环
                        }
                        binary_data_1 = real;//复制一条完整数据到具体的数据缓存
                        data_1_catched = true;
                        buffer.RemoveRange(0, oneLen);//正确分析一条数据，从缓存中移除数据
                        
                        //至此，已经被找到了一条完整数据。我们将数据直接分析，或是缓存起来一起分析  
                        //我们这里采用的办法是缓存一次，好处就是如果你某种原因，数据堆积在缓存buffer中  
                        //已经很多了，那你需要循环的找到最后一组，只分析最新数据，过往数据你已经处理不及时  
                        //了，就不要浪费更多时间了，这也是考虑到系统负载能够降低。  

                    }
                    else
                    {
                        //这里是很重要的，如果数据开始不是头，则删除数据  
                        buffer.RemoveAt(0);
                    }
                }
                //分析数据  
                if (data_1_catched)
                {
                    //我们的数据都是定好格式的，所以当我们找到分析出的数据1，就知道固定位置一定是这些数据，我们只要显示就可以了
                    //STX(1) TYPE(1) VER(1) NC(1) M-ADDRESS(1) COMM(1) RESPONTION-FLAG(1) DATAINFO(变成) CHKSUM(2) EOI(1)
                    if (binary_data_1[6] != 0) return;

                    string data = "指令:" + binary_data_1[5].ToString("X2") + ", 返回状态:" + binary_data_1[6].ToString("X2") + ", 数据:";
                    for (int k = 0; k < binary_data_1.Length - 10; k++)
                    {
                        data += " " + binary_data_1[k + 7].ToString("X2");
                    }


                    //更新界面  
                    this.Invoke((EventHandler)(delegate
                    {
                        if (!onlineTimer.Enabled)
                        {
                            onlineTimer.Start();
                            //if (!initSwitchButtonFlag)
                            //{
                                //initSwitchButtonFlag = true;
                                //initSwitchButton();//只执行1次
                            //}
                        }
                        else
                        {
                            onlineTimer.Stop();
                            onlineTimer.Start();
                        }
                        pollingTimer.Start();//启动轮询

                        changeUnitState(true);
                        connected = true;

                        txData.Text = data;

                        String command = binary_data_1[5].ToString("X2");
                        //if (command == "C1" || command == "C2" || command == "40" || command == "60")//C1:设置参数值, C2:查询参数值
                        //{
                            try
                            {
                                showUI();
                            }
                            catch (Exception e1)
                            {
                                MessageBox.Show(e1.Message);
                            }
                        //}

                    }));
                }
                //如果需要别的协议，只要扩展这个data_n_catched就可以了。往往我们协议多的情况下，还会包含数据编号，给来的数据进行  
                //编号，协议优化后就是： 头+编号+长度+数据+校验  
                //</协议解析>  
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////  
                //builder.Clear();//清除字符串构造器的内容  
                //显示为16进制
                logger("接收: " + toHex(buf));
                //因为要访问ui资源，所以需要使用invoke方式同步ui。  
                this.Invoke((EventHandler)(delegate
                {
                    //追加的形式添加到文本框末端，并滚动到最后。
                    //this.txGet.AppendText("接收: " + toHex(buf) + System.Environment.NewLine);
                    //修改接收计数  
                    labelGetCount.Text = "Get:" + received_count.ToString();
                }));
            }
            finally
            {
                Listening = false;//我用完了，ui可以关闭串口了。  
            }
        }

        private void showRealValue(string switchState, ParamItem pm)
        {
            if (switchState == "OFF") return;

            List<ParamItem> paramList = new List<ParamItem>();
            paramList.Add(pm);

            serialQuery(paramList);
            logger("查询实时值: " + pm.ParamId);
        }

        private void showUI()
        {
            Packet packet = new Packet();
            Dictionary<string, ParamItem> map = packet.parse(binary_data_1);
            ParamItem pm = new ParamItem();
            //模块参数
            if (map.ContainsKey("pvModuleType"))
            {
                pm = map["pvModuleType"];
                pvModuleType.Text = pm.ParamValue;
            }
            if (map.ContainsKey("pvModuleAddr"))
            {
                pm = map["pvModuleAddr"];
                try
                {
                    //特殊处理(高位在前，低位在后，只显示低位)
                    byte[] t = BitConverter.GetBytes(ushort.Parse(pm.ParamValue));
                    int value = t[1];
                    pvModuleAddr.Text = value.ToString();
                }
                catch (Exception ex) { }
            }
            if (map.ContainsKey("pvProtocolVersion"))
            {
                pm = map["pvProtocolVersion"];
                pvProtocolVersion.Text = "V" + pm.ParamValue;
            }
            if (map.ContainsKey("pvSoftwareVersion"))
            {
                pm = map["pvSoftwareVersion"];
                try
                {
                    byte[] data = BitConverter.GetBytes(ushort.Parse(pm.ParamValue));
                    int l = data[0];
                    int h = data[1];
                    pvSoftwareVersion.Text = "V" + l.ToString() + "." + h.ToString();
                }
                catch (Exception ex) { }
            }
            if (map.ContainsKey("pvProductSerial"))
            {
                pm = map["pvProductSerial"];
                pvProductSerial.Text = pm.ParamValue;
            }
            //设备参数
            if (map.ContainsKey("pvSystemThermometer"))
            {
                pm = map["pvSystemThermometer"];
                pvSystemThermometer.Temperature = int.Parse(pm.ParamValue);
                lbSystemTemperature.Text = pm.ParamValue + pm.Unit;
            }
            if (map.ContainsKey("pvInputPower"))
            {
                pm = map["pvInputPower"];
                pvInputPower.Text = pm.ParamValue + pm.Unit;
                pvInputPower1.Text = pm.ParamValue + pm.Unit;
            }
            if (map.ContainsKey("pvOutputPower"))
            {
                pm = map["pvOutputPower"];
                pvOutputPower.Text = pm.ParamValue + pm.Unit;
                pvOutputPower1.Text = pm.ParamValue + pm.Unit;

            }
            if (map.ContainsKey("pvIncidentPower"))
            {
                pm = map["pvIncidentPower"];
                pvIncidentPower.Text = pm.ParamValue + pm.Unit;
                pvIncidentPower1.Text = pm.ParamValue + pm.Unit;
            }
            if (map.ContainsKey("pvVSWR"))
            {
                pm = map["pvVSWR"];
                pvVSWR.Text = pm.ParamValue + pm.Unit;
            }
            if (map.ContainsKey("pvInputPowerVoltage"))
            {
                pm = map["pvInputPowerVoltage"];
                pvInputPowerVoltage.Text = pm.ParamValue + pm.Unit;
                pvInputPowerVoltage1.Text = pm.ParamValue + pm.Unit;
            }
            if (map.ContainsKey("pvOutputPowerVoltage"))
            {
                pm = map["pvOutputPowerVoltage"];
                pvOutputPowerVoltage.Text = pm.ParamValue + pm.Unit;
                pvOutputPowerVoltage1.Text = pm.ParamValue + pm.Unit;
            }
            if (map.ContainsKey("pvIncidentPowerVoltage"))
            {
                pm = map["pvIncidentPowerVoltage"];
                pvIncidentPowerVoltage.Text = pm.ParamValue + pm.Unit;
                pvIncidentPowerVoltage1.Text = pm.ParamValue + pm.Unit;
            }
            if (map.ContainsKey("pvDigitalAtt"))
            {
                pm = map["pvDigitalAtt"];
                pvDigitalAtt.Text = pm.ParamValue + pm.Unit;
            }
            if (map.ContainsKey("pvAlcControl"))
            {
                pm = map["pvAlcControl"];
                pvAlcControl.Text = pm.ParamValue + pm.Unit;
                pvAlcControl1.Text = pm.ParamValue + pm.Unit;
            }
            if (map.ContainsKey("pvPowerSwitch"))
            {
                pm = map["pvPowerSwitch"];
                pvPowerSwitch.Text = (pm.ParamValue=="0")?"OFF":"ON";
            }
            //增益，ALC调节
            if (map.ContainsKey("pvAlcGainEnabled"))
            {
                pm = map["pvAlcGainEnabled"];
                pvAlcGainEnabled.ButtonState = (pm.ParamValue == "0"); //true:自动, false:手动
                alcGainControl(); //确保pvAlcVoltageSwitch和pvGainVoltageSwitch比pvAlcGainEnabled先解析
            }
            if (map.ContainsKey("pvAlcVoltageSwitch"))
            {
                pm = map["pvAlcVoltageSwitch"];
                pvAlcVoltage.SwitchState = (pm.ParamValue == "0" ? "OFF" : "ON");
                pvAlcVoltage1.SwitchState = (pm.ParamValue == "0" ? "OFF" : "ON");
                //changeTrackBarState(pvAlcVoltage, (pvAlcVoltage.SwitchState == "OFF" ? false : true));
                //changeTrackBarState(pvAlcVoltage1, (pvAlcVoltage1.SwitchState == "OFF" ? false : true));
                //showRealValue(pvAlcVoltage.SwitchState, new ParamItem("D140", "pvAlcRealVoltage", 2, "", 0, "uint2", ""));
            }
            if (map.ContainsKey("pvGainVoltageSwitch"))
            {
                pm = map["pvGainVoltageSwitch"];
                pvGainVoltage1.SwitchState = (pm.ParamValue == "0" ? "OFF" : "ON");
                //changeTrackBarState(pvGainVoltage1, (pvGainVoltage1.SwitchState == "OFF" ? false : true));
                //showRealValue(pvGainVoltage1.SwitchState, new ParamItem("D141", "pvGainRealVoltage", 2, "", 0, "uint2", ""));
            }
            //if (pvAlcGainEnabled.ButtonState && map.ContainsKey("pvGainRealVoltage"))
            if (map.ContainsKey("pvGainRealVoltage"))
            {
                pm = map["pvGainRealVoltage"];
                pvGainVoltage1.Value = int.Parse(pm.ParamValue);
                pvGainVoltage1.fineNumeric.Value = int.Parse(pm.ParamValue);
            }
            //if (pvAlcGainEnabled.ButtonState && map.ContainsKey("pvAlcRealVoltage"))
            if (map.ContainsKey("pvAlcRealVoltage"))
            {
                pm = map["pvAlcRealVoltage"];
                pvAlcVoltage.Value = int.Parse(pm.ParamValue);
                pvAlcVoltage1.Value = int.Parse(pm.ParamValue);
                pvAlcVoltage.fineNumeric.Value = int.Parse(pm.ParamValue);
                pvAlcVoltage1.fineNumeric.Value = int.Parse(pm.ParamValue);
            }
            //栅压调节
            if (map.ContainsKey("pvVgEnabled"))
            {
                pm = map["pvVgEnabled"];
                pvVgEnabled.ButtonState = (pm.ParamValue == "0");
                vgControl(); //确保pvVgSwitch1到pvVgSwitch16比pvVgEnabled先解析
            }
            if (map.ContainsKey("pvVgSwitch1"))
            {
                pm = map["pvVgSwitch1"];
                pvVgVoltage1.SwitchState = (pm.ParamValue == "0" ? "OFF" : "ON");
                changeTrackBarState(pvVgVoltage1, (pvVgVoltage1.SwitchState == "OFF" ? false : true));
                showRealValue(pvVgVoltage1.SwitchState, new ParamItem("D130", "pvVgRealVoltage1", 2, "", 0, "uint2", ""));
                //if (pvVgVoltage1.SwitchState == "ON" && pvVgEnabled.Text == "自动")
                //{
                //    pvVgEnabled_Click(null, null);
                //}
            }
            if (map.ContainsKey("pvVgSwitch2"))
            {
                pm = map["pvVgSwitch2"];
                pvVgVoltage2.SwitchState = (pm.ParamValue == "0" ? "OFF" : "ON");
                changeTrackBarState(pvVgVoltage2, (pvVgVoltage2.SwitchState == "OFF" ? false : true));
                showRealValue(pvVgVoltage2.SwitchState, new ParamItem("D131", "pvVgRealVoltage2", 2, "", 0, "uint2", ""));
                //if (pvVgVoltage2.SwitchState == "ON" && pvVgEnabled.Text == "自动")
                //{
                //    pvVgEnabled_Click(null, null);
                //}
            }
            if (map.ContainsKey("pvVgSwitch3"))
            {
                pm = map["pvVgSwitch3"];
                pvVgVoltage3.SwitchState = (pm.ParamValue == "0" ? "OFF" : "ON");
                changeTrackBarState(pvVgVoltage3, (pvVgVoltage3.SwitchState == "OFF" ? false : true));
                showRealValue(pvVgVoltage3.SwitchState, new ParamItem("D132", "pvVgRealVoltage3", 2, "", 0, "uint2", ""));
                //if (pvVgVoltage3.SwitchState == "ON" && pvVgEnabled.Text == "自动")
                //{
                //    pvVgEnabled_Click(null, null);
                //}
            }
            if (map.ContainsKey("pvVgSwitch4"))
            {
                pm = map["pvVgSwitch4"];
                pvVgVoltage4.SwitchState = (pm.ParamValue == "0" ? "OFF" : "ON");
                changeTrackBarState(pvVgVoltage4, (pvVgVoltage4.SwitchState == "OFF" ? false : true));
                showRealValue(pvVgVoltage4.SwitchState, new ParamItem("D133", "pvVgRealVoltage4", 2, "", 0, "uint2", ""));
                //if (pvVgVoltage4.SwitchState == "ON" && pvVgEnabled.Text == "自动")
                //{
                //    pvVgEnabled_Click(null, null);
                //}
            }
            if (map.ContainsKey("pvVgSwitch5"))
            {
                pm = map["pvVgSwitch5"];
                pvVgVoltage5.SwitchState = (pm.ParamValue == "0" ? "OFF" : "ON");
                changeTrackBarState(pvVgVoltage5, (pvVgVoltage5.SwitchState == "OFF" ? false : true));
                showRealValue(pvVgVoltage5.SwitchState, new ParamItem("D134", "pvVgRealVoltage5", 2, "", 0, "uint2", ""));
                //if (pvVgVoltage5.SwitchState == "ON" && pvVgEnabled.Text == "自动")
                //{
                //    pvVgEnabled_Click(null, null);
                //}
            }
            if (map.ContainsKey("pvVgSwitch6"))
            {
                pm = map["pvVgSwitch6"];
                pvVgVoltage6.SwitchState = (pm.ParamValue == "0" ? "OFF" : "ON");
                changeTrackBarState(pvVgVoltage6, (pvVgVoltage6.SwitchState == "OFF" ? false : true));
                showRealValue(pvVgVoltage6.SwitchState, new ParamItem("D135", "pvVgRealVoltage6", 2, "", 0, "uint2", ""));
                //if (pvVgVoltage6.SwitchState == "ON" && pvVgEnabled.Text == "自动")
                //{
                //    pvVgEnabled_Click(null, null);
                //}
            }
            if (map.ContainsKey("pvVgSwitch7"))
            {
                pm = map["pvVgSwitch7"];
                pvVgVoltage7.SwitchState = (pm.ParamValue == "0" ? "OFF" : "ON");
                changeTrackBarState(pvVgVoltage7, (pvVgVoltage7.SwitchState == "OFF" ? false : true));
                showRealValue(pvVgVoltage7.SwitchState, new ParamItem("D136", "pvVgRealVoltage7", 2, "", 0, "uint2", ""));
                //if (pvVgVoltage7.SwitchState == "ON" && pvVgEnabled.Text == "自动")
                //{
                //    pvVgEnabled_Click(null, null);
                //}
            }
            if (map.ContainsKey("pvVgSwitch8"))
            {
                pm = map["pvVgSwitch8"];
                pvVgVoltage8.SwitchState = (pm.ParamValue == "0" ? "OFF" : "ON");
                changeTrackBarState(pvVgVoltage8, (pvVgVoltage8.SwitchState == "OFF" ? false : true));
                showRealValue(pvVgVoltage8.SwitchState, new ParamItem("D137", "pvVgRealVoltage8", 2, "", 0, "uint2", ""));
                //if (pvVgVoltage8.SwitchState == "ON" && pvVgEnabled.Text == "自动")
                //{
                //    pvVgEnabled_Click(null, null);
                //}
            }
            if (map.ContainsKey("pvVgSwitch9"))
            {
                pm = map["pvVgSwitch9"];
                pvVgVoltage9.SwitchState = (pm.ParamValue == "0" ? "OFF" : "ON");
                changeTrackBarState(pvVgVoltage9, (pvVgVoltage9.SwitchState == "OFF" ? false : true));
                showRealValue(pvVgVoltage9.SwitchState, new ParamItem("D138", "pvVgRealVoltage9", 2, "", 0, "uint2", ""));
                //if (pvVgVoltage9.SwitchState == "ON" && pvVgEnabled.Text == "自动")
                //{
                //    pvVgEnabled_Click(null, null);
                //}
            }
            if (map.ContainsKey("pvVgSwitch10"))
            {
                pm = map["pvVgSwitch10"];
                pvVgVoltage10.SwitchState = (pm.ParamValue == "0" ? "OFF" : "ON");
                changeTrackBarState(pvVgVoltage10, (pvVgVoltage10.SwitchState == "OFF" ? false : true));
                showRealValue(pvVgVoltage10.SwitchState, new ParamItem("D139", "pvVgRealVoltage10", 2, "", 0, "uint2", ""));
                //if (pvVgVoltage10.SwitchState == "ON" && pvVgEnabled.Text == "自动")
                //{
                //    pvVgEnabled_Click(null, null);
                //}
            }
            if (map.ContainsKey("pvVgSwitch11"))
            {
                pm = map["pvVgSwitch11"];
                pvVgVoltage1.SwitchState = (pm.ParamValue == "0" ? "OFF" : "ON");
                changeTrackBarState(pvVgVoltage11, (pvVgVoltage11.SwitchState == "OFF" ? false : true));
                showRealValue(pvVgVoltage11.SwitchState, new ParamItem("D13A", "pvVgRealVoltage11", 2, "", 0, "uint2", ""));
                //if (pvVgVoltage11.SwitchState == "ON" && pvVgEnabled.Text == "自动")
                //{
                //    pvVgEnabled_Click(null, null);
                //}
            }
            if (map.ContainsKey("pvVgSwitch12"))
            {
                pm = map["pvVgSwitch12"];
                pvVgVoltage12.SwitchState = (pm.ParamValue == "0" ? "OFF" : "ON");
                changeTrackBarState(pvVgVoltage12, (pvVgVoltage12.SwitchState == "OFF" ? false : true));
                showRealValue(pvVgVoltage12.SwitchState, new ParamItem("D13B", "pvVgRealVoltage12", 2, "", 0, "uint2", ""));
                //if (pvVgVoltage12.SwitchState == "ON" && pvVgEnabled.Text == "自动")
                //{
                //    pvVgEnabled_Click(null, null);
                //}
            }
            //if (pvVgEnabled.ButtonState && map.ContainsKey("pvVgRealVoltage1"))
            if (map.ContainsKey("pvVgRealVoltage1"))
            {
                pm = map["pvVgRealVoltage1"];
                pvVgVoltage1.Value = int.Parse(pm.ParamValue);
                pvVgVoltage1.fineNumeric.Value = int.Parse(pm.ParamValue);
            }
            //if (pvVgEnabled.ButtonState && map.ContainsKey("pvVgRealVoltage2"))
            if (map.ContainsKey("pvVgRealVoltage2"))
            {
                pm = map["pvVgRealVoltage2"];
                pvVgVoltage2.Value = int.Parse(pm.ParamValue);
                pvVgVoltage2.fineNumeric.Value = int.Parse(pm.ParamValue);
            }
            //if (pvVgEnabled.ButtonState && map.ContainsKey("pvVgRealVoltage3"))
            if (map.ContainsKey("pvVgRealVoltage3"))
            {
                pm = map["pvVgRealVoltage3"];
                pvVgVoltage3.Value = int.Parse(pm.ParamValue);
                pvVgVoltage3.fineNumeric.Value = int.Parse(pm.ParamValue);
            }
            //if (pvVgEnabled.ButtonState && map.ContainsKey("pvVgRealVoltage4"))
            if (map.ContainsKey("pvVgRealVoltage4"))
            {
                pm = map["pvVgRealVoltage4"];
                pvVgVoltage4.Value = int.Parse(pm.ParamValue);
                pvVgVoltage4.fineNumeric.Value = int.Parse(pm.ParamValue);
            }
            //if (pvVgEnabled.ButtonState && map.ContainsKey("pvVgRealVoltage5"))
            if (map.ContainsKey("pvVgRealVoltage5"))
            {
                pm = map["pvVgRealVoltage5"];
                pvVgVoltage5.Value = int.Parse(pm.ParamValue);
                pvVgVoltage5.fineNumeric.Value = int.Parse(pm.ParamValue);
            }
            //if (pvVgEnabled.ButtonState && map.ContainsKey("pvVgRealVoltage6"))
            if (map.ContainsKey("pvVgRealVoltage6"))
            {
                pm = map["pvVgRealVoltage6"];
                pvVgVoltage6.Value = int.Parse(pm.ParamValue);
                pvVgVoltage6.fineNumeric.Value = int.Parse(pm.ParamValue);
            }
            //if (pvVgEnabled.ButtonState && map.ContainsKey("pvVgRealVoltage7"))
            if (map.ContainsKey("pvVgRealVoltage7"))
            {
                pm = map["pvVgRealVoltage7"];
                pvVgVoltage7.Value = int.Parse(pm.ParamValue);
                pvVgVoltage7.fineNumeric.Value = int.Parse(pm.ParamValue);
            }
            //if (pvVgEnabled.ButtonState && map.ContainsKey("pvVgRealVoltage8"))
            if (map.ContainsKey("pvVgRealVoltage8"))
            {
                pm = map["pvVgRealVoltage8"];
                pvVgVoltage8.Value = int.Parse(pm.ParamValue);
                pvVgVoltage8.fineNumeric.Value = int.Parse(pm.ParamValue);
            }
            //if (pvVgEnabled.ButtonState && map.ContainsKey("pvVgRealVoltage9"))
            if (map.ContainsKey("pvVgRealVoltage9"))
            {
                pm = map["pvVgRealVoltage9"];
                pvVgVoltage9.Value = int.Parse(pm.ParamValue);
                pvVgVoltage9.fineNumeric.Value = int.Parse(pm.ParamValue);
            }
            //if (pvVgEnabled.ButtonState && map.ContainsKey("pvVgRealVoltage10"))
            if (map.ContainsKey("pvVgRealVoltage10"))
            {
                pm = map["pvVgRealVoltage10"];
                pvVgVoltage10.Value = int.Parse(pm.ParamValue);
                pvVgVoltage10.fineNumeric.Value = int.Parse(pm.ParamValue);
            }
            //if (pvVgEnabled.ButtonState && map.ContainsKey("pvVgRealVoltage11"))
            if (map.ContainsKey("pvVgRealVoltage11"))
            {
                pm = map["pvVgRealVoltage11"];
                pvVgVoltage11.Value = int.Parse(pm.ParamValue);
                pvVgVoltage11.fineNumeric.Value = int.Parse(pm.ParamValue);
            }
            //if (pvVgEnabled.ButtonState && map.ContainsKey("pvVgRealVoltage12"))
            if (map.ContainsKey("pvVgRealVoltage12"))
            {
                pm = map["pvVgRealVoltage12"];
                pvVgVoltage12.Value = int.Parse(pm.ParamValue);
                pvVgVoltage12.fineNumeric.Value = int.Parse(pm.ParamValue);
            }

            //衰减补偿
            if (map.ContainsKey("pvAttCompensation"))
            {
                pm = map["pvAttCompensation"];
                pvAttCompensation.Text = pm.ParamValue;
                ShowAttTable(pvAttCompensation, toList(pm.ParamValue, 3));
            }
            //功率标定
            if (map.ContainsKey("pvInputPowerAd"))
            {
                pm = map["pvInputPowerAd"];
                ShowTable(pvInputPowerAd, toList(pm.ParamValue, 2));
            }
            if (map.ContainsKey("pvInputPowerAdValue"))
            {
                pm = map["pvInputPowerAdValue"];
                DataTable dt = (DataTable)pvInputPowerAd.DataSource;
                dt.Rows[currentEditRowIndex][0] = pm.ParamValue;
                dt.Rows[currentEditRowIndex][1] = currentEditCellValue;
            }
            if (map.ContainsKey("pvOutputPowerAd"))
            {
                pm = map["pvOutputPowerAd"];
                ShowTable(pvOutputPowerAd, toList(pm.ParamValue, 2));
            }
            if (map.ContainsKey("pvOutputPowerAdValue"))
            {
                pm = map["pvOutputPowerAdValue"];
                DataTable dt = (DataTable)pvOutputPowerAd.DataSource;
                dt.Rows[currentEditRowIndex][0] = pm.ParamValue;
                dt.Rows[currentEditRowIndex][1] = currentEditCellValue;
            }
            if (map.ContainsKey("pvIncidentPowerAd"))
            {
                pm = map["pvIncidentPowerAd"];
                ShowTable(pvIncidentPowerAd, toList(pm.ParamValue, 2));
            }
            if (map.ContainsKey("pvIncidentPowerAdValue"))
            {
                pm = map["pvIncidentPowerAdValue"];
                DataTable dt = (DataTable)pvIncidentPowerAd.DataSource;
                dt.Rows[currentEditRowIndex][0] = pm.ParamValue;
                dt.Rows[currentEditRowIndex][1] = currentEditCellValue;
            }
            if (map.ContainsKey("pvAlcPowerVoltage"))
            {
                pm = map["pvAlcPowerVoltage"];
                ShowTable(pvAlcPowerVoltage, toList(pm.ParamValue, 2));
            }
            if (map.ContainsKey("pvAlcPowerVoltageValue"))
            {
                pm = map["pvAlcPowerVoltageValue"];
                DataTable dt = (DataTable)pvAlcPowerVoltage.DataSource;
                dt.Rows[currentEditRowIndex][0] = pm.ParamValue;
                dt.Rows[currentEditRowIndex][1] = currentEditCellValue;
            }
            //告警
            if (map.ContainsKey("pvPowerModuleAlarm"))
            {
                pm = map["pvPowerModuleAlarm"];
                if (pm.ParamValue == "0")
                    pvPowerModuleAlarm.ButtonColor = Color.Green;
                else
                    pvPowerModuleAlarm.ButtonColor = Color.Red;
            }
            if (map.ContainsKey("pvOverPowerAlarm"))
            {
                pm = map["pvOverPowerAlarm"];
                if (pm.ParamValue == "0")
                    pvOverPowerAlarm.ButtonColor = Color.Green;
                else
                    pvOverPowerAlarm.ButtonColor = Color.Red;
            }
            if (map.ContainsKey("pvVSWRAlarm"))
            {
                pm = map["pvVSWRAlarm"];
                if (pm.ParamValue == "0")
                    pvVSWRAlarm.ButtonColor = Color.Green;
                else
                    pvVSWRAlarm.ButtonColor = Color.Red;
            }
            if (map.ContainsKey("pvOverTemperatureAlarm"))
            {
                pm = map["pvOverTemperatureAlarm"];
                if (pm.ParamValue == "0")
                {
                    pvOverTemperatureAlarm.ButtonColor = Color.Green;
                }
                else
                {
                    pvOverTemperatureAlarm.ButtonColor = Color.Red;
                }
            }

        }

        private List<string[]> toList(string data, int length)
        {
            List<string[]> result = new List<string[]>();
            string[] list = data.Split(' ');
            for (int i = 0; i < list.Length; i++)
            {
                string[] item = list[i].Split(',');
                result.Add(item);
            }
            return result;
        }

        private bool crc16(byte[] real)
        {
            //起始位（1）+命令头（6字节）+命令体（多字节）+校验和（2字节）+结束位（1）
            List<byte> ls = new List<byte>(real.Length);
            ls.AddRange(real);
            byte[] data = new byte[real.Length - 4];//命令体
            byte[] chksum = new byte[2];//校验和
            ls.RemoveRange(0, 1);
            ls.CopyTo(0, data, 0, data.Length);//取出命令体
            ls.RemoveRange(0, data.Length);
            ls.CopyTo(0, chksum, 0, 2);//取出校验和

            CRC c = new CRC(InitialCrcValue.Zeros);
            byte[] res = c.ComputeChecksumBytes(data);//算出校验和

            if (CRC.ByteArrayToHexString(res) == CRC.ByteArrayToHexString(chksum))
            {
                return true;
            }
            return false;
        }

        private byte[] escapeCode(byte[] b)
        {
            List<byte> ls = new List<byte>(b.Length);
            ls.AddRange(b);
            byte[] data = new byte[b.Length - 2];//去掉起始位和结束位的包
            ls.RemoveRange(0, 1);
            ls.CopyTo(0, data, 0, data.Length);//取出包
            ls.Clear();

            string l = "";
            string h = "";
            for (int i = 0; i < data.Length; i++)
            {
                l = data[i].ToString("X2");
                if (l == "5E")
                {
                    if ((i + 1) >= data.Length)
                    {
                        ls.Add(data[i]);
                        continue;
                    }
                    h = data[i + 1].ToString("X2");
                    if (h == "4D")
                    {
                        ls.Add((int)0x4E);
                        i++;
                    }
                    else if (h == "5D")
                    {
                        ls.Add((int)0x5E);
                        i++;
                    }
                    else
                    {
                        ls.Add(data[i]);
                    }
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

        private void changeTrackBarState(MyTrackBar myTrackBar, bool flag)
        {
            //myTrackBar.fineNumeric.Enabled = flag;
            //myTrackBar.coarseTrackBar.Enabled = flag;
            //myTrackBar.saveButton.Enabled = flag;
            if (flag)
            {
                myTrackBar.SwitchState = "ON";
            }
            else
            {
                myTrackBar.Value = 0;
                myTrackBar.SwitchState = "OFF";
            }
        }

        private void changeTrackBarEnabled(MyTrackBar myTrackBar, bool flag)
        {
            myTrackBar.fineNumeric.Enabled = flag;
            myTrackBar.coarseTrackBar.Enabled = flag;
            myTrackBar.saveButton.Enabled = flag;
            myTrackBar.switchButton.Enabled = flag;
            if (myTrackBar.switchButton.Text == "OFF")
            {
                //myTrackBar.coarseTrackBar.BackColor = Color.FromArgb(153, 180, 209); // Blue
                myTrackBar.coarseTrackBar.Enabled = false;
            }
            else
            {
                myTrackBar.coarseTrackBar.BackColor = flag ? Color.FromArgb(105, 232, 120) : Color.FromArgb(153, 180, 209);
                myTrackBar.coarseTrackBar.Enabled = flag;
            }
            if (!flag)
            {
                myTrackBar.SwitchState = "ON";
            }
        }

        private void alcGainControl()
        {
            changeTrackBarEnabled(pvGainVoltage1, !pvAlcGainEnabled.ButtonState);
            changeTrackBarEnabled(pvAlcVoltage, !pvAlcGainEnabled.ButtonState);
            changeTrackBarEnabled(pvAlcVoltage1, !pvAlcGainEnabled.ButtonState);
        }

        private void vgControl()
        {
            changeTrackBarEnabled(pvVgVoltage1, !pvVgEnabled.ButtonState);
            changeTrackBarEnabled(pvVgVoltage2, !pvVgEnabled.ButtonState);
            changeTrackBarEnabled(pvVgVoltage3, !pvVgEnabled.ButtonState);
            changeTrackBarEnabled(pvVgVoltage4, !pvVgEnabled.ButtonState);
            changeTrackBarEnabled(pvVgVoltage5, !pvVgEnabled.ButtonState);
            changeTrackBarEnabled(pvVgVoltage6, !pvVgEnabled.ButtonState);
            changeTrackBarEnabled(pvVgVoltage7, !pvVgEnabled.ButtonState);
            changeTrackBarEnabled(pvVgVoltage8, !pvVgEnabled.ButtonState);
            changeTrackBarEnabled(pvVgVoltage9, !pvVgEnabled.ButtonState);
            changeTrackBarEnabled(pvVgVoltage10, !pvVgEnabled.ButtonState);
            changeTrackBarEnabled(pvVgVoltage11, !pvVgEnabled.ButtonState);
            changeTrackBarEnabled(pvVgVoltage12, !pvVgEnabled.ButtonState);
        }

        private void btnConn_Click(object sender, EventArgs e)
        {
            if (!onlineTimer.Enabled)
            {
                onlineTimer.Start();
            }
            changeUnitState(lbLedUnitState.ButtonColor == Color.Red);
            if (connected && pollingTimer.Enabled)
            {
                pollingTimer.Stop();
            }
            else
            {
                pollingTimer.Start();
                List<ParamItem> paramList = new List<ParamItem>();
                ParamItem pm = null;
                pm = new ParamItem("8045", "pvSystemThermometer", 1, "0", 0, "sint1", "℃");
                paramList.Add(pm);

                serialQuery(paramList);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txGet.Text = "";
        }

        private void queryBaseParams()
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("F001", "pvModuleType", 20, "0", 0, "string", "");
            paramList.Add(pm);
            pm = new ParamItem("F002", "pvModuleAddr", 2, "0", 0, "uint2", "");
            paramList.Add(pm);
            pm = new ParamItem("F003", "pvProtocolVersion", 2, "0", 0, "uint2", "");
            paramList.Add(pm);
            pm = new ParamItem("F018", "pvSoftwareVersion", 2, "0", 0, "uint2", "");
            paramList.Add(pm);
            pm = new ParamItem("F019", "pvProductSerial", 20, "0", 0, "string", "");
            paramList.Add(pm);

            serialQueryBase(paramList);
        }

        //轮询
        private void queryUnitParams()
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("8045", "pvSystemThermometer", 1, "0", 0, "sint1", "℃");
            paramList.Add(pm);
            pm = new ParamItem("80BF", "pvInputPower", 2, "0", 10, "sint2", "dBm");
            paramList.Add(pm);
            pm = new ParamItem("8004", "pvOutputPower", 2, "0", 10, "sint2", "dBm");
            paramList.Add(pm);
            pm = new ParamItem("800C", "pvIncidentPower", 2, "0", 10, "sint2", "dBm");
            paramList.Add(pm);
            pm = new ParamItem("8120", "pvVSWR", 1, "0", 10, "uint1", "");
            paramList.Add(pm);
            pm = new ParamItem("80C3", "pvInputPowerVoltage", 2, "0", 1000, "uint2", "V");
            paramList.Add(pm);
            pm = new ParamItem("8086", "pvOutputPowerVoltage", 2, "0", 1000, "uint2", "V");
            paramList.Add(pm);
            pm = new ParamItem("809E", "pvIncidentPowerVoltage", 2, "0", 1000, "uint2", "V");
            paramList.Add(pm);

            pm = new ParamItem("0024", "pvDigitalAtt", 1, "0", 2, "uint1", "dB");
            paramList.Add(pm);
            pm = new ParamItem("0018", "pvAlcControl", 2, "0", 10, "sint2", "dBm");
            paramList.Add(pm);
            pm = new ParamItem("0008", "pvPowerSwitch", 1, "0", 0, "uint1", "");
            paramList.Add(pm);


            //pm = new ParamItem("D120", "pvAlcVoltageSwitch", 1, "", 0, "uint1", "");
            //paramList.Add(pm);
            //pm = new ParamItem("D121", "pvGainVoltageSwitch", 1, "", 0, "uint1", "");
            //paramList.Add(pm);
            pm = new ParamItem("D123", "pvAlcGainEnabled", 1, "0", 0, "uint1", "");
            paramList.Add(pm);
            if (pvAlcGainEnabled.ButtonState)
            {
                if (pvAlcVoltage.SwitchState == "ON" || pvAlcVoltage1.SwitchState == "ON")
                {
                    pm = new ParamItem("D140", "pvAlcRealVoltage", 2, "", 0, "uint2", "");
                    paramList.Add(pm);
                }
                if (pvGainVoltage1.SwitchState == "ON")
                {
                    pm = new ParamItem("D141", "pvGainRealVoltage", 2, "", 0, "uint2", "");
                    paramList.Add(pm);
                }
            }

            pm = new ParamItem("D122", "pvVgEnabled", 1, "", 0, "uint1", "");
            paramList.Add(pm);
            /*pm = new ParamItem("D100", "pvVgSwitch1", 1, "", 0, "uint1", "");//OFF:0, ON:1
            paramList.Add(pm);
            pm = new ParamItem("D101", "pvVgSwitch2", 1, "", 0, "uint1", "");//OFF:0, ON:1
            paramList.Add(pm);
            pm = new ParamItem("D102", "pvVgSwitch3", 1, "", 0, "uint1", "");//OFF:0, ON:1
            paramList.Add(pm);
            pm = new ParamItem("D103", "pvVgSwitch4", 1, "", 0, "uint1", "");//OFF:0, ON:1
            paramList.Add(pm);
            pm = new ParamItem("D104", "pvVgSwitch5", 1, "", 0, "uint1", "");//OFF:0, ON:1
            paramList.Add(pm);
            pm = new ParamItem("D105", "pvVgSwitch6", 1, "", 0, "uint1", "");//OFF:0, ON:1
            paramList.Add(pm);
            pm = new ParamItem("D106", "pvVgSwitch7", 1, "", 0, "uint1", "");//OFF:0, ON:1
            paramList.Add(pm);
            pm = new ParamItem("D107", "pvVgSwitch8", 1, "", 0, "uint1", "");//OFF:0, ON:1
            paramList.Add(pm);
            pm = new ParamItem("D108", "pvVgSwitch9", 1, "", 0, "uint1", "");//OFF:0, ON:1
            paramList.Add(pm);
            pm = new ParamItem("D109", "pvVgSwitch10", 1, "", 0, "uint1", "");//OFF:0, ON:1
            paramList.Add(pm);
            pm = new ParamItem("D10A", "pvVgSwitch11", 1, "", 0, "uint1", "");//OFF:0, ON:1
            paramList.Add(pm);
            pm = new ParamItem("D10B", "pvVgSwitch12", 1, "", 0, "uint1", "");//OFF:0, ON:1
            paramList.Add(pm);*/
            if (pvVgEnabled.ButtonState)
            {
                if (pvVgVoltage1.SwitchState == "ON")
                {
                    pm = new ParamItem("D130", "pvVgRealVoltage1", 2, "", 0, "uint2", "");
                    paramList.Add(pm);
                }
                if (pvVgVoltage2.SwitchState == "ON")
                {
                    pm = new ParamItem("D131", "pvVgRealVoltage2", 2, "", 0, "uint2", "");
                    paramList.Add(pm);
                }
                if (pvVgVoltage3.SwitchState == "ON")
                {
                    pm = new ParamItem("D132", "pvVgRealVoltage3", 2, "", 0, "uint2", "");
                    paramList.Add(pm);
                }
                if (pvVgVoltage4.SwitchState == "ON")
                {
                    pm = new ParamItem("D133", "pvVgRealVoltage4", 2, "", 0, "uint2", "");
                    paramList.Add(pm);
                }
                if (pvVgVoltage5.SwitchState == "ON")
                {
                    pm = new ParamItem("D134", "pvVgRealVoltage5", 2, "", 0, "uint2", "");
                    paramList.Add(pm);
                }
                if (pvVgVoltage6.SwitchState == "ON")
                {
                    pm = new ParamItem("D135", "pvVgRealVoltage6", 2, "", 0, "uint2", "");
                    paramList.Add(pm);
                }
                if (pvVgVoltage7.SwitchState == "ON")
                {
                    pm = new ParamItem("D136", "pvVgRealVoltage7", 2, "", 0, "uint2", "");
                    paramList.Add(pm);
                }
                if (pvVgVoltage8.SwitchState == "ON")
                {
                    pm = new ParamItem("D137", "pvVgRealVoltage8", 2, "", 0, "uint2", "");
                    paramList.Add(pm);
                }
                if (pvVgVoltage9.SwitchState == "ON")
                {
                    pm = new ParamItem("D138", "pvVgRealVoltage9", 2, "", 0, "uint2", "");
                    paramList.Add(pm);
                }
                if (pvVgVoltage10.SwitchState == "ON")
                {
                    pm = new ParamItem("D139", "pvVgRealVoltage10", 2, "", 0, "uint2", "");
                    paramList.Add(pm);
                }
                if (pvVgVoltage11.SwitchState == "ON")
                {
                    pm = new ParamItem("D13A", "pvVgRealVoltage11", 2, "", 0, "uint2", "");
                    paramList.Add(pm);
                }
                if (pvVgVoltage12.SwitchState == "ON")
                {
                    pm = new ParamItem("D13B", "pvVgRealVoltage12", 2, "", 0, "uint2", "");
                    paramList.Add(pm);
                }
            }
            pm = new ParamItem("8028", "pvPowerModuleAlarm", 1, "", 0, "uint1", "");
            paramList.Add(pm);
            pm = new ParamItem("801C", "pvOverPowerAlarm", 1, "", 0, "uint1", "");
            paramList.Add(pm);
            pm = new ParamItem("80C8", "pvVSWRAlarm", 1, "", 0, "uint1", "");
            paramList.Add(pm);
            pm = new ParamItem("81F0", "pvOverTemperatureAlarm", 1, "", 0, "uint1", "");
            paramList.Add(pm);

            serialQuery(paramList);
        }

        /// <summary>  
        /// 获取时间戳  
        /// </summary>  
        /// <returns></returns>  
        public static string GetTimeStamp()  
        {  
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);  
            return Convert.ToInt64(ts.TotalMilliseconds).ToString();  
        } 

        private void serialSend(Packet packet, List<ParamItem> paramList)
        {
            try
            {
                byte[] data = packet.pack(paramList);
                if (data == null) return;

                if (comm.IsOpen)
                {
                    comm.Write(data, 0, data.Length);
                    logger("发送: " + toHex(data));
                }
                else
                {
                    if (pollingTimer.Enabled) pollingTimer.Stop();
                }
            }
            catch (Exception e1)
            {
                this.Invoke((EventHandler)(delegate
                {
                    pollingTimer.Enabled = false;
                }));
                MessageBox.Show("发送数据失败: " + e1.Message);
            }
        }

        private void serialQuery(List<ParamItem> paramList)
        {
            Packet packet = new QueryPacket();
            serialSend(packet, paramList);
        }

        private void serialQueryBase(List<ParamItem> paramList)
        {
            Packet packet = new QueryBasePacket();
            serialSend(packet, paramList);
        }

        private void serialSet(List<ParamItem> paramList)
        {
            //设置参数时，先停掉当前的轮询
            bool flag = pollingTimer.Enabled;
            if (flag) pollingTimer.Stop();

            Packet packet = new SetPacket();
            serialSend(packet, paramList);

            if (flag)
            {
                delayOneSeconds();
                pollingTimer.Start();
            }
        }

        public void serialSetBase(List<ParamItem> paramList)
        {
            //设置参数时，先停掉当前的轮询
            bool flag = pollingTimer.Enabled;
            if (flag) pollingTimer.Stop();

            Packet packet = new SetBasePacket();
            serialSend(packet, paramList);

            if (flag)
            {
                delayOneSeconds();
                pollingTimer.Start();
            }
        }

        private void serialSpecSet(List<ParamItem> paramList)
        {
            if (pollingTimer.Enabled) pollingTimer.Stop();

            Packet packet = new SpecSetPacket();
            serialSend(packet, paramList);

            delayOneSeconds();
            pollingTimer.Start();
        }

        private void toolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void btnModuleType_Click(object sender, EventArgs e)
        {
            ParamSetForm psf = new ParamSetForm();
            psf.type = "string";
            psf.unit = "";
            psf.scale = 0;
            psf.ShowDialog();
            if (psf.value == null) return;

            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("F001", "pvModuleType", 20, psf.value, 0, "string", "");
            paramList.Add(pm);
            serialSetBase(paramList);
            logger("设置模块类型: " + psf.value);
        }

        private void btnModuleAddr_Click(object sender, EventArgs e)
        {
            ParamSetForm psf = new ParamSetForm();
            psf.type = "int";
            psf.unit = "";
            psf.scale = 0;
            psf.maximum = 16;
            psf.minimum = 0;
            psf.Owner = this;
            psf.paramId = "F002";
            psf.ShowDialog();
            if (psf.value == null) return;

            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            byte[] data = new byte[2];//特殊处理(高位在前，低位在后)
            data[0] = 8;
            data[1] = byte.Parse(psf.value);
            int value = System.BitConverter.ToUInt16(data, 0);
            pm = new ParamItem("F002", "pvModuleAddr", 2, value.ToString(), 0, "uint2", "");
            paramList.Add(pm);
            serialSetBase(paramList);
            logger("设置模块地址: " + psf.value);
        }

        private void btnProductSerial_Click(object sender, EventArgs e)
        {
            ParamSetForm psf = new ParamSetForm();
            psf.type = "string";
            psf.unit = "";
            psf.scale = 0;
            psf.ShowDialog();
            if (psf.value == null) return;

            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("F019", "pvProductSerial", 20, psf.value, 0, "string", "");
            paramList.Add(pm);
            serialSetBase(paramList);
            logger("设置产品序列号: " + psf.value);
        }

        private void btnDigitalAtt_Click(object sender, EventArgs e)
        {
            ParamSetForm psf = new ParamSetForm();
            psf.type = "float";
            psf.unit = "dB";
            psf.scale = 2;
            psf.maximum = 31.5F;
            psf.minimum = 0;
            psf.Owner = this;
            psf.paramId = "0024";
            psf.ShowDialog();
            if (psf.value == null) return;

            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("0024", "pvDigitalAtt", 1, psf.value, 2, "uint1", "dB");
            paramList.Add(pm);
            serialSet(paramList);
            logger("设置ATT: " + psf.value);
        }

        private void btnAlcControl_Click(object sender, EventArgs e)
        {
            ParamSetForm psf = new ParamSetForm();
            psf.type = "float";
            psf.unit = "dBm";
            psf.scale = 10;
            psf.maximum = 32767;
            psf.minimum = -32768;
            psf.Owner = this;
            psf.paramId = "0018";
            psf.ShowDialog();
            if (psf.value == null) return;

            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("0018", "pvAlcControl", 2, psf.value, 10, "sint2", "dBm");
            paramList.Add(pm);

            serialSet(paramList);
            logger("设置ALC: " + psf.value);
        }

        private void pvPowerSwitch_Click(object sender, EventArgs e)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("0008", "pvPowerSwitch", 1, pvPowerSwitch.Text == "OFF" ? "1" : "0", 0, "uint1", "");
            paramList.Add(pm);

            serialSet(paramList);
            logger("功放开关: " + pvPowerSwitch.Text);
        }

        
        private void trackBarSet(MyTrackBar myTrackBar, bool flag)
        {
            myTrackBar.fineNumeric.Enabled = flag;
            myTrackBar.coarseTrackBar.Enabled = flag;
            myTrackBar.switchButton.Enabled = flag;
            myTrackBar.saveButton.Enabled = flag;
        }

        //ALC，Gain调节
        private void pvAlcGainEnabled_Click(object sender, EventArgs e)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D123", "pvAlcGainEnabled", 1, (pvAlcGainEnabled.ButtonState ? "1" : "0"), 0, "uint1", "mV");
            paramList.Add(pm);

            serialSet(paramList);
            logger("ALC,GAIN手工/自动使能: " + (pvAlcGainEnabled.ButtonState ? "手动" : "自动"));//点击按钮后自动改为手动
        }

        private void pvGainVoltage1_OnSwitchButton_Click(object sender)
        {
            pvGainVoltage1.SwitchState = (pvGainVoltage1.SwitchState == "ON" ? "OFF" : "ON");
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D121", "pvGainVoltageSwitch", 1, (pvGainVoltage1.SwitchState == "ON" ? "1" : "0"), 0, "uint1", "");
            paramList.Add(pm);

            serialSet(paramList);
            logger("增益电压开关: " + pvGainVoltage1.SwitchState);

            changeTrackBarState(pvGainVoltage1, (pvGainVoltage1.SwitchState == "OFF" ? false : true));
            showRealValue(pvGainVoltage1.SwitchState, new ParamItem("D141", "pvGainRealVoltage", 2, "", 0, "uint2", ""));
        }
        private void pvAlcVoltage_OnSwitchButton_Click(object sender)
        {
            pvAlcVoltage.SwitchState = (pvAlcVoltage.SwitchState == "ON" ? "OFF" : "ON");
            pvAlcVoltage1.SwitchState = (pvAlcVoltage1.SwitchState == "ON" ? "OFF" : "ON");
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D120", "pvAlcVoltageSwitch", 1, (pvAlcVoltage.SwitchState == "ON" ? "1" : "0"), 0, "uint1", "");
            paramList.Add(pm);

            serialSet(paramList);
            logger("ALC电压开关: " + pvAlcVoltage.SwitchState);
            changeTrackBarState(pvAlcVoltage, (pvAlcVoltage.SwitchState == "OFF" ? false : true));
            changeTrackBarState(pvAlcVoltage1, (pvAlcVoltage1.SwitchState == "OFF" ? false : true));
            showRealValue(pvAlcVoltage.SwitchState, new ParamItem("D140", "pvAlcRealVoltage", 2, "", 0, "uint2", ""));
        }

        private void pvGainVoltage1_OnSaveButton_Click(object sender)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("DF30", "pvGainVoltageTemp", 2, pvGainVoltage1.fineNumeric.Value.ToString(), 0, "uint2", "mV");
            paramList.Add(pm);

            serialSet(paramList);
            logger("增益电压值与温度绑定: " + pvGainVoltage1.fineNumeric.Value);
        }

        private void pvAlcVoltage_OnSaveButton_Click(object sender)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("DF20", "pvAlcVoltageTemp", 2, pvAlcVoltage.fineNumeric.Value.ToString(), 0, "uint2", "mV");
            paramList.Add(pm);

            serialSet(paramList);
            logger("ALC电压值与温度绑定: " + pvAlcVoltage.fineNumeric.Value);
        }

        private void setGainVoltage()
        {
            this.Invoke((EventHandler)(delegate
                {
                    if (pvGainVoltage1.SwitchState == "OFF") return;

                    List<ParamItem> paramList = new List<ParamItem>();
                    ParamItem pm = null;
                    pm = new ParamItem("DE30", "pvGainVoltage", 2, pvGainVoltage1.Value.ToString(), 0, "uint2", "mV");
                    paramList.Add(pm);

                    serialSpecSet(paramList);
                    logger("增益调节: " + pvGainVoltage1.Value);
                }));
        }

        private void setAlcVoltage()
        {
            this.Invoke((EventHandler)(delegate
                {
                    if (pvAlcVoltage.SwitchState == "OFF") return;

                    List<ParamItem> paramList = new List<ParamItem>();
                    ParamItem pm = null;
                    pm = new ParamItem("DE20", "pvAlcVoltage", 2, pvAlcVoltage.Value.ToString(), 0, "uint2", "mV");
                    paramList.Add(pm);

                    serialSpecSet(paramList);
                    logger("ALC调节: " + pvAlcVoltage.Value);
                }));
        }

        private void pvGainVoltage1_OnTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            setGainVoltage();
        }

        private void pvAlcVoltage_OnTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            setAlcVoltage();
        }

        private bool pvGainVoltage1NumericEnabled = false;
        private void pvGainVoltage1_OnNumericUpDown_MouseUp(object sender, MouseEventArgs e)
        {
            if (delayTimer.Enabled)
            {
                pvGainVoltage1NumericEnabled = false;
                delayTimer.Stop();
            }
            pvGainVoltage1NumericEnabled = true;
            delayTimer.Start();
            
        }

        private bool pvAlcVoltageNumericEnabled = false;
        private void pvAlcVoltage_OnNumericUpDown_MouseUp(object sender, MouseEventArgs e)
        {
            if (delayTimer.Enabled)
            {
                pvAlcVoltageNumericEnabled = false;
                delayTimer.Stop();
            }
            pvAlcVoltageNumericEnabled = true;
            delayTimer.Start();
        }



        //栅压调节
        private void pvVgEnabled_Click(object sender, EventArgs e)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D122", "pvVgEnabled", 1, (pvVgEnabled.ButtonState ? "1" : "0"), 0, "uint1", "mV");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压手工/自动使能: " + (pvVgEnabled.ButtonState ? "手动" : "自动"));
        }

        private void pvVgVoltage1_OnSwitchButton_Click(object sender)
        {
            pvVgVoltage1.SwitchState = (pvVgVoltage1.SwitchState == "ON" ? "OFF" : "ON");
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D100", "pvVgSwitch1", 1, (pvVgVoltage1.SwitchState == "ON" ? "1" : "0"), 0, "uint1", "");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压1开关: " + pvVgVoltage1.SwitchState);
        }

        private void pvVgVoltage2_OnSwitchButton_Click(object sender)
        {
            pvVgVoltage2.SwitchState = (pvVgVoltage2.SwitchState == "ON" ? "OFF" : "ON");
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D101", "pvVgSwitch2", 1, (pvVgVoltage2.SwitchState == "ON" ? "1" : "0"), 0, "uint1", "");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压2开关: " + pvVgVoltage2.SwitchState);
        }

        private void pvVgVoltage3_OnSwitchButton_Click(object sender)
        {
            pvVgVoltage3.SwitchState = (pvVgVoltage3.SwitchState == "ON" ? "OFF" : "ON");
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D102", "pvVgSwitch3", 1, (pvVgVoltage3.SwitchState == "ON" ? "1" : "0"), 0, "uint1", "");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压3开关: " + pvVgVoltage3.SwitchState);
        }

        private void pvVgVoltage4_OnSwitchButton_Click(object sender)
        {
            pvVgVoltage4.SwitchState = (pvVgVoltage4.SwitchState == "ON" ? "OFF" : "ON");
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D103", "pvVgSwitch4", 1, (pvVgVoltage4.SwitchState == "ON" ? "1" : "0"), 0, "uint1", "");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压4开关: " + pvVgVoltage4.SwitchState);
        }

        private void pvVgVoltage5_OnSwitchButton_Click(object sender)
        {
            pvVgVoltage5.SwitchState = (pvVgVoltage5.SwitchState == "ON" ? "OFF" : "ON");
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D104", "pvVgSwitch5", 1, (pvVgVoltage5.SwitchState == "ON" ? "1" : "0"), 0, "uint1", "");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压5开关: " + pvVgVoltage5.SwitchState);
        }

        private void pvVgVoltage6_OnSwitchButton_Click(object sender)
        {
            pvVgVoltage6.SwitchState = (pvVgVoltage6.SwitchState == "ON" ? "OFF" : "ON");
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D105", "pvVgSwitch6", 1, (pvVgVoltage6.SwitchState == "ON" ? "1" : "0"), 0, "uint1", "");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压6开关: " + pvVgVoltage6.SwitchState);
        }

        private void pvVgVoltage7_OnSwitchButton_Click(object sender)
        {
            pvVgVoltage7.SwitchState = (pvVgVoltage7.SwitchState == "ON" ? "OFF" : "ON");
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D106", "pvVgSwitch7", 1, (pvVgVoltage7.SwitchState == "ON" ? "1" : "0"), 0, "uint1", "");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压7开关: " + pvVgVoltage7.SwitchState);
        }

        private void pvVgVoltage8_OnSwitchButton_Click(object sender)
        {
            pvVgVoltage8.SwitchState = (pvVgVoltage8.SwitchState == "ON" ? "OFF" : "ON");
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D107", "pvVgSwitch8", 1, (pvVgVoltage8.SwitchState == "ON" ? "1" : "0"), 0, "uint1", "");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压8开关: " + pvVgVoltage8.SwitchState);
        }

        private void pvVgVoltage9_OnSwitchButton_Click(object sender)
        {
            pvVgVoltage9.SwitchState = (pvVgVoltage9.SwitchState == "ON" ? "OFF" : "ON");
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D108", "pvVgSwitch9", 1, (pvVgVoltage9.SwitchState == "ON" ? "1" : "0"), 0, "uint1", "");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压9开关: " + pvVgVoltage9.SwitchState);
        }

        private void pvVgVoltage10_OnSwitchButton_Click(object sender)
        {
            pvVgVoltage10.SwitchState = (pvVgVoltage10.SwitchState == "ON" ? "OFF" : "ON");
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D109", "pvVgSwitch10", 1, (pvVgVoltage10.SwitchState == "ON" ? "1" : "0"), 0, "uint1", "");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压10开关: " + pvVgVoltage10.SwitchState);
        }

        private void pvVgVoltage11_OnSwitchButton_Click(object sender)
        {
            pvVgVoltage11.SwitchState = (pvVgVoltage11.SwitchState == "ON" ? "OFF" : "ON");
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D10A", "pvVgSwitch11", 1, (pvVgVoltage11.SwitchState == "ON" ? "1" : "0"), 0, "uint1", "");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压11开关: " + pvVgVoltage11.SwitchState);
        }

        private void pvVgVoltage12_OnSwitchButton_Click(object sender)
        {
            pvVgVoltage12.SwitchState = (pvVgVoltage12.SwitchState == "ON" ? "OFF" : "ON");
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D10B", "pvVgSwitch12", 1, (pvVgVoltage12.SwitchState == "ON" ? "1" : "0"), 0, "uint1", "");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压12开关: " + pvVgVoltage12.SwitchState);
        }

        private void pvVgVoltage1_OnSaveButton_Click(object sender)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("DF10", "pvVgVoltageTemp1", 2, pvVgVoltage1.fineNumeric.Value.ToString(), 0, "uint2", "mV");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压1电压值与温度绑定: " + pvVgVoltage1.fineNumeric.Value);
        }

        private void pvVgVoltage2_OnSaveButton_Click(object sender)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("DF11", "pvVgVoltageTemp2", 2, pvVgVoltage2.fineNumeric.Value.ToString(), 0, "uint2", "mV");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压2电压值与温度绑定: " + pvVgVoltage2.fineNumeric.Value);
        }

        private void pvVgVoltage3_OnSaveButton_Click(object sender)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("DF12", "pvVgVoltageTemp3", 2, pvVgVoltage3.fineNumeric.Value.ToString(), 0, "uint2", "mV");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压3电压值与温度绑定: " + pvVgVoltage3.fineNumeric.Value);
        }

        private void pvVgVoltage4_OnSaveButton_Click(object sender)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("DF13", "pvVgVoltageTemp4", 2, pvVgVoltage4.fineNumeric.Value.ToString(), 0, "uint2", "mV");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压4电压值与温度绑定: " + pvVgVoltage4.fineNumeric.Value);
        }

        private void pvVgVoltage5_OnSaveButton_Click(object sender)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("DF14", "pvVgVoltageTemp5", 2, pvVgVoltage5.fineNumeric.Value.ToString(), 0, "uint2", "mV");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压5电压值与温度绑定: " + pvVgVoltage5.fineNumeric.Value);
        }

        private void pvVgVoltage6_OnSaveButton_Click(object sender)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("DF15", "pvVgVoltageTemp6", 2, pvVgVoltage6.fineNumeric.Value.ToString(), 0, "uint2", "mV");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压6电压值与温度绑定: " + pvVgVoltage6.fineNumeric.Value);
        }

        private void pvVgVoltage7_OnSaveButton_Click(object sender)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("DF16", "pvVgVoltageTemp7", 2, pvVgVoltage7.fineNumeric.Value.ToString(), 0, "uint2", "mV");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压7电压值与温度绑定: " + pvVgVoltage7.fineNumeric.Value);
        }

        private void pvVgVoltage8_OnSaveButton_Click(object sender)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("DF17", "pvVgVoltageTemp8", 2, pvVgVoltage8.fineNumeric.Value.ToString(), 0, "uint2", "mV");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压8电压值与温度绑定: " + pvVgVoltage8.fineNumeric.Value);
        }

        private void pvVgVoltage9_OnSaveButton_Click(object sender)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("DF18", "pvVgVoltageTemp9", 2, pvVgVoltage9.fineNumeric.Value.ToString(), 0, "uint2", "mV");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压9电压值与温度绑定: " + pvVgVoltage9.fineNumeric.Value);
        }

        private void pvVgVoltage10_OnSaveButton_Click(object sender)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("DF19", "pvVgVoltageTemp10", 2, pvVgVoltage10.fineNumeric.Value.ToString(), 0, "uint2", "mV");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压10电压值与温度绑定: " + pvVgVoltage10.fineNumeric.Value);
        }

        private void pvVgVoltage11_OnSaveButton_Click(object sender)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("DF1A", "pvVgVoltageTemp11", 2, pvVgVoltage11.fineNumeric.Value.ToString(), 0, "uint2", "mV");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压11电压值与温度绑定: " + pvVgVoltage11.fineNumeric.Value);
        }

        private void pvVgVoltage12_OnSaveButton_Click(object sender)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("DF1B", "pvVgVoltageTemp12", 2, pvVgVoltage12.fineNumeric.Value.ToString(), 0, "uint2", "mV");
            paramList.Add(pm);

            serialSet(paramList);
            logger("栅压12电压值与温度绑定: " + pvVgVoltage12.fineNumeric.Value);
        }

        #region 栅压调节
        private void setVgVoltage1()
        {
            this.Invoke((EventHandler)(delegate
                {
                    if (pvVgVoltage1.SwitchState == "OFF") return;

                    List<ParamItem> paramList = new List<ParamItem>();
                    ParamItem pm = null;
                    pm = new ParamItem("DE10", "pvVgVoltage1", 2, pvVgVoltage1.Value.ToString(), 0, "uint2", "mV");
                    paramList.Add(pm);

                    serialSpecSet(paramList);
                    logger("栅压1调节: " + pvVgVoltage1.Value);
                }));
        }

        private void setVgVoltage2()
        {
            this.Invoke((EventHandler)(delegate
                {
                    if (pvVgVoltage2.SwitchState == "OFF") return;

                    List<ParamItem> paramList = new List<ParamItem>();
                    ParamItem pm = null;
                    pm = new ParamItem("DE11", "pvVgVoltage2", 2, pvVgVoltage2.Value.ToString(), 0, "uint2", "mV");
                    paramList.Add(pm);

                    serialSpecSet(paramList);
                    logger("栅压2调节: " + pvVgVoltage2.Value);
                }));
        }

        private void setVgVoltage3()
        {
            this.Invoke((EventHandler)(delegate
                {
                    if (pvVgVoltage3.SwitchState == "OFF") return;

                    List<ParamItem> paramList = new List<ParamItem>();
                    ParamItem pm = null;
                    pm = new ParamItem("DE12", "pvVgVoltage3", 2, pvVgVoltage3.Value.ToString(), 0, "uint2", "mV");
                    paramList.Add(pm);

                    serialSpecSet(paramList);
                    logger("栅压3调节: " + pvVgVoltage3.Value);
                }));
        }

        private void setVgVoltage4()
        {
            this.Invoke((EventHandler)(delegate
                {
                    if (pvVgVoltage4.SwitchState == "OFF") return;

                    List<ParamItem> paramList = new List<ParamItem>();
                    ParamItem pm = null;
                    pm = new ParamItem("DE13", "pvVgVoltage4", 2, pvVgVoltage4.Value.ToString(), 0, "uint2", "mV");
                    paramList.Add(pm);

                    serialSpecSet(paramList);
                    logger("栅压4调节: " + pvVgVoltage4.Value);
                }));
        }

        private void setVgVoltage5()
        {
            this.Invoke((EventHandler)(delegate
                {
                    if (pvVgVoltage5.SwitchState == "OFF") return;

                    List<ParamItem> paramList = new List<ParamItem>();
                    ParamItem pm = null;
                    pm = new ParamItem("DE14", "pvVgVoltage5", 2, pvVgVoltage5.Value.ToString(), 0, "uint2", "mV");
                    paramList.Add(pm);

                    serialSpecSet(paramList);
                    logger("栅压5调节: " + pvVgVoltage5.Value);
                }));
        }

        private void setVgVoltage6()
        {
            this.Invoke((EventHandler)(delegate
                {
                    if (pvVgVoltage6.SwitchState == "OFF") return;

                    List<ParamItem> paramList = new List<ParamItem>();
                    ParamItem pm = null;
                    pm = new ParamItem("DE15", "pvVgVoltage6", 2, pvVgVoltage6.Value.ToString(), 0, "uint2", "mV");
                    paramList.Add(pm);

                    serialSpecSet(paramList);
                    logger("栅压6调节: " + pvVgVoltage6.Value);
                }));
        }

        private void setVgVoltage7()
        {
            this.Invoke((EventHandler)(delegate
                {
                    if (pvVgVoltage7.SwitchState == "OFF") return;

                    List<ParamItem> paramList = new List<ParamItem>();
                    ParamItem pm = null;
                    pm = new ParamItem("DE16", "pvVgVoltage7", 2, pvVgVoltage7.Value.ToString(), 0, "uint2", "mV");
                    paramList.Add(pm);

                    serialSpecSet(paramList);
                    logger("栅压7调节: " + pvVgVoltage7.Value);
                }));
        }

        private void setVgVoltage8()
        {
            this.Invoke((EventHandler)(delegate
                {
                    if (pvVgVoltage8.SwitchState == "OFF") return;

                    List<ParamItem> paramList = new List<ParamItem>();
                    ParamItem pm = null;
                    pm = new ParamItem("DE17", "pvVgVoltage8", 2, pvVgVoltage8.Value.ToString(), 0, "uint2", "mV");
                    paramList.Add(pm);

                    serialSpecSet(paramList);
                    logger("栅压8调节: " + pvVgVoltage8.Value);
                }));
        }

        private void setVgVoltage9()
        {
            this.Invoke((EventHandler)(delegate
                {
                    if (pvVgVoltage9.SwitchState == "OFF") return;

                    List<ParamItem> paramList = new List<ParamItem>();
                    ParamItem pm = null;
                    pm = new ParamItem("DE18", "pvVgVoltage9", 2, pvVgVoltage9.Value.ToString(), 0, "uint2", "mV");
                    paramList.Add(pm);

                    serialSpecSet(paramList);
                    logger("栅压9调节: " + pvVgVoltage9.Value);
                }));
        }

        private void setVgVoltage10()
        {
            this.Invoke((EventHandler)(delegate
                {
                    if (pvVgVoltage10.SwitchState == "OFF") return;

                    List<ParamItem> paramList = new List<ParamItem>();
                    ParamItem pm = null;
                    pm = new ParamItem("DE19", "pvVgVoltage10", 2, pvVgVoltage10.Value.ToString(), 0, "uint2", "mV");
                    paramList.Add(pm);

                    serialSpecSet(paramList);
                    logger("栅压10调节: " + pvVgVoltage10.Value);
                }));
        }

        private void setVgVoltage11()
        {
            this.Invoke((EventHandler)(delegate
                {
                    if (pvVgVoltage11.SwitchState == "OFF") return;

                    List<ParamItem> paramList = new List<ParamItem>();
                    ParamItem pm = null;
                    pm = new ParamItem("DE1A", "pvVgVoltage11", 2, pvVgVoltage11.Value.ToString(), 0, "uint2", "mV");
                    paramList.Add(pm);

                    serialSpecSet(paramList);
                    logger("栅压11调节: " + pvVgVoltage11.Value);
                }));
        }

        private void setVgVoltage12()
        {
            this.Invoke((EventHandler)(delegate
                {
                    if (pvVgVoltage12.SwitchState == "OFF") return;

                    List<ParamItem> paramList = new List<ParamItem>();
                    ParamItem pm = null;
                    pm = new ParamItem("DE1B", "pvVgVoltage12", 2, pvVgVoltage12.Value.ToString(), 0, "uint2", "mV");
                    paramList.Add(pm);

                    serialSpecSet(paramList);
                    logger("栅压12调节: " + pvVgVoltage12.Value);
                }));
        }
        #endregion

        private void pvVgVoltage1_OnTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            setVgVoltage1();
        }

        private void pvVgVoltage2_OnTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            setVgVoltage2();
        }

        private void pvVgVoltage3_OnTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            setVgVoltage3();
        }

        private void pvVgVoltage4_OnTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            setVgVoltage4();
        }

        private void pvVgVoltage5_OnTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            setVgVoltage5();
        }

        private void pvVgVoltage6_OnTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            setVgVoltage6();
        }

        private void pvVgVoltage7_OnTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            setVgVoltage7();
        }

        private void pvVgVoltage8_OnTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            setVgVoltage8();
        }

        private void pvVgVoltage9_OnTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            setVgVoltage9();
        }

        private void pvVgVoltage10_OnTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            setVgVoltage10();
        }

        private void pvVgVoltage11_OnTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            setVgVoltage11();
        }

        private void pvVgVoltage12_OnTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            setVgVoltage12();
        }

        private bool pvVgVoltage1NumericEnabled = false;
        private void pvVgVoltage1_OnNumericUpDown_MouseUp(object sender, MouseEventArgs e)
        {
            if (delayTimer.Enabled)
            {
                pvVgVoltage1NumericEnabled = false;
                delayTimer.Stop();
            }
            pvVgVoltage1NumericEnabled = true;
            delayTimer.Start();
        }

        private bool pvVgVoltage2NumericEnabled = false;
        private void pvVgVoltage2_OnNumericUpDown_MouseUp(object sender, MouseEventArgs e)
        {
            if (delayTimer.Enabled)
            {
                pvVgVoltage2NumericEnabled = false;
                delayTimer.Stop();
            }
            pvVgVoltage2NumericEnabled = true;
            delayTimer.Start();
        }

        private bool pvVgVoltage3NumericEnabled = false;
        private void pvVgVoltage3_OnNumericUpDown_MouseUp(object sender, MouseEventArgs e)
        {
            if (delayTimer.Enabled)
            {
                pvVgVoltage3NumericEnabled = false;
                delayTimer.Stop();
            }
            pvVgVoltage3NumericEnabled = true;
            delayTimer.Start();
        }

        private bool pvVgVoltage4NumericEnabled = false;
        private void pvVgVoltage4_OnNumericUpDown_MouseUp(object sender, MouseEventArgs e)
        {
            if (delayTimer.Enabled)
            {
                pvVgVoltage4NumericEnabled = false;
                delayTimer.Stop();
            }
            pvVgVoltage4NumericEnabled = true;
            delayTimer.Start();
        }

        private bool pvVgVoltage5NumericEnabled = false;
        private void pvVgVoltage5_OnNumericUpDown_MouseUp(object sender, MouseEventArgs e)
        {
            if (delayTimer.Enabled)
            {
                pvVgVoltage5NumericEnabled = false;
                delayTimer.Stop();
            }
            pvVgVoltage5NumericEnabled = true;
            delayTimer.Start();
        }

        private bool pvVgVoltage6NumericEnabled = false;
        private void pvVgVoltage6_OnNumericUpDown_MouseUp(object sender, MouseEventArgs e)
        {
            if (delayTimer.Enabled)
            {
                pvVgVoltage6NumericEnabled = false;
                delayTimer.Stop();
            }
            pvVgVoltage6NumericEnabled = true;
            delayTimer.Start();
        }

        private bool pvVgVoltage7NumericEnabled = false;
        private void pvVgVoltage7_OnNumericUpDown_MouseUp(object sender, MouseEventArgs e)
        {
            if (delayTimer.Enabled)
            {
                pvVgVoltage7NumericEnabled = false;
                delayTimer.Stop();
            }
            pvVgVoltage7NumericEnabled = true;
            delayTimer.Start();
        }

        private bool pvVgVoltage8NumericEnabled = false;
        private void pvVgVoltage8_OnNumericUpDown_MouseUp(object sender, MouseEventArgs e)
        {
            if (delayTimer.Enabled)
            {
                pvVgVoltage8NumericEnabled = false;
                delayTimer.Stop();
            }
            pvVgVoltage8NumericEnabled = true;
            delayTimer.Start();
        }

        private bool pvVgVoltage9NumericEnabled = false;
        private void pvVgVoltage9_OnNumericUpDown_MouseUp(object sender, MouseEventArgs e)
        {
            if (delayTimer.Enabled)
            {
                pvVgVoltage9NumericEnabled = false;
                delayTimer.Stop();
            }
            pvVgVoltage9NumericEnabled = true;
            delayTimer.Start();
        }

        private bool pvVgVoltage10NumericEnabled = false;
        private void pvVgVoltage10_OnNumericUpDown_MouseUp(object sender, MouseEventArgs e)
        {
            if (delayTimer.Enabled)
            {
                pvVgVoltage10NumericEnabled = false;
                delayTimer.Stop();
            }
            pvVgVoltage10NumericEnabled = true;
            delayTimer.Start();
        }

        private bool pvVgVoltage11NumericEnabled = false;
        private void pvVgVoltage11_OnNumericUpDown_MouseUp(object sender, MouseEventArgs e)
        {
            if (delayTimer.Enabled)
            {
                pvVgVoltage11NumericEnabled = false;
                delayTimer.Stop();
            }
            pvVgVoltage11NumericEnabled = true;
            delayTimer.Start();
        }

        private bool pvVgVoltage12NumericEnabled = false;
        private void pvVgVoltage12_OnNumericUpDown_MouseUp(object sender, MouseEventArgs e)
        {
            if (delayTimer.Enabled)
            {
                pvVgVoltage12NumericEnabled = false;
                delayTimer.Stop();
            }
            pvVgVoltage12NumericEnabled = true;
            delayTimer.Start();
        }

        #region 
        //test
        private void pvAttCompensation_Click(object sender, EventArgs e)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("DE41", "pvAttCompensation", 9, "0", 0, "table type:uint1+uint1+sint1*3;scale:0,0,2", "");
            paramList.Add(pm);
            serialQuery(paramList);
        }

        private void btnInputPowerAd_Click(object sender, EventArgs e)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("80BF", "pvInputPower", 2, "", 10, "sint2", "dBm");
            paramList.Add(pm);
            pm = new ParamItem("D00C", "pvInputPowerAd", 40, "0", 0, "table type:sint2+uint2*10;scale:10,0", "dBm");
            paramList.Add(pm);
            serialQuery(paramList);
        }

        private void btnOutputPowerAd_Click(object sender, EventArgs e)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("8004", "pvOutputPower", 2, "", 10, "sint2", "dBm");
            paramList.Add(pm);
            pm = new ParamItem("D004", "pvOutputPowerAd", 40, "0", 0, "table type:sint2+uint2*10;scale:10,0", "dBm");
            paramList.Add(pm);
            serialQuery(paramList);
        }

        private void btnIncidentPowerAd_Click(object sender, EventArgs e)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("800C", "pvIncidentPower", 2, "", 10, "sint2", "dBm");
            paramList.Add(pm);
            pm = new ParamItem("D010", "pvIncidentPowerAd", 40, "0", 0, "table type:sint2+uint2*10;scale:10,0", "dBm");
            paramList.Add(pm);
            serialQuery(paramList);
        }

        private void btnAlcPowerVoltage_Click(object sender, EventArgs e)
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("0018", "pvAlcControl", 2, "", 10, "sint2", "dBm");
            paramList.Add(pm);
            pm = new ParamItem("D030", "pvAlcPowerVoltage", 40, "0", 0, "table type:sint2+uint2*10;scale:10,0", "dBm");
            paramList.Add(pm);
            serialQuery(paramList);
        }

        private void inputPowerAdValue()
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D020", "pvInputPowerAdValue", 2, "0", 0, "uint2", "");
            paramList.Add(pm);
            serialQuery(paramList);
        }

        private void pvInputPowerAd_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'r')
            {
                DataGridView dgv = sender as DataGridView;
                DataGridViewCell cell = dgv.CurrentCell;
                currentEditRowIndex = cell.RowIndex;
                if (cell.IsInEditMode)
                {
                    if (cell.EditedFormattedValue != null)
                    {
                        currentEditCellValue = cell.EditedFormattedValue.ToString();
                        inputPowerAdValue();
                    }
                }
                else
                {
                    DataTable dt = (DataTable)pvInputPowerAd.DataSource;
                    currentEditCellValue = dt.Rows[currentEditRowIndex][1].ToString();
                    inputPowerAdValue();
                }
            }
        }

        private void outputPowerAdValue()
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D018", "pvOutputPowerAdValue", 2, "0", 0, "uint2", "");
            paramList.Add(pm);
            serialQuery(paramList);
        }

        private void pvOutputPowerAd_KeyPress(object sender, KeyPressEventArgs e)
        {
            //回车加载 
            if (e.KeyChar == 'r')
            {
                DataGridView dgv = sender as DataGridView;
                DataGridViewCell cell = dgv.CurrentCell;
                currentEditRowIndex = cell.RowIndex;
                //if (this.pvOutputPowerAd.CurrentCell.ColumnIndex == 0) return;
                if (cell.IsInEditMode)
                {
                    if (cell.EditedFormattedValue != null)
                    {
                        currentEditCellValue = cell.EditedFormattedValue.ToString();
                        outputPowerAdValue();
                    }
                }
                else
                {
                    DataTable dt = (DataTable)pvOutputPowerAd.DataSource;
                    currentEditCellValue = dt.Rows[currentEditRowIndex][1].ToString();
                    outputPowerAdValue();
                }
            }
        }

        private void incidentPowerAdValue()
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D024", "pvIncidentPowerAdValue", 2, "0", 0, "uint2", "");
            paramList.Add(pm);
            serialQuery(paramList);
        }

        private void pvIncidentPowerAd_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'r')
            {
                DataGridView dgv = sender as DataGridView;
                DataGridViewCell cell = dgv.CurrentCell;
                currentEditRowIndex = cell.RowIndex;
                if (cell.IsInEditMode)
                {
                    if (cell.EditedFormattedValue != null)
                    {
                        currentEditCellValue = cell.EditedFormattedValue.ToString();
                        incidentPowerAdValue();
                    }
                }
                else
                {
                    DataTable dt = (DataTable)pvIncidentPowerAd.DataSource;
                    currentEditCellValue = dt.Rows[currentEditRowIndex][1].ToString();
                    incidentPowerAdValue();
                }
            }
        }

        private void alcPowerVoltageValue()
        {
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D040", "pvAlcPowerVoltageValue", 2, "0", 0, "uint2", "");
            paramList.Add(pm);
            serialQuery(paramList);
        }

        private void pvAlcPowerVoltage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'r')
            {
                DataGridView dgv = sender as DataGridView;
                DataGridViewCell cell = dgv.CurrentCell;
                currentEditRowIndex = cell.RowIndex;
                if (cell.IsInEditMode)
                {
                    if (cell.EditedFormattedValue != null)
                    {
                        currentEditCellValue = cell.EditedFormattedValue.ToString();
                        alcPowerVoltageValue();
                    }
                }
                else
                {
                    DataTable dt = (DataTable)pvAlcPowerVoltage.DataSource;
                    currentEditCellValue = dt.Rows[currentEditRowIndex][1].ToString();
                    alcPowerVoltageValue();
                }
            }
        }
        #endregion

        #region 标定
        private string tableToString(DataTable dataTable, int length)
        {
            StringBuilder result = new StringBuilder();
            DataRowCollection drc = dataTable.Rows;
            for (int i = 0; i < drc.Count; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    if (length == 2)
                        result.Append(drc[i][length - 1 - j]);//功率标定时，设置值与表格列顺序相反
                    else
                        result.Append(drc[i][j]);
                    if(j < length - 1)
                        result.Append(",");
                }
                result.Append(" ");
            }
            result.Remove(result.Length - 1, 1);
            return result.ToString();
        }

        private void btnInputPowerSave_Click(object sender, EventArgs e)
        {
            string param = tableToString((DataTable)pvInputPowerAd.DataSource, 2);
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D00C", "pvInputPowerAd", 40, param, 0, "table type:sint2+uint2*10;scale:10,0", "dBm");
            paramList.Add(pm);
            serialSet(paramList);
            logger("输入功率标定: " + param);
        }

        private void btnOutputPowerSave_Click(object sender, EventArgs e)
        {
            string param = tableToString((DataTable)pvOutputPowerAd.DataSource, 2);
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D004", "pvOutputPowerAd", 40, param, 0, "table type:sint2+uint2*10;scale:10,0", "dBm");
            paramList.Add(pm);
            serialSet(paramList);
            logger("输出功率标定: " + param);
        }

        private void btnIncidentPowerSave_Click(object sender, EventArgs e)
        {
            string param = tableToString((DataTable)pvIncidentPowerAd.DataSource, 2);
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D010", "pvIncidentPowerAd", 40, param, 0, "table type:sint2+uint2*10;scale:10,0", "dBm");
            paramList.Add(pm);
            serialSet(paramList);
            logger("反射功率标定: " + param);
        }

        private void btnAlcPowerSave_Click(object sender, EventArgs e)
        {
            string param = tableToString((DataTable)pvAlcPowerVoltage.DataSource, 2);
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("D030", "pvAlcPowerVoltage", 40, param, 0, "table type:sint2+uint2*10;scale:10,0", "dBm");
            paramList.Add(pm);
            serialSet(paramList);
            logger("ALC功率标定: " + param);
        }

        private void btnAttCompensationSave_Click(object sender, EventArgs e)
        {
            string param = tableToString((DataTable)pvAttCompensation.DataSource, 3);
            List<ParamItem> paramList = new List<ParamItem>();
            ParamItem pm = null;
            pm = new ParamItem("DE41", "pvAttCompensation", 9, param, 0, "table type:uint1+uint1+sint1*3;scale:0,0,2", "");
            paramList.Add(pm);
            serialSet(paramList);
            logger("衰减补偿: " + param);
        }
        #endregion

        private void pvAlcVoltage_OnTrackBar_ValueChange(object sender)
        {
            pvAlcVoltage1.Value = pvAlcVoltage.Value;
        }

        private void pvAlcVoltage1_OnTrackBar_ValueChange(object sender)
        {
            pvAlcVoltage.Value = pvAlcVoltage1.Value;
        }

        #region Excel
        private void btnExportExcel_Click(object sender, EventArgs e)
        {
            List<DataTable> dts = new List<DataTable>();
            dts.Add((DataTable)pvInputPowerAd.DataSource);
            dts.Add((DataTable)pvOutputPowerAd.DataSource);
            dts.Add((DataTable)pvIncidentPowerAd.DataSource);
            dts.Add((DataTable)pvAlcPowerVoltage.DataSource);
            dts.Add((DataTable)pvAttCompensation.DataSource);
            //ExportExcel(dts);
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "导出Excel(*.xls)|*.xls|导出Excel(*.xlsx)|*.xlsx";
                sfd.FileName = "Backup_" + DateTime.Now.ToString("yyyyMMddhhmmss");
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    ExcelHelper.WriteExcel(dts, sfd.FileName);
                }
            }
            catch (Exception ex)
            {
                error("导出到Excel失败: " + ex.ToString());
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnImportExcel_Click(object sender, EventArgs e)
        {
            //ImportExcel2();

            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "导入 Microsoft Excel Document";
                dlg.Filter = "Microsoft Excel|*.xls|Microsoft Excel(*.xlsx)|*.xlsx";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    List<DataTable> dts = ExcelHelper.ImportExcelFile(dlg.FileName, pvInputPowerAd, pvOutputPowerAd, pvIncidentPowerAd, pvAlcPowerVoltage, pvAttCompensation);
                }
            }
            catch (Exception ex)
            {
                error("Excel导入失败: " + ex.ToString());
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SplitTable(DataTable dt, MyDataGridView myDataGridView, int columnIndex, int count)
        {
            try
            {
                //int rows = dt.Rows.Count;
                DataTable dataTable = (DataTable)myDataGridView.DataSource;
                if (count == 1)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        dataTable.Rows[j][1] = toFloat(dt.Rows[j + 1][columnIndex].ToString());
                    }
                }
                else if (count == 3)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        dataTable.Rows[j][0] = toFloat(dt.Rows[j + 1][columnIndex].ToString());
                        dataTable.Rows[j][1] = toFloat(dt.Rows[j + 1][columnIndex + 1].ToString());
                        dataTable.Rows[j][2] = toFloat(dt.Rows[j + 1][columnIndex + 2].ToString());
                    }
                }
                //myDataGridView.DataSource = dataTable;
            }
            catch (Exception ex)
            {
                error("Excel数据格式错误: " + ex.ToString());
                MessageBox.Show("Excel数据格式错误", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private float toFloat(object data)
        {
            if (data == null) return 0;
            float f = 0;
            try
            {
                f = float.Parse(data.ToString());
            }
            catch (Exception e)
            {
                f = 0;
            }
            return f;
        }

        private void pvInputPowerAd_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = false;
        }

        private void pvOutputPowerAd_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = false;
        }

        private void pvIncidentPowerAd_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = false;
        }

        private void pvAlcPowerVoltage_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = false;
        }

        private void pvAttCompensation_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = false;
        }
        #endregion

        #region 菜单
        private void menuUnitParam_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 0;
        }

        private void menuVg_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 1;
        }

        private void menuPowerSet_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 2;
        }

        private void menuAttCompensation_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 3;
        }

        private void menuLog_Click(object sender, EventArgs e)
        {
            if(!tabControl1.TabPages.Contains(loggerTabPage))
                tabControl1.TabPages.Add(loggerTabPage);
            tabControl1.SelectedIndex = 4;
        }

        private void menuAbout_Click(object sender, EventArgs e)
        {
            AboutForm af = new AboutForm();
            af.ShowDialog();
        }
        #endregion

        private void MainForm_MaximumSizeChanged(object sender, EventArgs e)
        {

        }

    }
}
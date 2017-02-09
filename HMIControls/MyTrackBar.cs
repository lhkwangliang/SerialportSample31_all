using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HMIControls
{
    public partial class MyTrackBar : UserControl
    {
        public GroupBox group;
        public TrackBar coarseTrackBar;
        public NumericUpDown fineNumeric;
        public Button switchButton;
        public Button saveButton;

        private ImageList imgList = new ImageList();

        public MyTrackBar()
        {
            InitializeComponent();
            group = groupBox1;
            coarseTrackBar = trackBar1;
            fineNumeric = numericUpDown1;
            switchButton = button2;
            saveButton = button3;

            
        }

        private void initGraduation()
        {
            int count = 10;
            int per = (Maximum - Minimum) / count;
            for (int i = 0; i < count + 1; i++)
            {
                Label lbl = new Label();
                lbl.Text = (Minimum + per * i).ToString();
                lbl.Width = 30;
                lbl.Height = 12;
                int x = i * GraduationWidth / count - lbl.Width / 2;
                if (x < 0)
                {
                    x = lbl.Width / 2;
                }
                else if (x > (GraduationWidth - lbl.Width))
                {
                    x = GraduationWidth - lbl.Width;
                }
                lbl.Location = new Point(x, 25);
                this.coarseTrackBar.Controls.Add(lbl);
            }
        }

        public string Title
        {
            get
            {
                return label1.Text;
            }
            set
            {
                label1.Text = value;
                this.DisplayTitle = this.DisplayTitle;
            }
        }

        private bool displayTitle = true;
        public bool DisplayTitle
        {
            get
            { return displayTitle; }
            set
            {
                if (value)
                    group.Text = "";
                else
                    group.Text = this.Title;
                label1.Visible = value;
            }
        }

        public int Maximum
        {
            get { return coarseTrackBar.Maximum; }
            set 
            {
                coarseTrackBar.Maximum = value;
                fineNumeric.Maximum = value;
            }
        }

        public int Minimum
        {
            get { return coarseTrackBar.Minimum; }
            set
            {
                coarseTrackBar.Minimum = value;
                fineNumeric.Minimum = value;
            }
        }

        public int Value
        {
            get { return coarseTrackBar.Value; }
            set
            {
                coarseTrackBar.Value = value;
                fineNumeric.Value = value;
            }
        }

        public string SwitchState
        {
            get { return button2.Text; }
            set
            { 
                button2.Text = value;
                button2.ImageIndex = (value == "OFF" ? 0 : 1);
            }
        }

        public int GraduationWidth
        {
            get { return coarseTrackBar.Width; }
            //    set { coarseTrackBar.Width = value; }
        }

        private void MyTrackBar_Paint(object sender, PaintEventArgs e)
        {
            initGraduation();
        }

        //声明事件委托    
        public delegate void ButtonClick(object sender);  
        public delegate void MouserDown(object sender, MouseEventArgs e);
        public delegate void MouseUp(object sender, MouseEventArgs e);
        public delegate void ValueChange(object sender);
        public delegate void NumericEnter(object sender);

        //定义事件
        public event ButtonClick OnSwitchButton_Click;
        private void button2_Click(object sender, EventArgs e)
        {
            if (OnSwitchButton_Click != null)
                OnSwitchButton_Click(this);
        }

        public event ButtonClick OnSaveButton_Click;
        private void button3_Click(object sender, EventArgs e)
        {
            if (OnSaveButton_Click != null)
                OnSaveButton_Click(this);
        }

        private void trackBar1_MouseDown(object sender, MouseEventArgs e)
        {
            int f = (e.X - 8) * 1000 / (this.coarseTrackBar.Width - 16);
            if (f > this.Minimum && f < this.Maximum)
                this.Value = f;
            else if (f > this.Maximum)
                this.Value = this.Maximum;
            else
                this.Value = this.Minimum;
        }

        public event ValueChange OnTrackBar_ValueChange;
        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            this.fineNumeric.Value = this.Value;
            if (OnTrackBar_ValueChange != null)
                OnTrackBar_ValueChange(this);
        }

        public event MouseUp OnTrackBar_MouseUp;
        private void trackBar1_MouseUp(object sender, MouseEventArgs e)
        {
            if (OnTrackBar_MouseUp != null)
                OnTrackBar_MouseUp(this, e);
        }

        public event MouseUp OnNumericUpDown_MouseUp;
        private void numericUpDown1_MouseUp(object sender, MouseEventArgs e)
        {
            if (OnNumericUpDown_MouseUp != null)
                OnNumericUpDown_MouseUp(this, e);
        }

        public event NumericEnter OnNumericUpDown_Enter;
        private void numericUpDown1_Enter(object sender, EventArgs e)
        {
            if (OnNumericUpDown_Enter != null)
                OnNumericUpDown_Enter(this);
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            this.Value = int.Parse(this.fineNumeric.Value.ToString());
        }
    }
}

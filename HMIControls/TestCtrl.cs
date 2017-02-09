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
    public partial class TestCtrl : UserControl
    {
        public TestCtrl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.UserPaint, true);

            InitializeComponent();
            this.Paint += new PaintEventHandler(TestCtrl_Paint);
        }

        // 水量
        private float volume = 100;
        [Category("水量"), Description("当前水量")]
        public float Volume
        {
            set { volume = value; }
            get { return volume; }
        }

        // 最高水量
        private float highVolume = 500;
        [Category("水量"), Description("最高水量")]
        public float HighVolume
        {
            set { highVolume = value; }
            get { return highVolume; }
        }

        // 最低水量
        private float lowVolume = 0;
        [Category("水量"), Description("最低水量")]
        public float LowVolume
        {
            set { lowVolume = value; }
            get { return lowVolume; }
        }

        // 当前温度数值的字体
        private Font tempFont = new Font("宋体", 12);
        [Category("温度"), Description("当前温度数值的字体")]
        public Font TempFont
        {
            set { tempFont = value; }
            get { return tempFont; }
        }

        // 温度柱背景颜色
        private Color mercuryBackColor = Color.LightBlue;
        [Category("刻度"), Description("温度柱背景颜色")]
        public Color MercuryBackColor
        {
            set { mercuryBackColor = value; }
            get { return mercuryBackColor; }
        }

        /// <summary>
        ///  变量
        /// </summary>
        private float X;
        private float Y;
        private float H;
        private Pen p, s_p;
        private Brush b;

        void TestCtrl_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            e.Graphics.TranslateTransform(2, 2);

            X = this.Width - 4;
            Y = this.Height - 4;

            // 绘制边框(最外边的框)
            Color dialOutLineColor = Color.DarkGray;
            p = new Pen(dialOutLineColor, 2);
            //e.Graphics.DrawLine(p, 0, X / 2, 0, (Y - X / 2));
            //e.Graphics.DrawLine(p, X, X / 2, X, (Y - X / 2));
            
            //e.Graphics.DrawArc(p, 0, 0, X, X, 180, 180);
            //e.Graphics.DrawArc(p, 0, (Y - X), X, X, 0, 180);

            e.Graphics.DrawLine(p, 0, X / 2, 0, Y);
            e.Graphics.DrawLine(p, X, X / 2, X, Y);
            e.Graphics.DrawLine(p, 0, Y, X, Y);
            e.Graphics.DrawArc(p, 0, 0, X, X, 180, 180);
            //e.Graphics.DrawArc(p, 0, (Y - X), X, X, 0, 180);

            

            // 在温度计底部,绘制当前温度数值
            Color tempColor = Color.Blue;
            b = new SolidBrush(tempColor);
            StringFormat format = new StringFormat();
            format.LineAlignment = StringAlignment.Center;
            format.Alignment = StringAlignment.Center;
            e.Graphics.DrawString((volume.ToString() + "L"), tempFont, b, X / 2, (Y - X / 4), format);

            // 绘制当前温度的位置
            float v = Y*(volume-lowVolume)/(highVolume-lowVolume);
            b = new SolidBrush(mercuryBackColor);
            e.Graphics.FillRectangle(b, 0, Y-v, X, v);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZedGraph;

namespace HMIControls
{
    public partial class AirMachine : UserControl
    {
        private bool isValveOn;
        private Timer timer;
        private double temperature;
        private Random random = new Random();

        private Point arrowLocation1;
        private Point arrowLocation2;
        private Point arrowLocation3;

        // Starting time in milliseconds
        int tickStart = 0;

        public AirMachine()
        {
            InitializeComponent();
            InitUI();
        }

        private void InitUI()
        {
            isValveOn = false;
            this.labelTemperature.Text = "0";
            this.button1.Text = "开";
            this.button1.BackColor = Color.Snow;
            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += new EventHandler(timer_Tick);
            this.Load += new EventHandler(AirMachine_Load);

            this.labelArrow1.Visible = false;
            this.labelArrow2.Visible = false;
            this.labelArrow3.Visible = false;

            arrowLocation1 = this.labelArrow1.Location;
            arrowLocation2 = this.labelArrow2.Location;
            arrowLocation3 = this.labelArrow3.Location;

            this.button1.Click += new EventHandler(button1_Click);
        }

        private void CreateGraph()
        {
            zedGraphControl1.IsEnableZoom = false;
            zedGraphControl1.IsShowContextMenu = false;

            // Get a reference to the GraphPane
            GraphPane myPane = zedGraphControl1.GraphPane;

            // Set the titles
            myPane.Title.Text = "实时数据";
            myPane.YAxis.Title.Text = "数据";
            myPane.XAxis.Title.Text = "时间";

            // Change the color of the title
            myPane.Title.FontSpec.FontColor = Color.Green;
            myPane.XAxis.Title.FontSpec.FontColor = Color.Green;
            myPane.YAxis.Title.FontSpec.FontColor = Color.Green;


            // Save 1200 points.  At 50 ms sample rate, this is one minute
            // The RollingPointPairList is an efficient storage class that always
            // keeps a rolling set of point data without needing to shift any data values
            RollingPointPairList list = new RollingPointPairList(1200);

            // Initially, a curve is added with no data points (list is empty)
            // Color is blue, and there will be no symbols
            LineItem myCurve = myPane.AddCurve("温度值", list, Color.Blue, SymbolType.None);

            // Fill the area under the curves
            myCurve.Line.Fill = new Fill(Color.White, Color.Blue, 45F);

            myCurve.Line.IsSmooth = true;
            myCurve.Line.SmoothTension = 0.5F;

            // Increase the symbol sizes, and fill them with solid white
            myCurve.Symbol.Size = 8.0F;
            myCurve.Symbol.Fill = new Fill(Color.Red);
            myCurve.Symbol.Type = SymbolType.Circle;

            // Just manually control the X axis range so it scrolls continuously
            // instead of discrete step-sized jumps
            myPane.XAxis.Scale.Min = 0;
            myPane.XAxis.Scale.Max = 100;
            myPane.XAxis.Scale.MinorStep = 1;
            myPane.XAxis.Scale.MajorStep = 5;

            // Add gridlines to the plot
            myPane.XAxis.MajorGrid.IsVisible = true;
            myPane.XAxis.MajorGrid.Color = Color.LightGray;
            myPane.YAxis.MajorGrid.IsVisible = true;
            myPane.YAxis.MajorGrid.Color = Color.LightGray;

            // Scale the axes
            zedGraphControl1.AxisChange();

            // Save the beginning time for reference
            tickStart = Environment.TickCount;
        }

        void AirMachine_Load(object sender, EventArgs e)
        {
            CreateGraph();
        }

        private void UpdateZedGraph(double yValue)
        {
            // Make sure that the curvelist has at least one curve
            if (zedGraphControl1.GraphPane.CurveList.Count <= 0)
                return;

            // Get the first CurveItem in the graph
            LineItem curve = zedGraphControl1.GraphPane.CurveList[0] as LineItem;
            if (curve == null)
                return;

            // Get the PointPairList
            IPointListEdit list = curve.Points as IPointListEdit;
            // If this is null, it means the reference at curve.Points does not
            // support IPointListEdit, so we won't be able to modify it
            if (list == null)
                return;

            // Time is measured in seconds
            double time = (Environment.TickCount - tickStart) / 1000.0;

            // 3 seconds per cycle
            //list.Add(time, Math.Sin(2.0 * Math.PI * time / 3.0));
            list.Add(time, yValue);

            // Keep the X scale at a rolling 30 second interval, with one
            // major step between the max X value and the end of the axis
            Scale xScale = zedGraphControl1.GraphPane.XAxis.Scale;
            if (time > xScale.Max - xScale.MajorStep)
            {
                xScale.Max = time + xScale.MajorStep;
                xScale.Min = xScale.Max - 100.0;
            }

            // Make sure the Y axis is rescaled to accommodate actual data
            zedGraphControl1.AxisChange();
            // Force a redraw
            zedGraphControl1.Invalidate();
        }

        private void UpdataArrowPosition()
        {
            this.labelArrow1.Location = new Point(this.labelArrow1.Location.X + 30, this.labelArrow1.Location.Y);
            if (this.labelArrow1.Location.X >= this.panelPic.Location.X + this.panelPic.Width)
            {
                this.labelArrow1.Location = arrowLocation1;
            }

            this.labelArrow2.Location = new Point(this.labelArrow2.Location.X + 30, this.labelArrow2.Location.Y);
            if (this.labelArrow2.Location.X >= this.panelPic.Location.X + this.panelPic.Width)
            {
                this.labelArrow2.Location = arrowLocation2;
            }

            this.labelArrow3.Location = new Point(this.labelArrow3.Location.X + 30, this.labelArrow3.Location.Y);
            if (this.labelArrow3.Location.X >= this.panelPic.Location.X + this.panelPic.Width)
            {
                this.labelArrow3.Location = arrowLocation3;
            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            temperature = random.NextDouble() * 100;
            this.labelTemperature.Text = Convert.ToInt32(temperature).ToString();

            UpdateZedGraph(temperature);

            UpdataArrowPosition();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            isValveOn = !isValveOn;
            if (isValveOn)
            {
                timer.Start();
                this.button1.Text = "关";
                this.button1.BackColor = Color.LawnGreen;
                this.labelTemperature.BackColor = Color.LawnGreen;
                this.labelArrow1.Visible = isValveOn;
                this.labelArrow2.Visible = isValveOn;
                this.labelArrow3.Visible = isValveOn;
            }
            else
            {
                timer.Stop();
                this.button1.Text = "开";
                this.button1.BackColor = Color.Snow;
                this.labelTemperature.Text = "0";
                this.labelTemperature.BackColor = Color.Snow;
                this.labelArrow1.Visible = isValveOn;
                this.labelArrow2.Visible = isValveOn;
                this.labelArrow3.Visible = isValveOn;
            }
        }
    }
}

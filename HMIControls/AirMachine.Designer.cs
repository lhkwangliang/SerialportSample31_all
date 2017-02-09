namespace HMIControls
{
    partial class AirMachine
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.panel2 = new System.Windows.Forms.Panel();
            this.zedGraphControl1 = new ZedGraph.ZedGraphControl();
            this.panelPic = new System.Windows.Forms.Panel();
            this.labelArrow3 = new System.Windows.Forms.Label();
            this.labelArrow2 = new System.Windows.Forms.Label();
            this.labelArrow1 = new System.Windows.Forms.Label();
            this.labelTemperature = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.panel2.SuspendLayout();
            this.panelPic.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.zedGraphControl1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 263);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(794, 251);
            this.panel2.TabIndex = 1;
            // 
            // zedGraphControl1
            // 
            this.zedGraphControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.zedGraphControl1.Location = new System.Drawing.Point(0, 0);
            this.zedGraphControl1.Name = "zedGraphControl1";
            this.zedGraphControl1.ScrollGrace = 0D;
            this.zedGraphControl1.ScrollMaxX = 0D;
            this.zedGraphControl1.ScrollMaxY = 0D;
            this.zedGraphControl1.ScrollMaxY2 = 0D;
            this.zedGraphControl1.ScrollMinX = 0D;
            this.zedGraphControl1.ScrollMinY = 0D;
            this.zedGraphControl1.ScrollMinY2 = 0D;
            this.zedGraphControl1.Size = new System.Drawing.Size(794, 251);
            this.zedGraphControl1.TabIndex = 0;
            // 
            // panelPic
            // 
            this.panelPic.BackgroundImage = global::HMIControls.Resource.airmachine_1;
            this.panelPic.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panelPic.Controls.Add(this.labelArrow3);
            this.panelPic.Controls.Add(this.labelArrow2);
            this.panelPic.Controls.Add(this.labelArrow1);
            this.panelPic.Controls.Add(this.labelTemperature);
            this.panelPic.Controls.Add(this.label2);
            this.panelPic.Controls.Add(this.label1);
            this.panelPic.Controls.Add(this.button1);
            this.panelPic.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelPic.Location = new System.Drawing.Point(0, 0);
            this.panelPic.Name = "panelPic";
            this.panelPic.Size = new System.Drawing.Size(794, 263);
            this.panelPic.TabIndex = 0;
            // 
            // labelArrow3
            // 
            this.labelArrow3.AutoSize = true;
            this.labelArrow3.BackColor = System.Drawing.Color.Transparent;
            this.labelArrow3.Image = global::HMIControls.Resource.arrow_right;
            this.labelArrow3.Location = new System.Drawing.Point(678, 118);
            this.labelArrow3.Name = "labelArrow3";
            this.labelArrow3.Size = new System.Drawing.Size(29, 12);
            this.labelArrow3.TabIndex = 6;
            this.labelArrow3.Text = "    ";
            // 
            // labelArrow2
            // 
            this.labelArrow2.AutoSize = true;
            this.labelArrow2.BackColor = System.Drawing.Color.Transparent;
            this.labelArrow2.Image = global::HMIControls.Resource.arrow_right;
            this.labelArrow2.Location = new System.Drawing.Point(678, 97);
            this.labelArrow2.Name = "labelArrow2";
            this.labelArrow2.Size = new System.Drawing.Size(29, 12);
            this.labelArrow2.TabIndex = 5;
            this.labelArrow2.Text = "    ";
            // 
            // labelArrow1
            // 
            this.labelArrow1.AutoSize = true;
            this.labelArrow1.BackColor = System.Drawing.Color.Transparent;
            this.labelArrow1.Image = global::HMIControls.Resource.arrow_right;
            this.labelArrow1.Location = new System.Drawing.Point(678, 78);
            this.labelArrow1.Name = "labelArrow1";
            this.labelArrow1.Size = new System.Drawing.Size(29, 12);
            this.labelArrow1.TabIndex = 4;
            this.labelArrow1.Text = "    ";
            // 
            // labelTemperature
            // 
            this.labelTemperature.AutoSize = true;
            this.labelTemperature.BackColor = System.Drawing.Color.Transparent;
            this.labelTemperature.Location = new System.Drawing.Point(657, 21);
            this.labelTemperature.Name = "labelTemperature";
            this.labelTemperature.Size = new System.Drawing.Size(41, 12);
            this.labelTemperature.TabIndex = 3;
            this.labelTemperature.Text = "label3";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Location = new System.Drawing.Point(598, 21);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "送风温度";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Location = new System.Drawing.Point(297, 236);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "阀门";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(332, 231);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(52, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // AirMachine
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panelPic);
            this.Name = "AirMachine";
            this.Size = new System.Drawing.Size(794, 514);
            this.panel2.ResumeLayout(false);
            this.panelPic.ResumeLayout(false);
            this.panelPic.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelPic;
        private System.Windows.Forms.Panel panel2;
        private ZedGraph.ZedGraphControl zedGraphControl1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelTemperature;
        private System.Windows.Forms.Label labelArrow1;
        private System.Windows.Forms.Label labelArrow3;
        private System.Windows.Forms.Label labelArrow2;
    }
}

namespace SerialportSample
{
    partial class ParamSetForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblParamUnit = new System.Windows.Forms.Label();
            this.pvParamValueNum = new System.Windows.Forms.NumericUpDown();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.pvParamValueStr = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pvParamValueNum)).BeginInit();
            this.SuspendLayout();
            // 
            // lblParamUnit
            // 
            this.lblParamUnit.AutoSize = true;
            this.lblParamUnit.Location = new System.Drawing.Point(142, 14);
            this.lblParamUnit.Name = "lblParamUnit";
            this.lblParamUnit.Size = new System.Drawing.Size(17, 12);
            this.lblParamUnit.TabIndex = 52;
            this.lblParamUnit.Text = "dB";
            // 
            // pvParamValueNum
            // 
            this.pvParamValueNum.DecimalPlaces = 1;
            this.pvParamValueNum.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.pvParamValueNum.Location = new System.Drawing.Point(37, 12);
            this.pvParamValueNum.Maximum = new decimal(new int[] {
            315,
            0,
            0,
            65536});
            this.pvParamValueNum.Name = "pvParamValueNum";
            this.pvParamValueNum.Size = new System.Drawing.Size(100, 21);
            this.pvParamValueNum.TabIndex = 51;
            this.pvParamValueNum.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.pvParamValueNum.ValueChanged += new System.EventHandler(this.pvParamValueNum_ValueChanged);
            this.pvParamValueNum.KeyUp += new System.Windows.Forms.KeyEventHandler(this.PressEnter_KeyUp);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(93, 56);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(79, 23);
            this.btnSave.TabIndex = 50;
            this.btnSave.Text = "设置";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(12, 56);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 53;
            this.btnCancel.Text = "关闭";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // pvParamValueStr
            // 
            this.pvParamValueStr.Location = new System.Drawing.Point(37, 29);
            this.pvParamValueStr.MaxLength = 20;
            this.pvParamValueStr.Name = "pvParamValueStr";
            this.pvParamValueStr.Size = new System.Drawing.Size(100, 21);
            this.pvParamValueStr.TabIndex = 56;
            this.pvParamValueStr.KeyUp += new System.Windows.Forms.KeyEventHandler(this.PressEnter_KeyUp);
            // 
            // ParamSetForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.LightGray;
            this.ClientSize = new System.Drawing.Size(184, 88);
            this.Controls.Add(this.pvParamValueStr);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lblParamUnit);
            this.Controls.Add(this.pvParamValueNum);
            this.Controls.Add(this.btnSave);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ParamSetForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "修改参数";
            this.Load += new System.EventHandler(this.ParamSetForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pvParamValueNum)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblParamUnit;
        private System.Windows.Forms.NumericUpDown pvParamValueNum;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox pvParamValueStr;
    }
}
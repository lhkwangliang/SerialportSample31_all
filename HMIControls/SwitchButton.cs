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
    public partial class SwitchButton : Button
    {
        public SwitchButton()
        {
            InitializeComponent();
            this.ImageList = imageList1;
            this.ImageAlign = ContentAlignment.MiddleLeft;
            changeState(buttonState);

            //监听属性值变化
            OnStateChanged += new StateChanged(DoEvent_StateChanged);
        }

        private void changeState(bool flag)
        {
            if (flag)
            {
                this.ImageIndex = 1;
                this.Text = "自动";
            }
            else
            {
                this.ImageIndex = 0;
                this.Text = "手动";
            }
        }


        //定义一个委托
        private delegate void StateChanged(object sender, EventArgs e);
        //定义一个委托关联的事件
        private event StateChanged OnStateChanged;

        //事件处理函数，属性值修改后的操作方法
        private void DoEvent_StateChanged(object sender, EventArgs e)
        {
            changeState(this.buttonState);
        }
        //事件触发函数
        private void WhenStateChanged()
        {
            if (OnStateChanged != null)
            {
                OnStateChanged(this, null);
            }
        }


        private bool buttonState;
        public bool ButtonState
        {
            get { return this.buttonState; }
            set
            {
                if (value != buttonState)
                {
                    buttonState = value;
                    WhenStateChanged();
                }
                this.buttonState = value;
            }
        }

    }
}

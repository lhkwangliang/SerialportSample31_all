using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HMIControls
{
    public sealed class MyDataGridView : System.Windows.Forms.DataGridView
    {
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                this.OnKeyPress(new KeyPressEventArgs('r'));
                return true;
            }
            else
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FC04_READ_INPUT_REGISTERS
{
    public partial class Support : Form
    {
        public Support()
        {
            InitializeComponent();
            
        }

        private void okButton_Click(object sender, EventArgs e)
        {
          //  Support f = new Support();
         //   f.Close();
            Support.ActiveForm.Close();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}

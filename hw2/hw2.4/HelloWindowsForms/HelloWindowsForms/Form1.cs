using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HelloWindowsForms
{
    class HelloWorld
    {
        public HelloWorld()
        {
            MessageBox.Show("《象曰》：天行健，君子以自强不息。\n", "这是一个不起眼的messagebox");
        }
    }
    public partial class form1 : Form
    {
        public form1()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            new HelloWorld();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class hello : Form
    {
        public hello()
        {
            InitializeComponent();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            //旋转显示文字
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            for (int i = 0; i <= 360; i += 10)
            {
                //平移Graphics对象到窗体中心
                g.TranslateTransform(this.Width / 2, this.Height / 2);
                //设置Graphics对象的输出角度
                g.RotateTransform(i);
                //设置文字填充颜色
                Brush brush = Brushes.DarkViolet;
                //旋转显示文字
                g.DrawString("****Hello World", new Font("Lucida Console", 18f), brush, 0, 0);
                //恢复全局变换矩阵
                g.ResetTransform();
            }
        }
    }
}

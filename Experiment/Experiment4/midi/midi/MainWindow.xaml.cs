using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ZedGraph;

namespace midi
{
    public partial class MainWindow : Window
    {
        int[] bauds = new int[] { 9600, 19200, 38400, 57600, 115200, 921600 }; //预设的波特率
        SerialPort mySerialPort = null; //SerialPort连接串口
        MidiData dataSent = new MidiData(); //绑定发送的数据
        MidiData dataRecv = new MidiData(); //绑定接收的数据
        MidiData dataNTC = new MidiData(); //绑定实时温度
        MidiData dataRCDS = new MidiData(); //绑定实时光强
        RGBValue rgbValue = new RGBValue(); //绑定RGB混合色
        LogData logData = new LogData(); //存储log数据

        /// <summary>
        /// 窗体初始化
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            //添加波特率选项
            comboBoxBaud.Items.Clear();
            foreach (var e in bauds)
            {
                comboBoxBaud.Items.Add(e);
            }
            comboBoxBaud.SelectedItem= comboBoxBaud.Items[4];

            //初始化绘图
            SetGraph();

            //绑定数据，并设定模式
            dataToSend.SetBinding(TextBox.TextProperty, new Binding("DataString") {
                Source=dataSent,
                Mode = BindingMode.OneWayToSource,
            });
            textBlockDataRecv.SetBinding(TextBlock.TextProperty, new Binding("DataString") {
                Source = dataRecv,
                Mode = BindingMode.OneWay,
            });
            textBlockRealData.SetBinding(TextBlock.TextProperty, new Binding("RealData") {
                Source = dataRecv,
                Mode = BindingMode.OneWay,
            });
            LabelMixRGB.SetBinding(System.Windows.Controls.Label.ForegroundProperty, new Binding("MixRGB") {
                Source = rgbValue,
                Mode = BindingMode.OneWay,
            });
            textBlockNTC.SetBinding(TextBlock.TextProperty, new Binding("RealData")
            {
                Source = dataNTC,
                Mode = BindingMode.OneWay,
            });
            textBlockRCDS.SetBinding(TextBlock.TextProperty, new Binding("RealData")
            {
                Source = dataRCDS,
                Mode = BindingMode.OneWay,
            });
        }

        /// <summary>
        /// 动态获取当前的串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBoxPort_DropDownOpened(object sender, EventArgs e)
        {
            string[] portnames = SerialPort.GetPortNames();
            ComboBox x = sender as ComboBox;
            x.Items.Clear();
            foreach (string xx in portnames)
            {
                x.Items.Add(xx);
            }
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        void CloseSerialPort()
        {
            if (mySerialPort != null)
            {
                mySerialPort.DataReceived -= new SerialDataReceivedEventHandler(DataReceivedHandler);
                mySerialPort.Close();
                Console.WriteLine("Closed:" + mySerialPort.ToString());
            }

        }

        /// <summary>
        /// 点击连接/关闭串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOpenClose_Click(object sender, RoutedEventArgs e)
        {
            Button now = sender as Button;
            if (now.Content.Equals("Open"))
            {
                if (comboBoxPort.SelectedItem != null)
                {
                    CloseSerialPort();
                    mySerialPort = new SerialPort(comboBoxPort.SelectedItem.ToString());

                    //串口设定初始化
                    mySerialPort.BaudRate = int.Parse(comboBoxBaud.SelectedItem.ToString());
                    mySerialPort.Parity = Parity.None;
                    mySerialPort.StopBits = StopBits.One;
                    mySerialPort.DataBits = 8;
                    mySerialPort.Handshake = Handshake.None;
                    mySerialPort.RtsEnable = false;
                    mySerialPort.ReceivedBytesThreshold = 1;
                    mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

                    mySerialPort.Open();

                    textBlockStatus.Text = "Open";
                    now.Content = "Close";
                }
                else
                {
                    MessageBox.Show("Please select serial port", "Warning");
                }
            }
            else
            {
                CloseSerialPort();
                textBlockStatus.Text = "Close";
                now.Content = "Open";
            }
        }

        /// <summary>
        /// arduino接收数据挂钩
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataReceivedHandler(
                    object sender,
                    SerialDataReceivedEventArgs e)
        {
            if (mySerialPort==null)
            {
                return;
            }
            int bytesLength = mySerialPort.BytesToRead;
            for (int i = 0; i < bytesLength; i++)
            {
                int data = mySerialPort.ReadByte();
                if ((data >> 7) != 0) //表示midi协议的头部，从位置0开始接收，避免数据漏收的情况
                {
                    dataRecv.DataIdx = 0;
                }
                dataRecv.SerialData[dataRecv.DataIdx++] = (byte)data;
                if (dataRecv.DataIdx >= dataRecv.SerialData.Length) //收满一个midi协议长度的字节流
                {
                    dataRecv.DataIdx = 0;
                    dataRecv.DataString = string.Format("{0:X2} {1:X2} {2:X2}", //更新绑定
                        dataRecv.SerialData[0],
                        dataRecv.SerialData[1],
                        dataRecv.SerialData[2]);
                    int realData = ((int)dataRecv.SerialData[2] << 7) + dataRecv.SerialData[1];
                    dataRecv.RealData = string.Format("{0:D5}", realData);
                    if ((dataRecv.SerialData[0]&0xf0)==0x80)
                    {
                        setADCData(dataRecv.SerialData[0] & 0xf, realData);
                    }
                    if (isLog) //如果log开始了，添加log数据
                    {
                        logData.received.Add(string.Format("{0:X2} {1:X2} {2:X2}",
                            dataRecv.SerialData[0],
                            dataRecv.SerialData[1],
                            dataRecv.SerialData[2]));
                    }
                }
            }
        }

        /// <summary>
        /// 设置传感器数据
        /// </summary>
        /// <param name="cnl"></param>
        /// <param name="realData"></param>
        private void setADCData(int cnl, int realData)
        {
            double res;
            if (cnl==0) //通道零，表示温度
            {
                double x = realData;
                const double DF_P2 = (9.619e-05);
                const double DF_P1 = (0.02703);
                const double DF_P0 = (-15.93);
                res = ((x) * ((x) * DF_P2 + DF_P1) + DF_P0);
                dataNTC.RealData = string.Format("{0:F2}", res); //更新绑定
                if (isLog) //更新log
                {
                    logData.realTimeTemperature.Add(string.Format("{0:F2}", res));
                }
            }
            else if (cnl==1) //通道一，表示光强
            {
                res = realData / 10.0;
                dataRCDS.RealData = string.Format("{0:F1}", res); //更新绑定
                if (isLog) //更新log
                {
                    logData.realTimeLightIntensity.Add(realData);
                }
            }
            else
            {
                return;
            }

            if (isPlot) //更新绘图
            {
                AddDataPoint(res, cnl);
            }
        }

        /// <summary>
        /// 发送数据btn
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSendData_Click(object sender, RoutedEventArgs e)
        {
            byte[] data = dataSent.DataBytes;
            if (data!=null && mySerialPort!=null)
            {
                mySerialPort.Write(data, 0, data.Length); //发送
                textBlockDataSent.Text = dataSent.DataString; //更新绑定
                if (isLog) //更新log
                {
                    logData.sent.Add(string.Format("#FF{0:X2}{1:X2}{2:X2}",
                        data[0],
                        data[1],
                        data[2]));
                }
            }
        }

        /// <summary>
        /// 窗口关闭，关闭串口连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseSerialPort();
        }

        /// <summary>
        /// 格式化midi数据
        /// </summary>
        /// <param name="cmd">命令</param>
        /// <param name="cnl">通道号</param>
        /// <param name="val">数值</param>
        /// <returns></returns>
        byte[] midiDataFormat(int cmd, int cnl, int val)
        {
            return new byte[] {
                (byte)(((cmd&0xf) << 4) | (cnl & 0xf)),
                (byte)(val & 0x7f),
                (byte)((val >> 7) & 0x7f)
            };
        }

        string[] color = { "Red", "Green", "Blue", "Yellow", "White" }; //LED灯5种颜色对应字符串
        int[] colorVal = new int[5]; //LED灯5种颜色对应数值
        int[] LEDID = { 9, 5, 6, 3, 10 }; //LED灯对应pin引脚号
        /// <summary>
        /// 滑块移动触发事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider x = sender as Slider;
            int idx = Array.IndexOf(color, x.Tag); //获取LED索引
            int val = (int)(x.Value*25.5); //获取要设置的灯亮度值
            colorVal[idx] = val; //修改值
            if (idx<3) //RGB三种颜色被修改，更新颜色块颜色
            {
                //格式为"#ARGB"，A为透明度，FF表示不透明
                rgbValue.MixRGB = string.Format("#FF{0:X2}{1:X2}{2:X2}",
                    colorVal[0],
                    colorVal[1],
                    colorVal[2]);
            }
            if (mySerialPort != null)
            {
                mySerialPort.Write(midiDataFormat(0xd, LEDID[idx], 255-val), 0, 3); //发送命令
                if (isLog) //更新log
                {
                    logData.LEDSent.Add(string.Format("{0:X2} {1:X2} {2:X2}",
                        0xd0 | LEDID[idx],
                        (255 - val) & 0x7f,
                        ((255 - val) >> 7) & 0x7f ));
                }
            }
        }

        int tickStart = 0; //维护绘图起始时间
        /// <summary>
        /// 绘图初设定
        /// </summary>
        private void SetGraph()
        {
            GraphPane myPane = zedgraph.GraphPane;
            myPane.Title.Text = ""; //标题
            myPane.XAxis.Title.Text = "Time(s)"; //X轴
            myPane.YAxis.Title.Text = ""; //Y轴
            RollingPointPairList list1 = new RollingPointPairList(1200); //第一条曲线，最多点数为1200
            RollingPointPairList list2 = new RollingPointPairList(1200); //第二条曲线，最多点数为1200
            LineItem curve1 = myPane.AddCurve("Temperature(°)", list1, System.Drawing.Color.Red, SymbolType.None/*.Diamond*/ ); //曲线风格
            LineItem curve2 = myPane.AddCurve("Light Intensity(x10 ADC)", list2, System.Drawing.Color.Blue, SymbolType.None);
            tickStart = Environment.TickCount; //获取当前时间戳
            zedgraph.AxisChange(); //更新X轴
        }
        /// <summary>
        /// 在curId条曲线上添加数据点(time,dataX)
        /// </summary>
        /// <param name="dataX"></param>
        /// <param name="curId"></param>
        void AddDataPoint(double dataX, int curId)
        {
            if (zedgraph.GraphPane.CurveList.Count <= curId) return; //非法curId
            LineItem curve = zedgraph.GraphPane.CurveList[curId] as LineItem;
            if (curve == null) return; //空曲线
            IPointListEdit list = curve.Points as IPointListEdit;
            if (list == null) return; //空列表
            double time = (Environment.TickCount - tickStart) / 1000.0; //当前时间，单位为秒
            list.Add(time, dataX); //添加数据点
            Scale xScale = zedgraph.GraphPane.XAxis.Scale; //调整X轴
            if (time > xScale.Max - xScale.MajorStep)
            {
                xScale.Max = time + xScale.MajorStep;
                xScale.Min = xScale.Max - 30.0;
            }
            zedgraph.AxisChange(); //更新X轴
            zedgraph.Invalidate();
        }
        bool isPlot; //是否开启了绘图
        /// <summary>
        /// 绘图开启btn
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnPlot_Click(object sender, RoutedEventArgs e)
        {
            Button x = sender as Button;
            if (x.Content.Equals("PlotStart"))
            {
                tickStart = Environment.TickCount; //设置绘图起始时间
                x.Content = "PlotStop";
                isPlot = true;
            }
            else
            {
                x.Content = "PlotStart";
                isPlot = false;
            }
        }
        /// <summary>
        /// 设置arduino返回实时数据的周期，单位是毫秒
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSetPeriod_Click(object sender, RoutedEventArgs e)
        {
            int res;
            bool parseRes = int.TryParse(textBoxSetPeriod.Text, out res);
            if (!parseRes || res<20 || res>16383)
            {
                //范围必须是[20,16383]ms
                MessageBox.Show("Please input a number within [20,16383].", "Warning");
                return;
            }
            if (mySerialPort != null)
            {
                mySerialPort.Write(midiDataFormat(0xa, 0, res), 0, 3); //发送指定的midi命令
            }
        }
        /// <summary>
        /// 清空绘图窗口btn
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnPlotClear_Click(object sender, RoutedEventArgs e)
        {
            //清空所有曲线
            for (int idxList = 0; idxList < zedgraph.GraphPane.CurveList.Count; idxList++)
            {
                LineItem curve = zedgraph.GraphPane.CurveList[idxList] as LineItem;
                if (curve == null) return;
                IPointListEdit list = curve.Points as IPointListEdit;
                if (list == null) return;
                list.Clear();
            }
            tickStart = Environment.TickCount; //重设绘图起始时间
            double time = (Environment.TickCount - tickStart) / 1000.0;
            Scale xScale = zedgraph.GraphPane.XAxis.Scale; //更新X轴
            xScale.Max = time + xScale.MajorStep;
            xScale.Min = xScale.Max - 30.0;
            zedgraph.AxisChange();
            zedgraph.Refresh(); //刷新图像
        }

        string logFileName; //保存log文件保存路径
        bool isLog; //是否开启log
        /// <summary>
        /// log开启btn
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLog_Click(object sender, RoutedEventArgs e)
        {
            Button x = sender as Button;
            if (x.Content.Equals("LogStart"))
            {
                if (logStart()) //尝试log开始
                {
                    x.Content = "LogStop";
                    isLog = true;
                }
            }
            else //log结束
            {
                string logJSONString = JsonConvert.SerializeObject(logData); //格式化json字符串
                System.IO.File.WriteAllText(logFileName, logJSONString); //写入到文件
                x.Content = "LogStart";
                isLog = false;
            }
        }
        /// <summary>
        /// 尝试log开始
        /// </summary>
        /// <returns></returns>
        private bool logStart()
        {
            if (mySerialPort==null || mySerialPort.IsOpen==false) //非法状态，返回失败
            {
                MessageBox.Show("Please log after opening.", "Warning");
                return false;
            }

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog(); //打开dlg
            dlg.FileName = string.Format("log-{0:yyyy-mm-dd-hh-mm-ss}", DateTime.Now); //以当前时间初始化文件名
            dlg.DefaultExt = ".json"; //默认格式
            dlg.Filter = "(.json)|*.json"; //可选格式
            Nullable<bool> result = dlg.ShowDialog(); //获取结果
            if (result==false) //非法结果返回
            {
                return false;
            }
            logFileName = dlg.FileName; //获取保存的路径

            logData = new LogData();
            logData.portName = mySerialPort.PortName; //获取当前串口的端口名和波特率
            logData.baudRate = mySerialPort.BaudRate;

            return true;
        }
    }
}

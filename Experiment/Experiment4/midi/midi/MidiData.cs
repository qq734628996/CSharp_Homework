using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace midi
{
    /// <summary>
    /// 绑定midi数据
    /// </summary>
    public class MidiData:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 输入的字符串
        /// </summary>
        private string dataString;
        public string DataString
        {
            get
            {
                return dataString;
            }
            set
            {
                if (dataString!=value)
                {
                    dataString = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 将dataString转为midi格式的数组
        /// </summary>
        public byte[] DataBytes
        {
            get
            {
                if (string.IsNullOrEmpty(dataString))
                {
                    return null;
                }
                string[] splited = dataString.Split(new Char[] { ' ', ',', '.', ':', '\t' });
                byte[] dataBuf = new byte[splited.Length];
                for (int i = 0; i < splited.Length; i++)
                {
                    if (!(byte.TryParse(splited[i], NumberStyles.HexNumber, null, out dataBuf[i])))
                    {
                        dataBuf[i] = 0;
                    }
                }
                return dataBuf;
            }
        }

        /// <summary>
        /// midi格式的数据，3个字节
        /// </summary>
        private byte[] serialData;
        public byte[] SerialData
        {
            get
            {
                return serialData;
            }
            set
            {
                serialData = value;
                NotifyPropertyChanged();
            }
        }
        public int DataIdx { get; set; }

        public MidiData()
        {
            dataString = "00 00 00";
            serialData = new byte[3];
            Array.Clear(serialData, 0, serialData.Length);
            DataIdx = 0;
        }

        /// <summary>
        /// 实际midi格式数据存储的真实数值
        /// </summary>
        private string realData;
        public string RealData
        {
            get
            {
                return realData;
            }
            set
            {
                realData = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// 温度对应的ADC值，并转化为摄氏度的单位
        /// </summary>
        private double ntc;
        public double NTC
        {
            get
            {
                const double DF_P2 = (9.619e-05);
                const double DF_P1 = (0.02703);
                const double DF_P0 = (-15.93);
                double x = ntc;
                return ((x) * ((x) * DF_P2 + DF_P1) + DF_P0);
            }
            set
            {
                if (ntc != value)
                {
                    ntc = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 光强对应的ADC值
        /// </summary>
        private double rcds;
        public double RCDS
        {
            get
            {
                return rcds;
            }
            set
            {
                if (rcds!=value)
                {
                    rcds = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }
}

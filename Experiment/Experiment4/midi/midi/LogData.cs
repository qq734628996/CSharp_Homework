using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace midi
{
    /// <summary>
    /// log数据
    /// </summary>
    class LogData
    {
        public string portName;
        public int baudRate;
        public List<string> sent = new List<string>();
        public List<string> received = new List<string>();
        public List<string> LEDSent = new List<string>();
        public List<string> realTimeTemperature = new List<string>();
        public List<int> realTimeLightIntensity = new List<int>();
    }
}

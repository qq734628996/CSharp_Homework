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
    /// 绑定RGB合成色
    /// </summary>
    public class RGBValue : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 混合色的值
        /// </summary>
        public string mixRGB;
        public string MixRGB
        {
            get
            {
                return mixRGB;
            }
            set
            {
                if (mixRGB != value)
                {
                    mixRGB = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 初始化，格式为ARGB对应的值，其中A为透明度，默认FF为不透明
        /// </summary>
        public RGBValue()
        {
            mixRGB = "#FF000000";
        }
    }
}

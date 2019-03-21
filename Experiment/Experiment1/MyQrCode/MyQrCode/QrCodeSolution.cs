using Gma.QrCodeNet.Encoding;
using Gma.QrCodeNet.Encoding.Windows.Render;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using Excel = Microsoft.Office.Interop.Excel;
using System.Reflection;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace MyQrCode
{
    /// <summary>
    /// 生成二维码解决方案
    /// </summary>
    class QrCodeSolution
    {

        /// <summary>
        /// 使用指南
        /// </summary>
        public static void showGuide()
        {
            Console.WriteLine("这是一个简单易用的二维码生成工具，使用方法如下：");
            Console.WriteLine();
            Console.WriteLine("MyQrCode.exe CONTENT");
            Console.WriteLine("    此命令用于生成文本为CONTENT的二维码，并将其显示到屏幕上");
            Console.WriteLine();
            Console.WriteLine("MyQrCode.exe -s");
            Console.WriteLine("    此命令用于从数据库中读取文本，批量生成二维码并保存到Image目录下");
            Console.WriteLine();
            Console.WriteLine("MyQrCode.exe -f DATAFILE");
            Console.WriteLine("    此命令用于从txt或excel文件中读取文本，批量生成二维码并保存到Image目录下");
            Console.WriteLine();
            Console.WriteLine("请注意，任何用于生成二维码的文本长度都不能超过64！");
        }

        /// <summary>
        /// 判断文本合法性
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <returns></returns>
        private static bool IsValidQrCode(string content)
        {
            return !string.IsNullOrEmpty(content) && content.Length <= 64; //任何空的或太长的文本都是非法的。
        }

        /// <summary>
        /// 生成并显示二维码到屏幕上
        /// </summary>
        /// <param name="content">文本内容</param>
        public static void ShowQrCode(string content)
        {
            if (!IsValidQrCode(content))                                   //如果文本非法
            {
                throw new Exception("Illegal content! Be sure the content is not empty, null or too large.");  //抛出异常
            }

            QrEncoder qrEncoder = new QrEncoder(ErrorCorrectionLevel.H);   //高容错能力，30%的字码可被修正，代价是二维码图变大
            QrCode qrCode = new QrCode();
            if (!qrEncoder.TryEncode(content, out qrCode))                 //如果尝试编码失败
            {
                throw new Exception("Fail to encode.");                    //抛出异常
            }

            for (int j = 0; j < qrCode.Matrix.Width; j++)                  //将编码后的方阵以二维码的形式打印到屏幕上
            {
                for (int i = 0; i < qrCode.Matrix.Width; i++)
                {
                    Console.Write(qrCode.Matrix[i, j] ? '　' : '█');
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 填充前导零
        /// </summary>
        /// <param name="lineNumber">行号</param>
        /// <param name="digit">格式化后数字的位数</param>
        /// <returns>以字符串的形式返回格式化后的数字</returns>
        private static string formatLineNumber(int lineNumber, int digit)
        {
            string goal = lineNumber.ToString();
            while (goal.Length < digit)
            {
                goal = "0" + goal;                                                      //填充前导零
            }
            return goal;
        }

        /// <summary>
        /// 根据路径后缀判断文件格式是否为txt文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns></returns>
        private static bool IsTxt(string path)
        {
            return Path.GetExtension(path)==".txt";
        }

        /// <summary>
        /// 根据路径后缀判断文件格式是否为excel文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns></returns>
        private static bool IsExcel(string path)
        {
            string extension = Path.GetExtension(path);
            return extension == ".xls" || extension == ".xlsx";
        }

        /// <summary>
        /// 从文件中读取文本
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>返回文本数组</returns>
        private static string[] GetContentsFromFile(string path)
        {
            if (!File.Exists(path))                                                     //如果路径不存在
            {
                throw new DirectoryNotFoundException($"Illegal path:{path}");           //抛出路径找不到的异常
            }
            if (IsTxt(path))                                                            //如果文件是一个txt文件
            {
                StreamReader sr = new StreamReader(path, EncodingType.GetType(path));   //获取正确的txt编码格式并读入到内存中
                string buf = sr.ReadToEnd();                                            //从流读入到字符串里
                sr.Close();                                                             //关闭流
                return buf.Split('\n');                                                 //根据换行符切割字符串并返回
            }
            else if (IsExcel(path))                                                     //如果文件是一个excel文件
            {
                _Application xlsApp = new Excel.Application();
                string fullPath = System.IO.Path.GetFullPath(path);                     //解决不能读取相对路径的问题
                Workbook xlsWb = xlsApp.Workbooks.Open(fullPath);                       //读取excel文件到内存
                Worksheet xlsWs = xlsWb.Worksheets[1];                                  //选择Sheet1第一个表

                List<string> contents = new List<string>();                             //使用List存储文本
                for (int i = 1; i <= xlsWs.UsedRange.Rows.Count; i++)                   //逐行读取
                {
                    contents.Add(xlsWs.Cells[i, 1].Value2);                             //读取单元格
                }
                return contents.ToArray();                                              //List转为数组并返回
            }
            else                                                                        //文件非法
            {
                throw new Exception($"Illegal file formation:{path}, it only accepts a txt or excel file.");  //抛出异常
            }
            return null;
        }

        /// <summary>
        /// 从数据库里读取文本。
        /// 有待于完善，目前只是简单读取本地用户名为HW，密码为空，数据库为hw，表格为qrcode，列为content的内容。
        /// </summary>
        /// <returns></returns>
        public static string[] getContentsFromMySQL()
        {
            string connStr = "user=HW;Database=hw;";                             //设置用户名，数据库信息，ip默认为本地，密码为空
            MySqlConnection conn = new MySqlConnection(connStr);
            conn.Open();                                                         //打开数据库
            MySqlCommand cmd = new MySqlCommand("select * from qrcode", conn);   //选择表"qrcode"
            MySqlDataReader reader = cmd.ExecuteReader();
            List<string> contents = new List<string>();                          //使用List存储
            while (reader.Read())                                                //逐行读取到末尾
            {
                contents.Add(reader.GetString("content"));                       //读取列为content的内容
            }
            reader.Close();
            return contents.ToArray();                                           //List转为数组并返回
        }

        /// <summary>
        /// 判断读取源的类型，有文件和数据库
        /// </summary>
        public enum SourceType
        {
            FILE,
            MYSQL
        };

        /// <summary>
        /// 从读取源读取文本
        /// </summary>
        /// <param name="sourceType">读取指令</param>
        /// <param name="path">文本路径，如果有</param>
        /// <returns>文本数组</returns>
        private static string[] getContents(SourceType sourceType, string path)
        {
            switch (sourceType)
            {
                case SourceType.FILE:                          //从文件中读取
                    return GetContentsFromFile(path);
                    break;
                case SourceType.MYSQL:                         //从数据库中读取
                    return getContentsFromMySQL();
                    break;
                default:                                       //非法的读取源
                    throw new Exception("SourceType error.");  //抛出异常
                    break;
            }
            return null;
        }

        /// <summary>
        /// 生成并保存二维码
        /// </summary>
        /// <param name="sourceType">读取源</param>
        /// <param name="path">读取路径，如果有</param>
        public static void SavaQrCode(SourceType sourceType, string path = null)
        {
            string[] contents = getContents(sourceType, path);                          //获取文本
            
            for (int i=0; i<contents.Length; i++)
            {
                if (!IsValidQrCode(contents[i])) continue;                              //非法文本会被忽略

                QrEncoder qrEncoder = new QrEncoder(ErrorCorrectionLevel.H);            //高容错能力，30%的字码可被修正，代价是二维码图变大
                QrCode qrCode = new QrCode();
                if (!qrEncoder.TryEncode(contents[i], out qrCode)) continue;            //编码失败则跳过

                GraphicsRenderer renderer = new GraphicsRenderer(new FixedCodeSize(400, QuietZoneModules.Zero));  //设置二维码固定大小
                MemoryStream ms = new MemoryStream();
                renderer.WriteToStream(qrCode.Matrix, ImageFormat.Png, ms);             //将二维码方阵写入到流
                var imageTemp = new Bitmap(ms);                                         //从流中生成位图
                var image = new Bitmap(imageTemp, new Size(new System.Drawing.Point(200, 200)));  //将位图resize
                if (!Directory.Exists("Image"))                                         //如果当前路径下没有Image这个路径
                {
                    Directory.CreateDirectory("Image");                                 //创建之
                }

                //保存图片到Image路径下，命名格式为三位数的行号+文本前4位内容，格式为png格式
                image.Save($"Image\\{formatLineNumber(i+1, 3)}{contents[i].Substring(0, 4)}.png", ImageFormat.Png);
            }

        }

        
    }
}

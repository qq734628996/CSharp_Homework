using System;
using solve = MyQrCode.QrCodeSolution;

namespace MyQrCode
{
    /// <summary>
    /// 一个简单易用的二维码生成工具
    /// </summary>
    class MyQrCode
    {
        static void Main()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();                             //获取命令行参数，第一个参数为文件路径
                switch (args.Length)
                {
                    case 1:                                                                   //没有任何参数
                        solve.showGuide();                                                    //显示说明手册
                        break;
                    case 2:                                                                   //有1个参数
                        if (args[1]=="-s")                                                    //-s表示从数据库读取数据
                        {
                            solve.SavaQrCode(solve.SourceType.MYSQL);                         //读取并保存
                        }
                        else if (args[1]=="-f")
                        {
                            throw new Exception("No input file!");
                        }
                        else
                        {
                            solve.ShowQrCode(args[1]);                                        //有一个参数并且是文本，生成二维码到屏幕上
                        }
                        break;
                    case 3:                                                                   //有两个参数
                        {
                            string instruction = args[1];                                     //获取指令参数
                            string path = args[2];                                            //获取文件路径
                            if (instruction.Equals("-f"))                                     //-f表示从文件中读取
                            {
                                solve.SavaQrCode(solve.SourceType.FILE, path);                //读取并保存
                            }
                            else                                                              //非法指令
                            {
                                throw new Exception($"Illegal instruction:{instruction}");    //抛出异常
                            }
                        }
                        break;
                    default:                                                                  //参数过多
                        throw new Exception("Too many arguments!");                           //抛出异常
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}

using System;

namespace HelloWorld
{
    class HelloWorld
    {
        public static void print(string[] args)
        {
            Console.WriteLine("实例化Hello World");
            Console.WriteLine("The length of args is {0}", args.Length);
            foreach (string i in args)
            {
                Console.Write(i + " ");
            }
            Console.WriteLine("");
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            HelloWorld.print(args);
            Console.ReadLine();
        }
    }
}

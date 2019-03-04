using System;

namespace HelloWorld
{
    class HelloWorld
    {
        public void print()
        {
            Console.WriteLine("实例化Hello World");
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            HelloWorld helloWorld = new HelloWorld();
            helloWorld.print();
            Console.ReadLine();
        }
    }
}

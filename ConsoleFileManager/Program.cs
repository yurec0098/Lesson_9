using System;
using System.Text;

namespace ConsoleFileManager
{
	class Program
	{
		static FileMan fm = new FileMan();
		static void Main(string[] args)
		{
			Console.OutputEncoding = Encoding.Unicode;
			Console.InputEncoding = Encoding.Unicode;
			fm.Run();
		}
	}
}

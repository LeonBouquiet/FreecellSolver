using FreecellSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreecellSolverApp
{
	public class ConsoleWriter : IConsoleWriter
	{
		public void Write(string text)
		{
			Console.Write(text);
		}

		public void WriteLine(string text)
		{
			Console.WriteLine(text);
		}

		public void Write(string format, params object[] args)
		{
			Console.Write(format, args);
		}

		public void WriteLine(string format, params object[] args)
		{
			Console.WriteLine(format, args);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreecellSolver
{
	public interface IConsoleWriter
	{
		void Write(string text);

		void WriteLine(string text);

		void Write(string format, params object[] args);

		void WriteLine(string format, params object[] args);

	}
}

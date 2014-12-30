using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreecellSolver
{
	public enum SolverCommandName
	{
		ShowResult,
		Quit
	}

	public class SolverCommand
	{
		public SolverCommandName Name { get; set; }

		public int FreeCellGameNr { get; set; }

		public SolverCommand(SolverCommandName name)
		{
			Name = name;
		}

		public SolverCommand(SolverCommandName name, int freeCellGameNr)
		{
			Name = name;
			FreeCellGameNr = freeCellGameNr;
		}

	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreecellSolver
{
	public class MoveDescription
	{
		public int MoveIncrement { get; set; }

		public string Text { get; set; }

		public MoveDescription(int moveIncrement, string text)
		{
			MoveIncrement = moveIncrement;
			Text = text;
		}
	}

	public class SolutionStep
	{
		public SolutionStep Parent { get; private set; }

		public GameState GameState { get; private set; }

		public List<MoveDescription> MoveDescriptions { get; private set; }

		public int Priority
		{
			get { return GameState.Priority; }
		}

		/// <summary>
		/// Returns all SolutionStep's Parents, starting with this one and ending with the root.
		/// </summary>
		public List<SolutionStep> PathToRoot
		{
			get
			{
				List<SolutionStep> result = new List<SolutionStep>();
				for (SolutionStep current = this; current.Parent != null; current = current.Parent)
					result.Add(current);

				return result;
			}
		}

		public SolutionStep(SolutionStep parent, GameState gameState)
		{
			Parent = parent;
			GameState = gameState;
			MoveDescriptions = new List<MoveDescription>();
		}
	}
}

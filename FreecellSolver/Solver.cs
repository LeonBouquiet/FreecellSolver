using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Wintellect.PowerCollections;

namespace FreecellSolver
{
	public class Solver
	{
		private ConcurrentQueue<SolverCommand> _commandQueue;

		private IConsoleWriter _consoleWriter;

		public Solver(ConcurrentQueue<SolverCommand> commandQueue, IConsoleWriter consoleWriter)
		{
			_commandQueue = commandQueue;
			_consoleWriter = consoleWriter;
		}

		private IDictionary<PackedGameState, PackedGameState> _knownGameStates = new Dictionary<PackedGameState, PackedGameState>();


		public void Solve(GameState gameState)
		{
			List<PackedGameState> solutionStates = RunPrioritized(gameState);
			WriteSolution(gameState, solutionStates);
		}

		public List<PackedGameState> RunPrioritized(GameState initialState)
		{
			PriorityQueue<GameState> queue = new PriorityQueue<GameState>((left, right) => (left.Priority - right.Priority));
			PrintGameState(initialState);

			List<PackedGameState> currentOptimalSolution = null;
			int maxLevel = initialState.MinimumSolutionCost + 11;
			long pruneCount = 0;
			long improveCount = 0;
			List<int> levelInBlock = new List<int>();

			initialState = (GameState)initialState.Clone();
			initialState.NormalizeAndPack(null);
			queue.Enqueue(initialState);

			int loopLimit = 9950000;
			for (int index = 0; index < loopLimit; index++)
			{
				if (queue.Count == 0)
				{
					_consoleWriter.WriteLine("No more GameStates available in the queue.");
					return currentOptimalSolution;
				}

				SolverCommand cmd;
				if(_commandQueue.TryDequeue(out cmd))
				{
					if (cmd.Name == SolverCommandName.ShowResult)
					{
						WriteSolution(initialState, currentOptimalSolution);
					}
					if (cmd.Name == SolverCommandName.Quit)
					{
						_consoleWriter.WriteLine("Quit requested...");
						return currentOptimalSolution;
					}
				}

				GameState gameState = queue.Dequeue();

				Move[] moves = MoveGenerator.Generate(gameState);
				foreach (Move move in moves)
				{
					GameState childState = gameState.CreateChildState(move, null);

					//If we've already found a solution, only explore this one if it has a chance 
					//of being better, otherwise disregard it.
					if (ShouldBePruned(childState, maxLevel))
					{
						pruneCount++;
						continue;
					}

					PackedGameState packedChild = childState.NormalizeAndPack(gameState.Packed);
					if (childState.IsSolved)
					{
						currentOptimalSolution = packedChild.PathToRoot;
						maxLevel = childState.Level;

						_consoleWriter.Write("New optimal solution found: level {0}, {1} steps. Pruning queue... ", childState.Level, currentOptimalSolution.Count);

						long removeCount = queue.RemoveAll(gs => ShouldBePruned(gs, maxLevel));
						_consoleWriter.WriteLine("Removed {0} queue entries.", removeCount);
					}

					levelInBlock.Add(childState.Level);

					//If we didn't queue this child state before, queue it now.
					PackedGameState existing = _knownGameStates.TryGetValue(packedChild);
					if (existing == null || existing.Level > packedChild.Level)
					{
						if (existing != null)
							improveCount++;

						_knownGameStates[packedChild] = packedChild;
						queue.Enqueue(childState);
					}
				}

				if (index % 10000 == 0)
				{
					levelInBlock.Sort();

					int min = levelInBlock.First();
					int q1 = levelInBlock[levelInBlock.Count / 4];
					int q3 = levelInBlock[3 * levelInBlock.Count / 4];
					int max = levelInBlock.Last();
					_consoleWriter.WriteLine("{0,8} processed, {1,8} queued, {2} pruned, {3} improved. Level [Min: {4}, Q1: {5}, Q3: {6}, Max: {7}]", index, queue.Count, pruneCount, improveCount, min, q1, q3, max);

					levelInBlock.Clear();
				}
			}

			return currentOptimalSolution;
		}

		private bool ShouldBePruned(GameState gameState, int maxLevel)
		{
			if (gameState.Level >= maxLevel)
				return true;

			if (gameState.Level + gameState.MinimumSolutionCost >= maxLevel)
				return true;

			//Werkt niet:

			//if(maxLevel < Int32.MaxValue)
			//{
			//	int fiftyPercentLevel = (int)Math.Ceiling(maxLevel * 0.5d);
			//	if (gameState.Level >= fiftyPercentLevel && gameState.Progress < 17)
			//		return true;

			//	int eightyPercentLevel = (int)Math.Ceiling(maxLevel * 0.8d);
			//	if (gameState.Level >= eightyPercentLevel && gameState.Progress < 26)
			//		return true;
			//}

			return false;
		}

		public void WriteSolution(GameState initialState, List<PackedGameState> solutionStates)
		{
			if (solutionStates == null)
			{
				_consoleWriter.WriteLine("No solution is available.");
				return;
			}

			Set<PackedGameState> solutionSet = new Set<PackedGameState>(solutionStates);
			List<SolutionStep> solutionSteps = GenerateSolutionSteps(initialState, solutionSet);
			solutionSteps.Reverse();

			int moveCount = 0;
			foreach (SolutionStep ss in solutionSteps)
			{
				foreach (string moveDescription in ss.MoveDescriptions)
					_consoleWriter.WriteLine("[{0,3}] {1}", ++moveCount, moveDescription);

				PrintGameState(ss.GameState);
			}
		}

		public List<SolutionStep> GenerateSolutionSteps(GameState initialState, Set<PackedGameState> solutionSet)
		{
			HashSet<PackedGameState> knownGameStates = new HashSet<PackedGameState>();
			Queue<SolutionStep> queue = new Queue<SolutionStep>();

			initialState = (GameState)initialState.Clone();
			PrintGameState(initialState);

			queue.Enqueue(new SolutionStep(null, initialState));

			for (int index = 0; index < 100000; index++)
			{
				SolutionStep currentStep = queue.Dequeue();

				Move[] moves = MoveGenerator.Generate(currentStep.GameState);
				foreach (Move move in moves)
				{
					MoveLogger moveLogger = new MoveLogger();
					GameState childState = currentStep.GameState.CreateChildState(move, moveLogger);

					GameState normalizedChild = (GameState)childState.Clone();
					PackedGameState packedChild = normalizedChild.NormalizeAndPack();

					//Only enqueue the child states that are known to be in the solution and that 
					//we haven't processed before.
					if (solutionSet.Contains(packedChild) && knownGameStates.Contains(packedChild) == false)
					{
						SolutionStep childStep = new SolutionStep(currentStep, childState);
						childStep.MoveDescriptions.AddRange(moveLogger.Messages);

						queue.Enqueue(childStep);
						knownGameStates.Add(packedChild);

						if (childState.IsSolved)
						{
							List<SolutionStep> solutionSteps = childStep.PathToRoot;
							return solutionSteps;
						}
					}
				}
			}

			return null;
		}

		private void PrintGameState(GameState gameState)
		{
			_consoleWriter.WriteLine(gameState.Description);
			_consoleWriter.WriteLine("");
		}
	}
}

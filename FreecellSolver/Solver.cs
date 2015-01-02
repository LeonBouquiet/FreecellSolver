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
		private IConsoleWriter _consoleWriter;

		public int ProcessCount { get; private set; }

		public Solver(IConsoleWriter consoleWriter)
		{
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
			PriorityQueue<PackedGameState> queue = new PriorityQueue<PackedGameState>((left, right) => (left.Priority - right.Priority));
			PrintGameState(initialState);

			List<PackedGameState> currentOptimalSolution = null;
			int maxLevel = initialState.MinimumSolutionCost + 11;
			long pruneCount = 0;
			long improveCount = 0;

			initialState = (GameState)initialState.Clone();
			PackedGameState initialPackedState = initialState.NormalizeAndPack(null);
			queue.Enqueue(initialPackedState);

			int loopLimit = 9950000;
			for (ProcessCount = 0; ProcessCount < loopLimit; ProcessCount++)
			{
				if (queue.Count == 0)
				{
					_consoleWriter.WriteLine("No more GameStates available in the queue.");
					return currentOptimalSolution;
				}

				PackedGameState packedState = queue.Dequeue();
				GameState gameState = packedState.Unpack();

				Move[] moves = MoveGenerator.Generate(gameState);
				foreach (Move move in moves)
				{
					GameState childState = gameState.CreateChildState(move, null);

					//If we've already found a solution, only explore this one if it has a chance 
					//of being better, otherwise disregard it.
					if (childState.Level >= maxLevel || (childState.Level + childState.MinimumSolutionCost >= maxLevel))
					{
						pruneCount++;
						continue;
					}

					PackedGameState packedChild = childState.NormalizeAndPack(packedState);
					if (childState.IsSolved)
					{
						currentOptimalSolution = packedChild.PathToRoot;
						maxLevel = childState.Level;

						_consoleWriter.Write("New optimal solution found: level {0}, {1} steps. Pruning queue... ", childState.Level, currentOptimalSolution.Count);

						long removeCount = queue.RemoveAll(gs => ShouldBePruned(gs, maxLevel));
						_consoleWriter.WriteLine("Removed {0} queue entries.", removeCount);
					}

					//If we didn't queue this child state before, queue it now.
					PackedGameState existing = _knownGameStates.TryGetValue(packedChild);
					if (existing == null || existing.Level > packedChild.Level)
					{
						if (existing != null)
							improveCount++;

						_knownGameStates[packedChild] = packedChild;
						queue.Enqueue(packedChild);
					}
				}

				if (ProcessCount % 10000 == 0)
					_consoleWriter.WriteLine("{0,8} processed, {1,8} queued, {2} pruned, {3} improved.", ProcessCount, queue.Count, pruneCount, improveCount);
			}

			return currentOptimalSolution;
		}

		private bool ShouldBePruned(PackedGameState packedState, int maxLevel)
		{
			if (packedState.Level >= maxLevel)
				return true;

			if (packedState.Level + packedState.MinimumSolutionCost >= maxLevel)
				return true;

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

			int previousCount = 0;
			int moveCount = 0;
			foreach (SolutionStep ss in solutionSteps)
			{
				foreach (MoveDescription moveDescription in ss.MoveDescriptions)
				{
					int temporaryIncrement = Math.Min(moveDescription.MoveIncrement, 1);
					moveCount += temporaryIncrement;
					string moveCountText = (moveCount != previousCount) ? moveCount.ToString() : "";

					_consoleWriter.WriteLine("[{0,3}] {1}", moveCountText, moveDescription.Text);

					previousCount = moveCount;
					moveCount += (-temporaryIncrement + moveDescription.MoveIncrement);
				}

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

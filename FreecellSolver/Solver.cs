using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Wintellect.PowerCollections;

namespace FreecellSolver
{
	public class Solver
	{
		public Statistics Statistics { get; set; }

		public Solver()
		{
			Statistics = new Statistics();
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
			Statistics.LogInfo(initialState.Description + Environment.NewLine);

			List<PackedGameState> currentOptimalSolution = null;
			int maxLevel = initialState.MinimumSolutionCost + 11;

			Statistics.Initialize(() => queue.Count);

			initialState = (GameState)initialState.Clone();
			PackedGameState initialPackedState = initialState.NormalizeAndPack(null);
			queue.Enqueue(initialPackedState);

			int loopLimit = 9950000;
			for (Statistics.ProcessCount = 0; Statistics.ProcessCount < loopLimit; Statistics.ProcessCount++)
			{
				if (queue.Count == 0)
				{
					Statistics.LogInfo("No more GameStates available in the queue.");
					break;
				}

				if(Console.KeyAvailable)
				{
					ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: false);
					if (Char.ToLower(keyInfo.KeyChar) == 's')
					{
						Statistics.LogInfo("User pressed 's' with {0} GameStates left in the queue, stopping...", queue.Count);
						break;
					}
				}

				PackedGameState packedState = queue.Dequeue();
				GameState gameState = packedState.Unpack();
				Statistics[gameState.Level].Processed++;

				Move[] moves = MoveGenerator.Generate(gameState);
				foreach (Move move in moves)
				{
					Statistics[gameState.Level].ChildStatesGenerated++;
					GameState childState = gameState.CreateChildState(move, null);

					//If we've already found a solution, only explore this one if it has a chance 
					//of being better, otherwise disregard it.
					if (childState.Level >= maxLevel || (childState.Level + childState.MinimumSolutionCost >= maxLevel))
					{
						Statistics.PruneCount++;
						Statistics[gameState.Level].ChildStatesPruned++;
						continue;
					}

					PackedGameState packedChild = childState.NormalizeAndPack(packedState);
					if (childState.IsSolved)
					{
						currentOptimalSolution = packedChild.PathToRoot;
						maxLevel = childState.Level;

						Statistics.LogEventWithoutNewLine("New optimal solution found: level {0}, {1} steps. Pruning queue... ", childState.Level, currentOptimalSolution.Count);

						long removeCount = queue.RemoveAll(gs => ShouldBePruned(gs, maxLevel));
						Statistics.LogEventAddition("Removed {0} queue entries.", removeCount);
					}

					//If we didn't queue this child state before, queue it now.
					PackedGameState existing = _knownGameStates.TryGetValue(packedChild);
					if (existing == null || existing.Level > packedChild.Level)
					{
						if (existing != null)
							Statistics.ImproveCount++;

						_knownGameStates[packedChild] = packedChild;
						queue.Enqueue(packedChild);
					}
					else
					{
						Statistics[gameState.Level].ChildStatesDuplicates++;
					}
				}

				if (Statistics.ProcessCount % 10000 == 0)
					Statistics.LogProgress();
			}

			Statistics.StopTimer();
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
				Statistics.LogResult("No solution is available.");
				return;
			}

			StringWriter writer = new StringWriter();
			writer.WriteLine(initialState.Description + Environment.NewLine);

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

					writer.WriteLine("[{0,3}] {1}", moveCountText, moveDescription.Text);

					previousCount = moveCount;
					moveCount += (-temporaryIncrement + moveDescription.MoveIncrement);
				}

				writer.WriteLine(ss.GameState.Description + Environment.NewLine);
			}

			Statistics.LogResult(writer.ToString());
		}

		public List<SolutionStep> GenerateSolutionSteps(GameState initialState, Set<PackedGameState> solutionSet)
		{
			Dictionary<PackedGameState, PackedGameState> knownGameStates = new Dictionary<PackedGameState, PackedGameState>();
			PriorityQueue<SolutionStep> queue = new PriorityQueue<SolutionStep>((left, right) => (left.Priority - right.Priority));

			initialState = (GameState)initialState.Clone();

			queue.Enqueue(new SolutionStep(null, initialState));
			List<SolutionStep> currentOptimalSolution = null;

			for (int index = 0; index < 100000; index++)
			{
				if (queue.Count == 0)
					break;

				SolutionStep currentStep = queue.Dequeue();

				Move[] moves = MoveGenerator.Generate(currentStep.GameState);
				foreach (Move move in moves)
				{
					MoveLogger moveLogger = new MoveLogger();
					GameState childState = currentStep.GameState.CreateChildState(move, moveLogger);

					GameState normalizedChild = (GameState)childState.Clone();
					PackedGameState packedChild = normalizedChild.NormalizeAndPack();

					//Only enqueue the child states that are known to be in the solution.
					PackedGameState existing = knownGameStates.TryGetValue(packedChild);
					if (solutionSet.Contains(packedChild) && (existing == null || packedChild.Priority < existing.Priority))
					{
						SolutionStep childStep = new SolutionStep(currentStep, childState);
						childStep.MoveDescriptions.AddRange(moveLogger.Messages);

						queue.Enqueue(childStep);
						knownGameStates[packedChild] = packedChild;

						if (childState.IsSolved)
						{
							List<SolutionStep> solutionSteps = childStep.PathToRoot;
							if (currentOptimalSolution == null || solutionSteps[0].Priority < currentOptimalSolution[0].Priority)
								currentOptimalSolution = solutionSteps;
						}
					}
				}
			}

			return currentOptimalSolution;
		}
	}
}

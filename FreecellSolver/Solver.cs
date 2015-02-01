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

		private IDictionary<PackedGameState, PackedGameState> _knownGameStates = new Dictionary<PackedGameState, PackedGameState>(PackedGameState.DictionaryComparer);


		public void Solve(GameState gameState)
		{
			List<PackedGameState> solutionStates = RunPrioritized(gameState);

			WriteSolution(gameState, solutionStates);
		}

		private static Dictionary<int, Tuple<int, bool>> _optimalSolutionHashes = new Dictionary<int, Tuple<int, bool>>
		{ 
			//{ 0x5C4AE531, new Tuple<int, bool>( 1, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x19D89438, new Tuple<int, bool>( 3, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x425BB126, new Tuple<int, bool>( 4, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x38F08D76, new Tuple<int, bool>( 5, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x2B1B2412, new Tuple<int, bool>( 6, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x5D388DA5, new Tuple<int, bool>( 7, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x6937233D, new Tuple<int, bool>( 8, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x32CAD219, new Tuple<int, bool>( 9, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x5665F4F2, new Tuple<int, bool>(10, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x403F18EE, new Tuple<int, bool>(11, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x6650D875, new Tuple<int, bool>(12, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x16B6E055, new Tuple<int, bool>(13, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x23E0CFBF, new Tuple<int, bool>(14, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			{ 0x6A2E40E7, new Tuple<int, bool>(17, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			{ 0x0E947601, new Tuple<int, bool>(18, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x39660FD0, new Tuple<int, bool>(19, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x593203BB, new Tuple<int, bool>(20, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x26E487A8, new Tuple<int, bool>(21, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x7B6E34A0, new Tuple<int, bool>(22, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x44C5BF91, new Tuple<int, bool>(23, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x40421619, new Tuple<int, bool>(25, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x7F802811, new Tuple<int, bool>(26, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x07A887DE, new Tuple<int, bool>(27, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x2753CC4E, new Tuple<int, bool>(28, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x5FC21780, new Tuple<int, bool>(29, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x35B5D171, new Tuple<int, bool>(30, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x37149028, new Tuple<int, bool>(31, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x788FAD33, new Tuple<int, bool>(32, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x499B3B99, new Tuple<int, bool>(34, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x3955AD66, new Tuple<int, bool>(35, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x0EB1AD44, new Tuple<int, bool>(36, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x1A580094, new Tuple<int, bool>(37, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x1D9715F3, new Tuple<int, bool>(38, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
			//{ 0x6DF28662, new Tuple<int, bool>(39, false) },	 //Lvl=   1 CSec= 1 Cmpl= 1 MnRq=36
		};

		//	0x19D89438,	//Lvl=   3 CSec= 2 Cmpl= 2 MnRq=34
		//	0x425BB126,	//Lvl=   4 CSec= 3 Cmpl= 2 MnRq=33
		//	0x38F08D76,	//Lvl=   5 CSec= 4 Cmpl= 2 MnRq=32
		//	0x2B1B2412,	//Lvl=   6 CSec= 5 Cmpl= 2 MnRq=32
		//	0x5D388DA5,	//Lvl=   7 CSec= 6 Cmpl= 2 MnRq=31
		//	0x6937233D,	//Lvl=   8 CSec= 7 Cmpl= 3 MnRq=30
		//	0x32CAD219,	//Lvl=   9 CSec= 8 Cmpl= 3 MnRq=30
		//	0x5665F4F2,	//Lvl=  10 CSec= 8 Cmpl= 3 MnRq=29
		//	0x403F18EE,	//Lvl=  11 CSec= 9 Cmpl= 3 MnRq=28
		//	0x6650D875,	//Lvl=  12 CSec=10 Cmpl= 3 MnRq=27
		//	0x16B6E055,	//Lvl=  13 CSec=11 Cmpl= 3 MnRq=26
		//	0x23E0CFBF,	//Lvl=  14 CSec=12 Cmpl= 3 MnRq=25
		//	0x6A2E40E7,	//Lvl=  17 CSec=13 Cmpl= 3 MnRq=22
		//	0x0E947601,	//Lvl=  18 CSec=14 Cmpl= 3 MnRq=21
		//	0x39660FD0,	//Lvl=  19 CSec=15 Cmpl= 5 MnRq=20
		//	0x593203BB,	//Lvl=  20 CSec=16 Cmpl= 5 MnRq=19
		//	0x26E487A8,	//Lvl=  21 CSec=17 Cmpl= 5 MnRq=18
		//	0x7B6E34A0,	//Lvl=  22 CSec=18 Cmpl= 5 MnRq=17
		//	0x44C5BF91,	//Lvl=  23 CSec=19 Cmpl= 5 MnRq=16
		//	0x40421619,	//Lvl=  25 CSec=19 Cmpl= 5 MnRq=14
		//	0x7F802811,	//Lvl=  26 CSec=20 Cmpl= 6 MnRq=13
		//	0x07A887DE,	//Lvl=  27 CSec=21 Cmpl= 6 MnRq=12
		//	0x2753CC4E,	//Lvl=  28 CSec=21 Cmpl= 6 MnRq=11
		//	0x5FC21780,	//Lvl=  29 CSec=22 Cmpl= 6 MnRq=10
		//	0x35B5D171,	//Lvl=  30 CSec=23 Cmpl= 6 MnRq= 9
		//	0x37149028,	//Lvl=  31 CSec=24 Cmpl= 7 MnRq= 8
		//	0x788FAD33,	//Lvl=  32 CSec=25 Cmpl= 7 MnRq= 7
		//	0x499B3B99,	//Lvl=  34 CSec=25 Cmpl=10 MnRq= 5
		//	0x3955AD66,	//Lvl=  35 CSec=25 Cmpl=12 MnRq= 4
		//	0x0EB1AD44,	//Lvl=  36 CSec=26 Cmpl=12 MnRq= 3
		//	0x1A580094,	//Lvl=  37 CSec=26 Cmpl=14 MnRq= 2
		//	0x1D9715F3,	//Lvl=  38 CSec= 0 Cmpl=48 MnRq= 1
		//	0x6DF28662,	//Lvl=  39 CSec= 0 Cmpl=52 MnRq= 0
		//});

		public List<PackedGameState> RunPrioritized(GameState initialState)
		{
			if(ConfigSettings.Relaxation > 0)
				Statistics.LogInfo("*** Relaxed pruning: -{0} ***", ConfigSettings.Relaxation);

			Statistics.LogInfo("Configured weights: Level={0}, Consecutiveness={1}, Completeness={2}, Availability={3}",
				ConfigSettings.LevelWeight, ConfigSettings.ConsecutivenessWeight, ConfigSettings.CompletenessWeight, ConfigSettings.AvailabilityWeight);
			Statistics.LogInfo(initialState.Description + Environment.NewLine);

			PriorityQueue<PackedGameState> queue = new PriorityQueue<PackedGameState>(PackedGameState.PriorityQueueComparer);

			List<PackedGameState> currentOptimalSolution = null;
			int maxLevel = initialState.MinimumSolutionCost + 11;

			Statistics.Initialize(() => queue.Count);

			initialState = (GameState)initialState.Clone();
			PackedGameState initialPackedState = initialState.NormalizeAndPack(null);
			queue.Enqueue(initialPackedState);

			while (queue.Count > 0)
			{
				Statistics.ProcessCount++;

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

				if(gameState.Breakpoint)
				{
					Statistics.LogInfo("*** Breakpoint for gamestate with hash code {0:X8} at level {1}.", gameState.HashCode, gameState.Level);
				}

				Move[] moves = MoveGenerator.Generate(gameState);
				foreach (Move move in moves)
				{
					Statistics[gameState.Level].ChildStatesGenerated++;
					GameState childState = gameState.CreateChildState(move, null);

					Tuple<int, bool> tuple = _optimalSolutionHashes.TryGetValue(childState.HashCode);
					if (tuple != null && childState.Level == tuple.Item1)
					{
						_optimalSolutionHashes[childState.HashCode] = new Tuple<int, bool>(tuple.Item1, true);
						childState.Breakpoint = true;
						Statistics.LogInfo("*** Found hash {0:X8} at level {1}.", childState.HashCode, childState.Level);
					}

					//If we've already found a solution, only explore this one if it has a chance 
					//of being better, otherwise disregard it.
					if (childState.Level >= maxLevel || (childState.Level + childState.MinimumSolutionCost - ConfigSettings.Relaxation >= maxLevel))
					{
						if (childState.Breakpoint)
						{
							Statistics.LogInfo("*** Pruning gamestate with hash code {0:X8} at level {1}.", childState.GetHashCode(), childState.Level);
						}

						Statistics.PruneCount++;
						Statistics[gameState.Level].ChildStatesPruned++;
						continue;
					}

					PackedGameState packedChild = childState.NormalizeAndPack(packedState);
					if (childState.IsSolved)
					{
						currentOptimalSolution = packedChild.PathToRoot;
						maxLevel = childState.Level;

						PackedGameState breakpointState = queue.ToList().FirstOrDefault(pgs => pgs.Breakpoint);

						int breakpointCount = queue.Count(pgs => pgs.Breakpoint);
						Statistics.LogEventWithoutNewLine("New optimal solution found: level {0}, {1} steps. Pruning queue... ", childState.Level, currentOptimalSolution.Count);

						//List<PackedGameState> queueList = queue.ToList();
						//List<PackedGameState> removed = queueList.Where(gs => ShouldBePruned(gs, maxLevel)).ToList();
						//int index = removed.FindIndex(pgs => pgs.GetHashCode() == 0x6A2E40E7);
						//int index2 = queueList.FindIndex(pgs => pgs.GetHashCode() == 0x6A2E40E7);

						//Self-test
						int previousPriority = Int32.MinValue;
						foreach(PackedGameState pgs in queue)
						{
							if (previousPriority <= pgs.Priority)
								previousPriority = pgs.Priority;
							else
								throw new InvalidOperationException("Queue is not sorted.");
						}

						long removeCount = queue.RemoveAll(gs => ShouldBePruned(gs, maxLevel));
						
						Statistics.LogEventAddition("Removed {0} queue entries.", removeCount);

						//index = removed.FindIndex(pgs => pgs.GetHashCode() == 0x6A2E40E7);
						//index2 = queueList.FindIndex(pgs => pgs.GetHashCode() == 0x6A2E40E7);
						breakpointCount = queue.Count(pgs => pgs.Breakpoint);

						//queue = new PriorityQueue<PackedGameState>((left, right) => (left.Priority - right.Priority));
						//foreach (PackedGameState pgs in queueList)
						//	queue.Add(pgs);

						Statistics.LogProgress(breakpointCount);
					}

					//If we didn't queue this child state before, queue it now.
					PackedGameState existing = _knownGameStates.TryGetValue(packedChild);
					if (existing == null || existing.Level > packedChild.Level)
					{
						if (existing != null)
						{
							Statistics.ImproveCount++;

							if (existing.Breakpoint)
							{
								Statistics.LogInfo("*** Improved gamestate with hash code {0:X8} at level {1}.", childState.GetHashCode(), childState.Level);
								packedChild.Breakpoint = true;
							}
						}

						_knownGameStates[packedChild] = packedChild;
						queue.Enqueue(packedChild);
					}
					else
					{
						Statistics[gameState.Level].ChildStatesDuplicates++;
					}
				}

				if (Statistics.ProcessCount % 10000 == 0)
				{
					int breakpointCount = queue.Count(pgs => pgs.Breakpoint);
					Statistics.LogProgress(breakpointCount);
				}
			}

			string result = string.Join("\r\n", _optimalSolutionHashes
				.OrderBy(tpl => tpl.Value.Item1)
				.Select(tpl => string.Format("Level {0}, Hashcode {1:X8}, seen: {2}", tpl.Value.Item1, tpl.Key, tpl.Value.Item2)));

			Statistics.LogInfo(result);

			Statistics.StopTimer();
			return currentOptimalSolution;
		}

		private bool ShouldBePruned(PackedGameState packedState, int maxLevel)
		{
			if (packedState.Level >= maxLevel || packedState.Level + packedState.MinimumSolutionCost - ConfigSettings.Relaxation >= maxLevel)
			{
				if (packedState.Breakpoint)
				{
					Statistics.LogInfo("*** Pruning gamestate with hash code {0:X8} at level {1}.", packedState.GetHashCode(), packedState.Level);
				}

				return true;
			}

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

			Set<PackedGameState> solutionSet = new Set<PackedGameState>(solutionStates, PackedGameState.DictionaryComparer);
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
			Dictionary<PackedGameState, PackedGameState> knownGameStates = new Dictionary<PackedGameState, PackedGameState>(PackedGameState.DictionaryComparer);
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using FreecellSolver;
using Wintellect.PowerCollections;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace FreecellSolverApp
{
	public class Program
	{
		#region Game states

		private const string Game_14937 =
			"(..)(..)(..)(..)<..><..><..><..>\n" +
			" c9  s5  sJ  hJ  d6  hK  c4  h6 \n" +
			" h8  sA  s6  hT  cA  sT  d8  c8 \n" +
			" c2  h3  s4  s8  s2  sK  h7  d2 \n" +
			" d3  h9  d5  dK  dJ  s9  c3  c5 \n" +
			"|cK  cJ  c7  cQ  hQ  s7  sQ  h2 \n" +
			"|h5  c6  d4  hA  s3  d7  dT  dA \n" +
			"|cT  d9  h4  dQ\n";

		private const string Game_14937_2 =
			"(..)(..)(..)(..)<..><..><d2><..>\n" +
			" d9  s5  sJ  hJ  d6  hK  c4  h6 \n" +
			" h8  sA  s6  hT  cA  sT  d8  c8 \n" +
			" c2  h3  s4  s8  s2  sK  h7  h2 \n" +
			" d3  h9  d5  dK  dJ  s9  c3  c5 \n" +
			"|cK  cJ  c7  cQ  hQ  s7  sQ  h4 \n" +
			"|h5 |c6  d4  hA  s3  d7  dT  ..\n" +
			"|cT |c9  ..  dQ \n";

		private const string Game_14937_2_1 =
			"(..)(..)(..)(..)<..><..><d2><..>\n" +
			" d9  s5  sJ  hJ  hQ  hK  c4  h6 \n" +
			" h8  sA  s6  hT  cA  sT  d8  c8 \n" +
			" c2  h3  s4  s8  s2  sK  h7  h2 \n" +
			" d3  h9  d5  dK  dJ  s9  c3  c5 \n" +
			"|cK  cJ  c7  cQ  d6  s7  sQ  h4 \n" +
			"|h5 |c6  d4  hA  s3  d7  dT  ..\n" +
			"|cT |c9  ..  dQ \n";

		private const string Game_14937_3 =
			"(dQ)(..)(..)(..)<hA><..><..><..>\n" +
			" d9  s5  sJ  hJ  d6  hK  c4  h6\n" +
			" h8  sA  s6  hT  cA  sT  d8  c8\n" +
			" c2  h3  s4  s8  s2  sK  h7  d2\n" +
			" d3  h9  d5 [dK] dJ  s9  c3  c5\n" +
			" cK  cJ  c7 [cQ] hQ  s7  sQ  h2\n" +
			" h5  c6  d4          d7  dT  dA\n" +
			" cT  c9 [h4]                   \n" +
			"        [s3] \n";

		private const string Game_5979 =
			"(c4)(cK)(sT)(..)<h2><s2><d3><cA>\n" +
			" s3  sK  h6  hT  sQ  c8  s9  d9\n" +
			" c3  hQ      dT  sJ  d7  d8  s8\n" +
			" hJ          cJ  cT  c6  s7  h7\n" +
			" h9          c7  dK      d6  s6\n" +
			" dJ          c2  h5      c5  d5\n" +
			" d4          s5  dQ      h4  s4\n" +
			" c9          hK              h3\n" +
			" h8          cQ";

		private const string Game_28387 =
			"(sJ)(dK)(..)(..)<hA><sA><dA><..>\n" +
			" h2  cQ  d2  h5  s2  cJ  c9  dJ\n" +
			" sQ  hK  d9  d3  h6  dT  c8  cT\n" +
			" s3  s8  h4  s7  s6      d7  h9\n" +
			" s5  dQ  hQ  d6  h3      sK    \n" +
			" hT  h8  c2  cA          c4    \n" +
			" s9  sT  c5  hJ          c7    \n" +
			" d8  d4  cK  h7                \n" +
			"     c3      c6\n" +
			"             d5\n" +
			"             s4";

		private const string Game_13501 =
			"(..)(..)(..)(..)<..><..><..><..>\n" +
			" s6  d2  s5  h7  c2  h3  h4  s4\n" +
			" hQ  dK  c3  h5  cQ  c6  h6  s2\n" +
			" cJ  hK  dT  h9  dQ  sA  s9  d4\n" +
			" h2  c4  s8  d6  d5  d9  cK  sQ\n" +
			" cT  cA  s7  c9  c8  sK  sJ  sT\n" +
			" c5  c7  s3  d8  dJ  d7  d3  hT\n" +
			" hJ  h8  hA  dA                ";

		private const string Game_29912 =
			"(c2)(..)(..)(..)<..><s2><..><..>\n" +
			" hQ  dQ  d3  cJ  cK  dK  cT  s4\n" +
			" cQ  sJ  h8  dT  cA  sQ  hA  s8\n" +
			" s9  hT  hJ      c8  dJ  c4  sK\n" +
			" h2  c9  h7      h6  sT  s6  c5\n" +
			" hK      s7      dA  d9  h3  d4\n" +
			" d2      d7      d8      d5  c3\n" +
			" h9      c6      c7            \n" +
			"         h5      d6            \n" +
			"                 s5            \n" +
			"                 h4            \n" +
			"                 s3            ";

		#endregion

		//private int _generatedCount;
		//private int _statesInPreviousLevels;

		//4, 31515!!! 64630
		//Balanced: 1 (20 min), 12851 (30min)
		public static void Main(string[] args)
		{
			ConsoleWriter consoleWriter = new ConsoleWriter();

			int freeCellGameNr = 3;
			GameState gameState = GameStateGenerator.GenerateGameState(freeCellGameNr);
			Solver solver = new Solver(consoleWriter);

			consoleWriter.WriteLine("Starting to solve FreeCell game number {0}...", freeCellGameNr);

			Stopwatch stopwatch = Stopwatch.StartNew();
			solver.Solve(gameState);
			stopwatch.Stop();

			double rate = solver.ProcessCount / stopwatch.Elapsed.TotalSeconds;
			Console.WriteLine("Found solution after {0} gamestates. Elapsed time: {1}m {2}s ({3:n0} gamestates/sec).", solver.ProcessCount, Math.Floor(stopwatch.Elapsed.TotalMinutes), stopwatch.Elapsed.Seconds, rate);

			Console.WriteLine("Press enter to exit...");
			Console.ReadLine();
		}

		//[ 41] Move the 2 cards "s8, h9" from cascade 7 to cascade 4. - 8 Stap
		//Lvl=  32 CSec=25 Cmpl=13 Aval= 6
		//----- Priority =       -80 -----
		//(..)(..)(..)(..)<h3><sA><d4><c5>
		//		 dQ  h7 [c6][cK] s4 [d8]
		//		 s2  cJ [h5][hQ]    [s7]
		//		 dT  h6     [sJ]    [d6]
		//		[hK] sK     [hT]    [s5]
		//		[cQ] s9     [c9]    [h4]
		//		[dJ][dK]    [h8]    [s3]
		//		[cT][sQ]    [c7]
		//		[d9][hJ]
		//		[c8][sT]
		//		[d7][h9]
		//		[s6][s8]
		//		[d5]


		//Regels: 
		// - 2 of meer opeenvolgende kaarten bovenop 1 of meer niet-opeenvolgende kaarten 
		//   kost in principe 1 level, tenzij de hoogste kaart van de reeks opeenvolgende
		//   kaarten kleiner* is dan de laagste kaart van de overige kaarten - dan kan het
		//   ook gratis.
		// - Verwijder alle opeenvolgende kaarten voor de rest van het algoritme.
		// - Loop per cascade alle kaarten van bovenop de cascade naar onderop af. Voor elke
		//   kaart K:
		//   - Bepaal of er in de kaarten onder K tenminste 1 kaart zit die kleiner* is dan
		//	 K; zo ja, dan kost het verwijderen van K 1 level, anders kan het gratis.
    
		//De regels van kleiner* volgen die van Safe moves, omdat dit de enige manier is om een
		//kaart gratis verplaatst te krijgen. Kleiner wil in dit geval zeggen: vereist een move 
		//anders dan een Safe move (en dus minimaal 1 level) om de kaart vrij te spelen.

		//Een kaart A is kleiner dan B als:
		//- Wanneer ze dezelfde suit hebben, rank(a) < rank(b)
		//- Wanneer de een rood is en de ander zwart, rank(a) + 1 < rank(b)
		//- Wanneer ze dezelfde kleur hebben, maar een verschillend suit, rank(a) + 2 < rank(b)
	}
}

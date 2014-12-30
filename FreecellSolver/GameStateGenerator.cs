using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreecellSolver
{
	/// <summary>
	/// Generates a GameState for a Windows FreeCell game nr.
	/// </summary>
	public static class GameStateGenerator
	{
		private static int _randomSeed;

		private const string ReferenceCards =
			"cA,dA,hA,sA,c2,d2,h2,s2,c3,d3,h3,s3,c4,d4,h4,s4,c5,d5,h5,s5,c6,d6,h6,s6,c7,d7,h7,s7,c8,d8,h8,s8," +
			"c9,d9,h9,s9,cT,dT,hT,sT,cJ,dJ,hJ,sJ,cQ,dQ,hQ,sQ,cK,dK,hK,sK";

		//Taken from http://rosettacode.org/wiki/Deal_cards_for_FreeCell
		public static GameState GenerateGameState(int freecellGameNr)
		{
			_randomSeed = freecellGameNr;
			List<int> remainingCards = ReferenceCards.Split(',')
				.Select(s => CardUtil.ParseCard(s))
				.ToList();

			List<int>[] resultCascades = Enumerable.Range(0, 8)
				.Select(i => new List<int>())
				.ToArray();

			for (int index = 0; index < 52; index++)
			{
				_randomSeed = (214013 * _randomSeed + 2531011) & Int32.MaxValue;

				int randomNr = _randomSeed >> 16;
				int randomIndex = randomNr % remainingCards.Count;
				remainingCards.Swap(randomIndex, remainingCards.Count - 1);

				resultCascades[index % 8].Add(remainingCards.Last());
				remainingCards.RemoveAt(remainingCards.Count - 1);
			}

			Cascade[] cascades = resultCascades
				.Select(li => new Cascade(li.AsEnumerable().Reverse()))
				.ToArray();

			GameState result = new GameState(new List<int>(), new int[] { 0, 0, 0, 0 }, cascades);
			return result;
		}
	}
}

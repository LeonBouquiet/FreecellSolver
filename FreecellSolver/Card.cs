using System;
using System.Collections.Generic;
using System.Text;

namespace FreecellSolver
{
	[Flags]
	public enum Suit
	{
		None = 0,
		Heart = 0x10,
		Spade = 0x20,
		Diamond = 0x30,
		Club = 0x40,
		Mask = 0x70,
		RedMask = 0x10
	}

	[Flags]
	public enum Rank
	{
		None = 0,
		Ace = 1,
		Two = 2,
		Three = 3,
		Four = 4,
		Five = 5,
		Six = 6,
		Seven = 7,
		Eight = 8,
		Nine = 9,
		Ten = 10,
		Jack = 11,
		Queen = 12,
		King = 13,
		Mask = 0x0F
	}

	public static class CardUtil
	{
		private static char[] SuitCharacters = new char[] 
			{ '.', 'h', 's', 'd', 'c'};

		public static Suit[] Suits = new Suit[] 
			{ Suit.Heart, Suit.Spade, Suit.Diamond, Suit.Club };

		private static char[] RankCharacters = new char[] 
			{ '.', 'A', '2', '3', '4', '5', '6', '7', '8', '9', 'T', 'J', 'Q', 'K'};

		public static Rank[] Ranks = new Rank[]
			{ Rank.Ace,   Rank.Two,  Rank.Three, Rank.Four, Rank.Five,  Rank.Six, Rank.Seven, 
			  Rank.Eight, Rank.Nine, Rank.Ten,   Rank.Jack, Rank.Queen, Rank.King };

		/// <summary>
		/// Determines if the <paramref name="candidateCard"/> is allowed to be placed on the 
		/// <paramref name="fixedCard"/>, according to the rules of the cascade: this is allowed
		/// when the candidateCard is one less in rank than the fixed card and has a different
		/// color (if one of them is red, the other must be black).
		/// </summary>
		public static bool CanBuildCascade(int fixedCard, int candidateCard)
		{
			if (fixedCard == 0x00 || candidateCard == 0x00)
				throw new ArgumentException("Neither the fixedCard nor the candidateCard may be a None card.");

			if ((fixedCard & (int)Rank.Mask) - (candidateCard & (int)Rank.Mask) != 1)
				return false;

			return ((fixedCard & (int)Suit.RedMask) != (candidateCard & (int)Suit.RedMask));
		}

		/// <summary>
		/// Determines if the <paramref name="candidateCard"/> is allowed to be placed on the 
		/// <paramref name="fixedCard"/>, according to the rules of the foundations: this is 
		/// allowed when the candidateCard is one higher in rank than the fixed card, and has 
		/// the same suit.
		/// </summary>
		public static bool CanBuildFoundation(int fixedCard, int candidateCard)
		{
			if ((candidateCard & (int)Rank.Mask) - (fixedCard & (int)Rank.Mask) != 1)
				return false;

			return ((fixedCard & (int)Suit.Mask) == (candidateCard & (int)Suit.Mask) || fixedCard == 0x00);
		}

		/// <summary>
		/// Calculates and returns the size of the largest card sequence that can be moved in one
		/// (super)move with the given number of <paramref name="freeCells"/> and 
		/// <paramref name="emptyCascades"/>.
		/// </summary>
		/// <param name="freeCells">The number of free swap cells.</param>
		/// <param name="emptyCascades">The number of cascades without cards.</param>
		/// <returns>The number of cards that can be moved in one supermove.</returns>
		public static int GetMaxSequenceSizeToMove(int freeCells, int emptyCascades)
		{
			//See posts containing "supermove" at http://www.grahamkendall.net/Mathematica/mm-463.txt
			//The formula is: size = (freecells + 1) * (2 ^ emptycascades)
			return (freeCells + 1) * (1 << emptyCascades);
		}

		/// <summary>
		/// Returns if <paramref name="cardA"/> is considered larger than <paramref name="cardB"/> 
		/// according to the Safe move definitions - that is, if cardA were on top of cardB, if 
		/// it is guaranteed to require a non-safe (i.e. non-free) move to get cardA removed from 
		/// this Cascade.
		/// </summary>
		public static bool IsDefinitelyMore(int cardA, int cardB, List<int> foundations)
		{
			//If cardA can be placed directly on its foundation, its move is free.
			int foundationCardForAsSuit = foundations[(cardA >> 4) - 1];
			if (CanBuildFoundation(foundationCardForAsSuit, cardA) == true)
				return false;

			//Shortcut: If the difference in Rank is 2 or more, the suits don't matter.
			int rankDifference = ((cardA & (int)Rank.Mask) - (cardB & (int)Rank.Mask));
			if (rankDifference >= 2)
				return true;

			if ((cardA & (int)Suit.Mask) == (cardB & (int)Suit.Mask))
			{
				//Both cards have the same Suit. cardA must simply be higher in rank.
				return (rankDifference >= 1);
			}
			else if ((cardA & (int)Suit.RedMask) != (cardB & (int)Suit.RedMask))
			{
				//One card is Red, the other black. cardA must be at least one more in rank.
				return (rankDifference >= 1);
			}
			else
			{
				//Both cards of the same color, but of different suit. cardA must have at least 2 
				//more in rank to definitely count as more.
				return (rankDifference >= 2);
			}
		}

		public static Suit ParseSuit(char suitChar)
		{
			if (suitChar == ' ')
				suitChar = '.';

			int index = Array.IndexOf(SuitCharacters, Char.ToLower(suitChar));
			if (index >= 0)
				return (Suit)(index * 0x10);
			else
				throw new ArgumentException(string.Format("The character \"{0}\" has no suit mapped to it.", suitChar));
		}

		public static Rank ParseRank(char rankChar)
		{
			if (rankChar == ' ')
				rankChar = '.';

			int index = Array.IndexOf(RankCharacters, Char.ToUpper(rankChar));
			if (index >= 0)
				return (Rank)index;
			else
				throw new ArgumentException(string.Format("The character \"{0}\" has no rank mapped to it.", rankChar));
		}

		public static int ParseCard(string suitRankString)
		{
			if (string.IsNullOrEmpty(suitRankString))
				throw new ArgumentNullException("suitRankString");
			if (suitRankString.Length != 2)
				throw new ArgumentException("The suit/rank string must be exactly 2 characters.", "suitRankString");

			int suit = (int)ParseSuit(suitRankString[0]);
			int rank = (int)ParseRank(suitRankString[1]);
			return suit | rank;
		}

		public static char GetSuitCharacter(Suit suit)
		{
			return SuitCharacters[((int)suit) >> 4];
		}

		public static char GetRankCharacter(Rank rank)
		{
			return RankCharacters[(int)rank];
		}

		public static string GetCardString(int card)
		{
			return new string(new char[] { 
				GetSuitCharacter((Suit)(card & (int)Suit.Mask)),
				GetRankCharacter((Rank)(card & (int)Rank.Mask))});
		}

		public static string GetCardString(IEnumerable<int> cards, string separator = ", ")
		{
			List<string> descriptions = new List<string>();
			foreach (int card in cards)
				descriptions.Add(GetCardString(card));

			return string.Join(separator, descriptions.ToArray());
		}
	}
}

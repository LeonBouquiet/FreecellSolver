using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace FreecellSolver
{
	/// <summary>
	/// Models a single vertical column of cards, where the 0th index is the "top" card of the 
	/// cascade (ie the one that can always be moved and which is typically displayed as the card 
	/// closest to the bottom of the screen).
	/// The card with the highest index is the "bottom" card of the cascade (because it has the 
	/// most cards covering it) and is always displayed near the top of the screen.
	/// </summary>
	[DebuggerDisplay("{CardUtil.GetCardString(_cards)}")]
	public class Cascade: IComparable<Cascade>
	{
		private List<int> _cards;

		private int _sequenceLength;

		/// <summary>
		/// Gets the column of cards, where the 0th index is the "top" card of the cascade.
		/// </summary>
		[DebuggerDisplay("{CardUtil.GetCardString(_cards)}")]
		public IList<int> Cards
		{
			get { return _cards; }
		}

		/// <summary>
		/// Returns the length of the rank-decreasing, suit-alternating sequence in this Cascade 
		/// that starts with the top card - this is 1 if there is no such sequence, or 0 if the
		/// cascade is empty.
		/// </summary>
		public int SequenceLength
		{
			get { return _sequenceLength; }
		}

		/// <summary>
		/// Gets if this Cascade doesn't contain any cards.
		/// </summary>
		public bool IsEmpty
		{
			get { return (_cards.Count == 0); }
		}

		/// <summary>
		/// Gets a value indicating how consecutive all cards in this Cascade are. The higher 
		/// this value, the more consecutive, or 0 if less than 2 cards are present.
		/// </summary>
		public int Consecutiveness
		{
			get
			{
				int result = 0;
				if (_cards.Count >= 2)
				{
					for (int index = _cards.Count - 1; index > 0; index--)
					{
						if (CardUtil.CanBuildCascade(_cards[index], _cards[index - 1]))
							result++;
					}
				}

				return result;
			}
		}

		/// <summary>
		/// Returns the lower bound of the cost (in Levels) that it will take to free all cards in 
		/// this Cascade.
		/// </summary>
		public int CalculateMinimumSolutionCost(List<int> foundations)
		{
			int minimumCost = 0;
			if (_cards.Count >= 2)
			{
				//Iterate over all cards from the topmost (i.e. movable) card to the bottom card.
				int cardToFreeIndex = 0;
				while(cardToFreeIndex < _cards.Count)
				{
					//Starting with the cardToFreeIndex, see how much consecutive cards we can find.
					//This is 0 if the range is empty, 1 if the card to free doesn't form a cascade
					//with the card below it, or more if there is a cascade.
					int sequenceLength = GetSequenceLength(cardToFreeIndex);
					if (sequenceLength > 0)
					{
						//In case of a cascade, we use only the bottom (i.e. the highest ranking) 
						//card to work with. In case there is no cascade, this is basically the top card.
						int cardToFree = _cards[cardToFreeIndex + sequenceLength - 1];

						//Of the cards below cardToFree, see if we can find at least one card that 
						//is smaller than cardToFree (according to the Safe moves). If so, this means
						//that we won't be able to move cardToFree with a safe move, hence, this will
						//cost at least one level.
						for (int index = cardToFreeIndex + sequenceLength; index < _cards.Count; index++)
						{
							if (CardUtil.IsDefinitelyMore(cardToFree, _cards[index], foundations))
							{
								minimumCost++;
								break;
							}
						}

						cardToFreeIndex += sequenceLength;
					}
				}
			}

			return minimumCost;
		}

		/// <summary>
		/// Constructs a new Cascase instance from the given collection of cards.
		/// </summary>
		/// <param name="cards">The cards to use, where the 0th index is the "top" card of the 
		/// cascade (which is typically displayed as the card closest to the bottom of the screen).</param>
		public Cascade(IEnumerable<int> cards)
		{
			_cards = new List<int>(cards);
			_sequenceLength = GetSequenceLength(0);
		}

		public Cascade(Cascade source)
		{
			_cards = new List<int>(source._cards);
			_sequenceLength = GetSequenceLength(0);
		}

		public int CompareTo(Cascade other)
		{
			//Compare Cascades based on their bottom cards, or the None card if a Cascade is empty.
			int leftCard = (_cards.Count > 0) ? _cards[_cards.Count - 1] : 0x00;
			int rightCard = (other._cards.Count > 0) ? other._cards[other._cards.Count - 1] : 0x00;

			//The rank is more important than the suit.
			if ((leftCard & (int)Rank.Mask) != ((rightCard & (int)Rank.Mask)))
				return (leftCard & (int)Rank.Mask) - ((rightCard & (int)Rank.Mask));
			else
				return (leftCard & (int)Suit.Mask) - ((rightCard & (int)Suit.Mask));

			//return leftCard - rightCard;
		}

		/// <summary>
		/// Returns the top N cards (starting with the top one) from the cascade, but does not 
		/// remove them.
		/// </summary>
		public int[] GetTopNCards(int count)
		{
			if (count > _cards.Count)
				throw new InvalidOperationException(string.Format("Cannot get {0} cards, because the cascade only has {1}.", count, _cards.Count));

			int[] result = _cards.Take(count).ToArray();
			return result;
		}

		/// <summary>
		/// Returns the length of the rank-decreasing, suit-alternating sequence in this Cascade 
		/// that starts with the top card - this is 1 if there is no such sequence, or 0 if the
		/// cascade is empty.
		/// </summary>
		public int GetSequenceLength(int startIndex)
		{
			if (_cards.Count == 0 && startIndex == 0)
				return 0;
			if (startIndex >= _cards.Count)
				throw new ArgumentException();

			//If there are less than 2 cards present, there is no sequence.
			if (_cards.Count - startIndex < 2)
				return _cards.Count - startIndex;

			for (int index = startIndex + 1; index < _cards.Count; index++)
			{
				//Test if the card at the current index is allowed to be built upon its underlying card.
				if (CardUtil.CanBuildCascade(_cards[index], _cards[index - 1]) == false)
					return index - startIndex;
			}

			//The entire cascade is a sequence.
			return _cards.Count - startIndex;
		}

		public int RemoveTopCard()
		{
			return RemoveSequence(1)[0];
		}

		public int[] RemoveSequence(int count)
		{
			if(count > _cards.Count)
				throw new InvalidOperationException(string.Format("Cannot move a sequence of {0} cards, because the cascade only has {1}.", count, _cards.Count));
			if(count > _sequenceLength)
				throw new InvalidOperationException(string.Format("Cannot move a sequence of {0} cards, because the cascade only defines a sequence of {1} cards.", count, _sequenceLength));

			int[] result = new int[count];
			_cards.CopyTo(0, result, 0, count);
			_cards.RemoveRange(0, count);

			_sequenceLength = GetSequenceLength(0);
			return result;
		}

		public void AppendTopCard(int card)
		{
			AppendSequence(new int[] { card });
		}

		public void AppendSequence(int[] cards)
		{
			if ((_cards.Count > 0) && (CardUtil.CanBuildCascade(_cards[0], cards[cards.Length - 1]) == false))
				throw new InvalidOperationException(string.Format("Cannot add the sequence starting with {0} to the cascade ending on {1}.",
					CardUtil.GetCardString(cards[cards.Length - 1]), CardUtil.GetCardString(_cards[0])));

			_cards.InsertRange(0, cards);
			_sequenceLength = GetSequenceLength(0);
		}

		/// <summary>
		/// Composes and returns a description of this Cascade as a multiline string, starting with 
		/// the bottommost card and ending with the top card. Each line is 4 characters long.
		/// </summary>
		public string[] GetMultilineDescription()
		{
			List<string> cardStrings = new List<string>();
			for (int index = 0; index < _cards.Count; index++ )
			{
				string cardString = CardUtil.GetCardString(_cards[index]);
				if (_sequenceLength > 1 && index < _sequenceLength)
					cardString = "[" + cardString + "]";
				else
					cardString = " " + cardString + " ";

				cardStrings.Add(cardString);
			}

			cardStrings.Reverse();
			return cardStrings.ToArray();
		}

		/// <summary>
		/// Describes the contents of this cascade, with the bottom card on the left and the top 
		/// card on the right.
		/// </summary>
		public override string ToString()
		{
			string result = "";
			for (int index = _cards.Count - 1; index >= 0; index--)
			{
				result += CardUtil.GetCardString(_cards[index]) + ((index > 0) ? " " : "");
			}

			return result;
		}

	}
}

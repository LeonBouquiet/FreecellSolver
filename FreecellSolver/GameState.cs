using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace FreecellSolver
{
	[DebuggerDisplay("{PriorityDescription}")]
	public class GameState: ICloneable
	{
		private List<int> _swapCells;
		private List<int> _foundations;
		private List<Cascade> _cascades;

		private int _emptyCascadeCount;
		private int _level;
		private int _priority;

		/// <summary>
		/// Gets the number of empty <see cref="Cascade"/>s in the GameState.
		/// </summary>
		public int EmptyCascadeCount
		{
			get { return _emptyCascadeCount; }
		}

		/// <summary>
		/// Gets a textual description of this game state.
		/// </summary>
		public string Description
		{
			get { return string.Join(Environment.NewLine, GetMultilineDescription()); }
		}

		/// <summary>
		/// Gets the contents of the 4 foundation cells (in the order Hearts, Spades, Diamonds, 
		/// Clubs). If a foundation cell is empty it contains the None card (0x00).
		/// </summary>
		[DebuggerDisplay("{CardUtil.GetCardString(_foundations)}")]
		public IList<int> Foundations
		{
			get { return _foundations; }
		}

		/// <summary>
		/// Gets the contents of the used swap cells (at most 4).
		/// </summary>
		[DebuggerDisplay("{CardUtil.GetCardString(_swapCells)}")]
		public IList<int> SwapCells
		{
			get { return _swapCells; }
		}

		/// <summary>
		/// Gets the 8 Cascades.
		/// </summary>
		public IList<Cascade> Cascades
		{
			get { return _cascades; }
		}

		/// <summary>
		/// Gets the sum of all <see cref="Cascade.Consecutiveness"/> values, indicating how much 
		/// the (remaining) cards are layed out consecutively. Theorethical range is [0, 48].
		/// </summary>
		public int Consecutiveness
		{
			get
			{
				int result = 0;
				foreach (Cascade cascade in _cascades)
					result += cascade.Consecutiveness;

				return result;
			}
		}

		/// <summary>
		/// Gets the number of cards placed on the <see cref="Foundations"/>, range is [0, 52].
		/// </summary>
		public int Completeness
		{
			get
			{
				int result = 0;

				//Masking out the Suit gives the rank of the card, in the range [0, 13], where 0 
				//means no card. Hence, this equals the number of cards in that particular foundation.
				foreach (int card in _foundations)
					result += (card & (int)Rank.Mask);

				return result;
			}
		}

		/// <summary>
		/// Gets the sum of the number of free cells and the number of empty cascades, range 
		/// is [0, 12].
		/// </summary>
		public int Availability
		{
			get { return _emptyCascadeCount + 4 - _swapCells.Count; }
		}

		/// <summary>
		/// Gets how many actions were taken to arrive at this state. The initial GameState is 
		/// level 0, and each pop and move adds 1 to the Level.
		/// </summary>
		public int Level
		{
			get { return _level; }
		}

		/// <summary>
		/// Gets the priority of this GameState: the lower, the more promising.
		/// </summary>
		public int Priority
		{
			get { return _priority; }
		}

		/// <summary>
		/// Gets a description containing the priority of this GameState and the parameters it is 
		/// composed of.
		/// </summary>
		public string PriorityDescription
		{
			get
			{
				return string.Format("Priority = {0,9} (Lvl={1}, CSec={2}, Cmpl={3}, Aval={4})",
					_priority, Level, Consecutiveness, Completeness, Availability);
			}
		}

		public bool IsSolved
		{
			get { return (Completeness == 52); }
		}

		/// <summary>
		/// Returns the lower bound of the cost (in Levels) that it will take to free all cards in 
		/// all Cascades.
		/// </summary>
		public int MinimumSolutionCost
		{
			get { return Cascades.Sum(c => c.MinimumSolutionCost); }
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="state"></param>
		private GameState(GameState state)
		{
			//Copy the swapcells and foundations...
			_swapCells = new List<int>(state._swapCells);
			_foundations = new List<int>(state._foundations);
			_level = state._level;

			//...and create a deep copy of the cascades
			_cascades = new List<Cascade>(8);
			foreach (Cascade cascade in state._cascades)
				_cascades.Add(new Cascade(cascade));

			_emptyCascadeCount = state._emptyCascadeCount;
			_priority = state._priority;
		}

		/// <summary>
		/// <see cref="ICloneable{T}"/> support.
		/// </summary>
		public object Clone()
		{
			GameState clone = new GameState(this);
			return clone;
		}

		/// <summary>
		/// Constructs a new GameState from the given information.
		/// </summary>
		/// <param name="swapCells">The contents of the used swapCells. May not contain the None card.</param>
		/// <param name="foundations">The top card of the foundation piles for Heart, Spade, 
		/// Diamond and Club suits, respectively. Its lenght is always 4; can contain the None card
		/// if the foundation is emtpy.</param>
		/// <param name="cascades">The 8 Cascades for this game state.</param>
		/// <param name="level">The level.</param>
		public GameState(List<int> swapCells, int[] foundations, Cascade[] cascades, int level)
		{
			_swapCells = swapCells;
			_foundations = new List<int>(foundations);
			_cascades = new List<Cascade>(cascades);
			_level = level;

			PerformSafeFoundationMoves(null);
		}

		public GameState CreateChildState(Move move, MoveLogger moveLogger)
		{
			//Clone this GameState and apply the move to it.
			GameState childState = (GameState)this.Clone();
			childState.ApplyMove(move, moveLogger, true);
			childState.PerformSafeFoundationMoves(moveLogger);

			return childState;
		}

		private void PerformSafeFoundationMoves(MoveLogger moveLogger)
		{
			//Perform as much safe foundation moves as possible. Because they are not performed by 
			//the 'user', they do not count towards the Level value.
			Move safeFoundationMove = MoveGenerator.GetSafeFoundationMove(this);
			while (safeFoundationMove != null)
			{
				ApplyMove(safeFoundationMove, moveLogger, false);
				safeFoundationMove = MoveGenerator.GetSafeFoundationMove(this);
			}

			//This may have affected the number of empty cascades and the priority - recalculate.
			Recalculate();
		}

		private void Recalculate()
		{
			_emptyCascadeCount = _cascades.Count(cs => cs.IsEmpty);

			//The calculated level value is based on the following four parameters. Next to each
			//parameter is their weight in the resulting priority.
			//  Consecutiveness	[0, 48]		 6 * Consecutiveness = [0, 288]
			//  Completeness	[0, 52]		10 * Completeness    = [0, 520]
			//  Availability	[0, 12]		20 * Availability    = [0, 240]
			//  Level			[0, ->]		-10 * Level
			_priority = 480 + (8 * Level) - (6 * Consecutiveness + 10 * Completeness + 20 * Availability);
		}

		public PackedGameState NormalizeAndPack(PackedGameState parent = null)
		{
			//This means sorting the swapcells and Cascades, so that game states with the same 
			//cascade configurations are recognised as being the same, even if the ordering in the 
			//cascade columns or swap cells differs.
			_swapCells.Sort();
			_cascades.Sort();

			//Note that this does not require a Recalculate, since none of the precalculated values 
			//are affected by these operations.
			PackedGameState packed = PackedGameState.Pack(this);
			packed.ParentState = parent;
			return packed;
		}

		/// <summary>
		/// Calculates and returns the size of the largest card sequence that can be moved in one
		/// (super)move for the current GameState.
		/// </summary>
		/// <param name="destIsEmptyCascade">True if the cards are to be moved to an empty cascade,
		/// false if the target is a non-empty Cascade.</param>
		/// <returns>The number of cards that can be moved in one supermove.</returns>
		public int GetMaxSequenceSizeToMove(bool destIsEmptyCascade)
		{
			return CardUtil.GetMaxSequenceSizeToMove(4 - _swapCells.Count, _emptyCascadeCount - (destIsEmptyCascade ? 1 : 0));
		}

		private void ApplyMove(Move move, MoveLogger moveLogger, bool updateLevel)
		{
			//Start by popping cards to the swap cells.
			if(move.Source.Area == Area.Cascade && move.Source.PopCount > 0)
			{
				if (moveLogger != null)
					moveLogger.LogMoveToSwapCells(move.Source.Index, _cascades[move.Source.Index], move.Source.PopCount);

				for (int sourcePopCount = 0; sourcePopCount < move.Source.PopCount; sourcePopCount++)
					_swapCells.Add(_cascades[move.Source.Index].RemoveTopCard());
			}

			if (move.Target.Area == Area.Cascade && move.Target.PopCount > 0)
			{
				if(moveLogger != null)
					moveLogger.LogMoveToSwapCells(move.Target.Index, _cascades[move.Target.Index], move.Target.PopCount);

				for (int targetPopCount = 0; targetPopCount < move.Target.PopCount; targetPopCount++)
					_swapCells.Add(_cascades[move.Target.Index].RemoveTopCard());
			}

			//Now do the actual move.
			if (move.Source.Area == Area.Cascade && move.Target.Area == Area.Cascade)
			{
				if(moveLogger != null)
					moveLogger.LogMoveToCascade(move.Source.Index, _cascades[move.Source.Index], move.SequenceLength, move.Target.Index);

				//Remove the sequence from the source Cascade and append it to the target.
				int[] cards = _cascades[move.Source.Index].RemoveSequence(move.SequenceLength);
				_cascades[move.Target.Index].AppendSequence(cards);
			}
			else
			{
				int card = RemoveSingleCard(move.Source.Area, move.Source.Card, move.Source.Index);
				if (moveLogger != null)
					moveLogger.LogMoveBetweenAreas(card, move.Source.Area, move.Source.Index, move.Target.Area, move.Target.Index, !updateLevel);

				AppendSingleCard(move.Target.Area, card, move.Target.Index);
			}

			//Finally, update the Level with the appropriate increment.
			if(updateLevel)
				_level += move.LevelIncrement;
		}

		private int RemoveSingleCard(Area area, int card, int cascadeIndex)
		{
			switch(area)
			{
				case Area.Cascade:
					return _cascades[cascadeIndex].RemoveTopCard();

				case Area.SwapCell:
					if (_swapCells.Remove(card) == false)
						throw new InvalidOperationException(string.Format("The card {0} is not stored in the SwapCells.", CardUtil.GetCardString(card)));

					return card;

				default:
					throw new InvalidOperationException(string.Format("Cannot remove cards from the {0} Area.", area));
			}
		}

		private void AppendSingleCard(Area area, int card, int cascadeIndex)
		{
			switch (area)
			{
				case Area.Cascade:
					_cascades[cascadeIndex].AppendTopCard(card);
					break;

				case Area.SwapCell:
					if (_swapCells.Count < 4)
						_swapCells.Add(card);
					else
						throw new InvalidOperationException(string.Format("Cannot append the card {0} to the swapcells because there are already 4 cards in it.", CardUtil.GetCardString(card)));
					break;

				case Area.Foundation:
					int foundationIndex = (card >> 4) - 1;
					if (CardUtil.CanBuildFoundation(_foundations[foundationIndex], card))
						_foundations[foundationIndex] = card;
					else
						throw new InvalidOperationException(string.Format("Cannot append the card {0} to the foundation card {1}.", 
							CardUtil.GetCardString(card), CardUtil.GetCardString(_foundations[foundationIndex])));
					break;
			}
		}

		public bool CanMoveToFoundation(int card)
		{
			int foundationIndex = (card >> 4) - 1;
			return CardUtil.CanBuildFoundation(_foundations[foundationIndex], card);
		}

		public IEnumerable<Location> GetMatchingCascades(int card)
		{
			for (int index = 0; index < 8; index++)
			{
				if (_cascades[index].IsEmpty || CardUtil.CanBuildFoundation(_cascades[index].Cards[0], card))
					yield return Location.InCascade(index);
			}
		}

		/// <summary>
		/// Verifies if cards are missing or if cards are duplicated. Also, makes sure that no 
		/// cascade contains None cards and that the foundations contain the correct suits.
		/// </summary>
		/// <exception cref="SolverException">When this GameState is invalid.</exception>
		private void Verify()
		{
			//We'll be collecting all cards that are in use in this HashSet.
			HashSet<int> cardsUsed = new HashSet<int>();

			//1. Verify that the foundations contain either the None card or are of the correct Suit.
			for (int index = 0; index < 4; index++)
			{
				if (_foundations[index] != 0x00)
				{
					//The current foundation is not empty. Get the Suit for it and verify that it 
					//is what we expect.
					Suit suit = (Suit)(_foundations[index] & (int)Suit.Mask);
					if (suit == CardUtil.Suits[index])
					{
						//For this suit, mark all cards between the Ace and the current rank as is use.
						for (int rank = (_foundations[index] & (int)Rank.Mask); rank >= (int)Rank.Ace; rank--)
							cardsUsed.Add(rank | (int)suit);
					}
					else
						throw new SolverException(string.Format("The foundation with index {0} has the wrong suit (expected: {1})", index, CardUtil.Suits[index]));
				}
			}

			//2. Register the cards that are on the swap cells
			foreach (int card in _swapCells)
			{
				if(cardsUsed.Add(card) == false)
					throw new SolverException(string.Format("The card {0} is used more than once.", CardUtil.GetCardString(card)));
			}

			//3. Walk through the cards of each Cascade in turn, and verify that there are no None 
			//cards in them.
			for (int column = 0; column < 8; column++)
			{
				foreach (int card in _cascades[column].Cards)
				{
					if (card == 0x00)
						throw new SolverException(string.Format("The cascade with index {0} has a None card in it.", column));

					//Mark this card as used.
					if (cardsUsed.Add(card) == false)
						throw new SolverException(string.Format("The card {0} is used more than once.", CardUtil.GetCardString(card)));
				}
			}

			//4. Verify that all 52 cards are in use.
			foreach (Suit suit in CardUtil.Suits)
			{
				foreach (Rank rank in CardUtil.Ranks)
				{
					if (cardsUsed.Remove((int)suit | (int)rank) == false)
						throw new SolverException(string.Format("The card {0} is not used.", CardUtil.GetCardString((int)suit | (int)rank)));
				}
			}

			//5. And that there are no more cards than this.
			if (cardsUsed.Count > 0)
				throw new SolverException("One or more of the specified cards are invalid.");
		}

		/// <summary>
		/// Parses the <paramref name="cascadeText"/> into a GameState
		/// </summary>
		/// <param name="cascadeText">Textual row-major description of the 8 cascades.
		/// The first line contains the cells and the foundations. The bottom card on must be on 
		/// the second line and the top card on the last line. Each row must be 
		/// delimited by a newline, and each card must take up exactly 4 characters, with the 
		/// middle two containing the suit and rank, respectively.</param>
		/// <remarks>
		/// Example: <code>
		/// "(..)(..)(..)(..)&lt;..&gt;&lt;..&gt;&lt;..&gt;&lt;..&gt;\n" +
		/// " d9  s5  sJ  hJ  d6  hK  c4  h6 \n" +
		/// " h8  sA  s6  hT  cA  sT  d8  c8 \n" +
		/// " c2  h3  s4  s8  s2  sK  h7  d2 \n" +
		/// " d3  h9  d5  dK  dJ  s9  c3  c5 \n" +
		/// " cK  cJ  c7  cQ  hQ  s7  sQ  h2 \n" +
		/// " h5  c6  d4  hA  s3  d7  dT  dA \n" +
		/// " cT  d9  h4  dQ\n";
		/// </code>
		/// </remarks>
		/// <exception cref="FormatException">On error parsing the text data.</exception>
		/// <exception cref="SolverException">When the text data describes an invalid game state.</exception>
		public static GameState Parse(string cascadeText)
		{
			string cardText;
			List<int> swapCells = new List<int>();
			int[] foundations = new int[4];
			Cascade[] cascades = new Cascade[8];
			string[] lines = cascadeText.Replace(Environment.NewLine, "\n").Split('\n');

			try
			{
				//Verify that the first line contains the swap cells and foundations, respectively...
				if (lines[0].Length < 32 || lines[0].StartsWith("(") == false || lines[0].EndsWith(">") == false)
					throw new FormatException("The first line of the cascadeText must contain the swap cells and foundations.");

				//...and parse their contents.
				for (int cellIndex = 0; cellIndex < 4; cellIndex++)
				{
					int card = CardUtil.ParseCard(lines[0].Substring(cellIndex * 4 + 1, 2));
					if (card != 0x00)
						swapCells.Add(card);
				}

				for (int foundationIndex = 0; foundationIndex < 4; foundationIndex++)
					foundations[foundationIndex] = CardUtil.ParseCard(lines[0].Substring(16 + foundationIndex * 4 + 1, 2));

				//Now parse the cascades.
				for (int column = 0; column < 8; column++)
				{
					//Compose the list of cards in this column, possibly including the 'None' card (0x00).
					List<int> cards = new List<int>();
					for (int row = 1; row < lines.Length; row++)
					{
						cardText = "..";
						if (lines[row].Length >= (column * 4) + 3)
							cardText = lines[row].Substring(column * 4 + 1, 2);

						cards.Add(CardUtil.ParseCard(cardText));
					}

					//None cards could have occured at the end of the list. Trim these None cards.
					while ((cards.Count > 0) && (cards[cards.Count - 1] == 0x00))
						cards.RemoveAt(cards.Count - 1);

					//Don't forget to reverse the sequence: the top card should be on index 0.
					cards.Reverse();
					cascades[column] = new Cascade(cards);
				}
			}
			catch (ArgumentException ae)
			{
				//The CardUtil.ParseXxx() methods throw ArgumentExceptions on error. Convert these
				//to FormatExceptions instead.
				throw new FormatException("Error parsing card: " + ae.Message, ae);
			}

			//Construct the GameState from the parsed data. 
			GameState gamestate = new GameState(swapCells, foundations, cascades, 0);

			//At this point, we haven't verified if cards are missing or if cards are duplicated.
			//Also, it's possible that the cascades contain None cards, or that the foundations
			//do not contain the correct suits. Verify these error situations before returning.
			gamestate.Verify();
			return gamestate;
		}

		/// <summary>
		/// Composes and returns a multiline description of this game state.
		/// </summary>
		public string[] GetMultilineDescription()
		{
			//As the first two lines, add the priority description.
			List<string> result = new List<string>();
			result.Add(string.Format("Lvl={0,4} CSec={1,2} Cmpl={2,2} MnRq={3,2}", Level, Consecutiveness, Completeness, MinimumSolutionCost));
			result.Add(string.Format("----- Priority = {0,9} -----", _priority));

			//Get descriptions for the swap cells and foundations.
			StringBuilder sb = new StringBuilder();
			foreach (int card in _swapCells)
				sb.AppendFormat("({0})", CardUtil.GetCardString(card));
			for (int index = _swapCells.Count; index < 4; index++)
				sb.Append("(..)");
			foreach (int card in _foundations)
				sb.AppendFormat("<{0}>", CardUtil.GetCardString(card));
			result.Add(sb.ToString());

			//Now get the multiline descriptions for the 8 Cascades, and determine what the 
			//maximum row count is.
			string[][] cascades = new string[8][];
			int maxRowCount = 0;
			for (int index = 0; index < 8; index++)
			{
				cascades[index] = _cascades[index].GetMultilineDescription();
				maxRowCount = Math.Max(maxRowCount, cascades[index].Length);
			}

			//Now combine the information of these 8 cascades into lines.
			for(int row = 0; row < maxRowCount; row++)
			{
				StringBuilder sbRow = new StringBuilder();
				for (int column = 0; column < 8; column++)
				{
					if (row < cascades[column].Length)
						sbRow.Append(cascades[column][row]);
					else
						sbRow.Append("    ");
				}

				result.Add(sbRow.ToString());
			}

			return result.ToArray();
		}
	}
}

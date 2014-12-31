using System;
using System.Collections.Generic;
using System.Text;

namespace FreecellSolver
{

	/// From		To			Allowed		Max.Seq.	Pop src		Pop target	Method
	/// Foundation	Foundation	No
	/// Foundation	SwapCell	No
	/// Foundation	Cascade		No
	/// SwapCell	Foundation	Yes			1			0			0			FromSwapCellsToFoundations
	/// SwapCell	SwapCell	No	
	/// SwapCell	Cascade		Yes			1			0			n			FromSwapCellsToCascades
	/// Cascade		Foundation	Yes			1			n			0			FromCascadesToFoundations
	/// Cascade		SwapCell	No(!)											(This is done by popping the source/target prior to move)
	/// Cascade		Cascade		Yes			n			n			n			BetweenCascades

	public class MoveGenerator
	{
		private GameState _gameState;
		private List<Move> _moves;

		private MoveGenerator(GameState gameState)
		{
			_gameState = gameState;
			_moves = new List<Move>();
		}

		private void FromSwapCellsToFoundations()
		{
			//Attempt to move each card in the SwapCells collection to its corresponding foundation.
			foreach (int card in _gameState.SwapCells)
			{
				int foundationCard = _gameState.Foundations[(card >> 4) - 1];
				if (CardUtil.CanBuildFoundation(foundationCard, card))
				{
					Location sourceLocation = new Location(Area.SwapCell, card);
					Location targetLocation = new Location(Area.Foundation);
					_moves.Add(new Move(sourceLocation, targetLocation));
				}
			}
		}

		private void FromSwapCellsToCascades()
		{
			//Attempt to move each card in the SwapCells collection to a cascade.
			foreach (int card in _gameState.SwapCells)
			{
				for (int index = 0; index < 8; index++)
				{
					//Based on the number of free swap cells available and the number of cards 
					//in the current Cascade, determine the maximum number of cards that can be
					//popped to the swap cells before making the 'real' move: moving the current
					//swap cell card to a cascade.
					Cascade cascade = _gameState.Cascades[index];
					int maxCardsToPop = Math.Min(4 - _gameState.SwapCells.Count, cascade.Cards.Count);

					for (int popCount = 0; popCount <= maxCardsToPop; popCount++)
					{
						//If, after popping, either the cascade is empty or the current swap cell 
						//card can be placed onto the cascade, it's a valid move.
						if (popCount == cascade.Cards.Count || CardUtil.CanBuildCascade(cascade.Cards[popCount], card))
						{
							Location sourceLocation = new Location(Area.SwapCell, card);
							Location targetLocation = new Location(Area.Cascade, index, popCount);
							_moves.Add(new Move(sourceLocation, targetLocation));
						}
					}
				}
			}
		}

		private void FromCascadesToFoundations()
		{
			for (int index = 0; index < 8; index++)
			{
				//Based on the number of free swap cells available and the number of cards in the 
				//current Cascade, determine the maximum number of cards that can be popped to the 
				//swap cells before making the 'real' move: moving the top card to its foundation.
				Cascade cascade = _gameState.Cascades[index];
				int maxCardsToPop = Math.Min(4 - _gameState.SwapCells.Count, cascade.Cards.Count - 1);

				//Note that the cascade is the source, so we don't pop the cascade empty (we 
				//always leave at least one card).
				for (int popCount = 0; popCount <= maxCardsToPop; popCount++)
				{
					//If, after popping, the card at the top of the cascade can be moved to its
					//foundation, it's a valid move.
					int card = cascade.Cards[popCount];
					int foundationCard = _gameState.Foundations[(card >> 4) - 1];

					if (CardUtil.CanBuildFoundation(foundationCard, card))
					{
						Location sourceLocation = new Location(Area.Cascade, index, popCount);
						Location targetLocation = new Location(Area.Foundation);
						_moves.Add(new Move(sourceLocation, targetLocation));
					}
				}
			}
		}

		private void BetweenCascades()
		{
			for (int sourceIndex = 0; sourceIndex < 8; sourceIndex++)
			{
				//Determine the max. cards to pop from the source Cascade; we'll always leave at 
				//least one card.
				Cascade source = _gameState.Cascades[sourceIndex];
				int maxSourceCardsToPop = Math.Min(4 - _gameState.SwapCells.Count, source.Cards.Count - 1);

				for (int sourcePopCount = 0; sourcePopCount <= maxSourceCardsToPop; sourcePopCount++)
				{
					//Determine, for this scope, the number of swap cells left after popping.
					int swapCellsLeft = 4 - _gameState.SwapCells.Count - sourcePopCount;

					//Also, determine the length of the sequence in the source Cascade that starts 
					//after popping the current nr. of cards. (This is the number of cards we could 
					//theoretically move if there are enough empty Cascades available.)
					int sequenceLength = source.GetSequenceLength(sourcePopCount);		//At least 1.

					//Enumerate over all target Cascades...
					for (int targetIndex = 0; targetIndex < 8; targetIndex++)
					{
						//...but handle the case where we move a sequence within the same Cascade elsewhere.
						if (targetIndex == sourceIndex)
							continue;

						//Determine the max. cards to pop from the target Cascade
						Cascade target = _gameState.Cascades[targetIndex];
						int maxTargetCardsToPop = Math.Min(swapCellsLeft, target.Cards.Count);

						for (int targetPopCount = 0; targetPopCount <= maxTargetCardsToPop; targetPopCount++)
						{
							int maxCardsToMove = 1;
							if (sequenceLength > 1)
							{
								//Count the number of empty cascades we have so we can determine 
								//the maximum sequence length we can move. 
								//Note that, for the purposes of this calculation, the target cascase 
								//does not count as empty, so it should be excluded.
								bool isTargetCascadeEmpty = (target.Cards.Count - targetPopCount) == 0;
								int emptyCascadeCount = _gameState.EmptyCascadeCount - (isTargetCascadeEmpty ? 1 : 0);

								//(Note that the source can never be empty, and even if the target
								//is now empty due to popping, it does not count as an empty Cascade
								//because it cannot be used for a supermove).
								maxCardsToMove = CardUtil.GetMaxSequenceSizeToMove(
									swapCellsLeft - targetPopCount, 
									emptyCascadeCount);

								//The max. number of cards we can move is still delimited by the 
								//actual sequence length, obviously.
								maxCardsToMove = Math.Min(maxCardsToMove, sequenceLength);
							}

							//Attempt to move a sequence of length in the range [0, maxCardsToMove] 
							//to the target Cascade.
							if (target.Cards.Count > targetPopCount)
							{
								//Determine the top card in the target Cascade after popping...
								int topCard = target.Cards[targetPopCount];

								//...and see if we can find a card in the sequence that can be placed 
								//on top of this card. Due to the nature of a sequence, we can find 
								//at most one card.
								for (int length = maxCardsToMove; length > 0; length--)
								{
									if (CardUtil.CanBuildCascade(topCard, source.Cards[sourcePopCount + length - 1]))
									{
										Location sourceLocation = new Location(Area.Cascade, sourceIndex, sourcePopCount);
										Location targetLocation = new Location(Area.Cascade, targetIndex, targetPopCount);
										_moves.Add(new Move(sourceLocation, targetLocation, length));
										break;
									}
								}
							}
							else
							{
								//The target Cascade is empty, so every length is a valid move.
								for (int length = maxCardsToMove; length > 0; length--)
								{
									Location sourceLocation = new Location(Area.Cascade, sourceIndex, sourcePopCount);
									Location targetLocation = new Location(Area.Cascade, targetIndex, targetPopCount);
									_moves.Add(new Move(sourceLocation, targetLocation, length));
								}
							}
						}
					}

					//Finally, handle the case where we move a sequence within the same cascade.
				}
			}
		}


		public static Move[] Generate(GameState gameState)
		{
			MoveGenerator generator = new MoveGenerator(gameState);
			generator.FromSwapCellsToFoundations();
			generator.FromCascadesToFoundations();
			generator.FromSwapCellsToCascades();
			generator.BetweenCascades();

			return generator._moves.ToArray();
		}

		public static Move GetSafeFoundationMove(GameState gameState)
		{
			MoveGenerator generator = new MoveGenerator(gameState);
			generator.FromSwapCellsToFoundations();
			generator.FromCascadesToFoundations();

			foreach (Move move in generator._moves)
			{
				//A safe move never involves popping (and because the target Area is Foundation,
				//it never involves a pop)
				if (move.Source.PopCount > 0)
					continue;

				//Get the card that is moved to its foundation by this Move.
				int sourceCard = (move.Source.Area == Area.Cascade)
					? gameState.Cascades[move.Source.Index].Cards[move.Source.PopCount]
					: move.Source.Card;

				//If it's an Ace or 2 it's always a safe move.
				Rank rank = (Rank)(sourceCard & (int)Rank.Mask);
				if (rank == Rank.Ace || rank == Rank.Two)
					return move;

				//Otherwise, we require that for both opposite Suits, the Ranks one less than the 
				//sourceCard are already on their foundation.
				//E.g. if the sourceCard is 4h, both 3s and 3c must be on their foundations.
				int offset = ((sourceCard & (int)Suit.RedMask) != 0) ? 1 : 0;

				int foundationCard = gameState.Foundations[0 + offset];
				if (foundationCard == 0x00 || ((int)rank - (foundationCard & (int)Rank.Mask)) > 1)
					continue;
				foundationCard = gameState.Foundations[2 + offset];
				if (foundationCard == 0x00 || ((int)rank - (foundationCard & (int)Rank.Mask)) > 1)
					continue;

				return move;
			}

			return null;
		}
	}
}

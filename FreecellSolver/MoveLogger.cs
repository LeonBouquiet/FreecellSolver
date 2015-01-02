using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreecellSolver
{
	public class MoveLogger
	{
		public List<MoveDescription> Messages { get; private set; }

		public MoveLogger()
		{
			Messages = new List<MoveDescription>();
		}

		public void Log(int moveIncrement, string format, params object[] args)
		{
			string text = string.Format(format, args);
			Messages.Add(new MoveDescription(moveIncrement, text));
		}

		public void LogMoveToSwapCells(int cascadeIndex, Cascade cascade, int cardCount)
		{
			if (cardCount == 1)
			{
				string cardString = CardUtil.GetCardString(cascade.GetTopNCards(1));
				Log(1, "Move the card \"{0}\" from cascade {1} to a swap cell.", cardString, cascadeIndex + 1);
			}
			else
			{
				string cardString = CardUtil.GetCardString(cascade.GetTopNCards(cardCount));
				Log(cardCount, "Move the {0} cards \"{1}\" from cascade {2} to the swap cells.", cardCount, cardString, cascadeIndex + 1);
			}
		}

		public void LogMoveToCascade(int sourceCascadeIndex, Cascade sourceCascade, int cardCount, int targetCascadeIndex)
		{
			if (cardCount == 1)
			{
				string cardString = CardUtil.GetCardString(sourceCascade.GetTopNCards(1));
				Log(1, "Move the card \"{0}\" from cascade {1} to cascade {2}.", cardString, sourceCascadeIndex + 1, targetCascadeIndex + 1);
			}
			else
			{
				string cardString = CardUtil.GetCardString(sourceCascade.GetTopNCards(cardCount));
				Log(1, "Move the {0} cards \"{1}\" from cascade {2} to cascade {3}.", cardCount, cardString, sourceCascadeIndex + 1, targetCascadeIndex + 1);
			}
		}

		public void LogMoveBetweenAreas(int card, Area sourceArea, int sourceIndex, Area targetArea, int targetIndex, bool isSafeMove)
		{
			string sourceDesc = GetAreaDescription(sourceArea, sourceIndex);
			string targetDesc = GetAreaDescription(targetArea, targetIndex);
			string annotation = isSafeMove ? " (*)" : string.Empty;

			int moveCount = isSafeMove ? 0 : 1;
			Log(moveCount, "Move the card \"{0}\" from {1} to {2}.{3}", CardUtil.GetCardString(card), sourceDesc, targetDesc, annotation);
		}

		private string GetAreaDescription(Area area, int index)
		{
			switch(area)
			{
				case Area.Cascade:
					return string.Format("cascade {0}", index + 1);
				case Area.SwapCell:
					return "swap cell";
				case Area.Foundation:
					return "its foundation";
				default:
					throw new ArgumentException(string.Format("Unknown Area type \"{0}\".", area));
			}
		}
	}
}

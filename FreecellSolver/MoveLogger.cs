using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreecellSolver
{
	public class MoveLogger
	{
		public List<string> Messages { get; private set; }

		public MoveLogger()
		{
			Messages = new List<string>();
		}

		public void Log(string format, params object[] args)
		{
			string message = string.Format(format, args);
			Messages.Add(message);
		}

		public void LogMoveToSwapCells(int cascadeIndex, Cascade cascade, int cardCount)
		{
			if (cardCount == 1)
			{
				string cardString = CardUtil.GetCardString(cascade.GetTopNCards(1));
				Log("Move the card \"{0}\" from cascade {1} to a swap cell.", cardString, cascadeIndex + 1);
			}
			else
			{
				string cardString = CardUtil.GetCardString(cascade.GetTopNCards(cardCount));
				Log("Move the {0} cards \"{1}\" from cascade {2} to the swap cells.", cardCount, cardString, cascadeIndex + 1);
			}
		}

		public void LogMoveToCascade(int sourceCascadeIndex, Cascade sourceCascade, int cardCount, int targetCascadeIndex)
		{
			if (cardCount == 1)
			{
				string cardString = CardUtil.GetCardString(sourceCascade.GetTopNCards(1));
				Log("Move the card \"{0}\" from cascade {1} to cascade {2}.", cardString, sourceCascadeIndex + 1, targetCascadeIndex + 1);
			}
			else
			{
				string cardString = CardUtil.GetCardString(sourceCascade.GetTopNCards(cardCount));
				Log("Move the {0} cards \"{1}\" from cascade {2} to cascade {3}.", cardCount, cardString, sourceCascadeIndex + 1, targetCascadeIndex + 1);
			}
		}

		public void LogMoveBetweenAreas(int card, Area sourceArea, int sourceIndex, Area targetArea, int targetIndex)
		{
			string sourceDesc = GetAreaDescription(sourceArea, sourceIndex);
			string targetDesc = GetAreaDescription(targetArea, targetIndex);

			Log("Move the card \"{0}\" from {1} to {2}.", CardUtil.GetCardString(card), sourceDesc, targetDesc);
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

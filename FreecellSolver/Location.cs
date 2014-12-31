using System;
using System.Collections.Generic;
using System.Text;

namespace FreecellSolver
{
	public enum Area
	{
		SwapCell,
		Foundation,
		Cascade
	}

	public struct Location
	{
		private Area _area;
		private int _card;
		private int _index;
		private int _popCount;

		public Area Area
		{
			get { return _area; }
			set { _area = value; }
		}

		/// <summary>
		/// The card to move (used when <see cref="Area"/> = SwapCell or Area = Foundation. In
		/// both cases, <see cref="PopCount"/> = 0 and <see cref="SequenceLength"/> = 1).
		/// </summary>
		public int Card
		{
			get { return _card; }
			set { _card = value; }
		}

		/// <summary>
		/// The index of the Cascade (used when <see cref="Area"/> = Cascade).
		/// </summary>
		public int Index
		{
			get { return _index; }
			set { _index = value; }
		}

		/// <summary>
		/// The number of cards to remove from the top of the cascade before moving the sequence.
		/// </summary>
		public int PopCount
		{
			get { return _popCount; }
			set { _popCount = value; }
		}

		public Location(Area area)
		{
			_area = area;
			_card = -1;
			_index = Int32.MaxValue;
			_popCount = 0;
		}

		public Location(Area area, int card)
		{
			_area = area;
			_card = card;
			_index = Int32.MaxValue;
			_popCount = 0;
		}

		public Location(Area area, int index, int popCount)
		{
			_area = area;
			_card = Int32.MaxValue;
			_index = index;
			_popCount = popCount;
		}

		public static Location InSwapCell(int card)
		{
			return new Location(Area.SwapCell, card);
		}

		public static Location InFoundation(int card)
		{
			return new Location(Area.Foundation, card);
		}

		public static Location InCascade(int index)
		{
			return new Location(Area.Cascade, index);
		}
	}

}
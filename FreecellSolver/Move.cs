using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreecellSolver
{
	public class Move
	{
		private Location _source;
		private Location _target;

		public Location Source
		{
			get { return _source; }
			set { _source = value; }
		}

		public Location Target
		{
			get { return _target; }
			set { _target = value; }
		}

		/// <summary>
		/// Gets with how much the <see cref="GameState.Level"/> is increased by applying this move.
		/// The <see cref="Move"/> itself counts as 1, but each pop also increments the level by one.
		/// Range is [1, 5].
		/// </summary>
		public int LevelIncrement
		{
			get { return 1 + _source.PopCount + _target.PopCount; }
		}

		public Move(Location source, Location target)
		{
			_source = source;
			_target = target;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// [PopSrc:0, PopTgt:1] Move 1 card from Cascade 1 to Cascade 7.
		/// [PopSrc:0, PopTgt:0] Move 1 card from Cascade 1 to Foundation.
		/// [PopSrc:0, PopTgt:0] Move 1 card from Cascade 1 to SwapCell.
		/// [PopSrc:0, PopTgt:0] Move card d3 from SwapCell to Cascade 1.
		/// </remarks>
		public override string ToString()
		{
			string desc = "[<pops>] Move <card> from <source> to <target>.";
			desc = desc.Replace("<pops>", string.Format("PopSrc:{0}, PopTgt:{1}", _source.PopCount, _target.PopCount));

			if (_source.Area == Area.Cascade)
				desc = desc.Replace("<card>", string.Format("{0} card{1}", _source.SequenceLength, (_source.SequenceLength > 1) ? "s" : ""));
			else
				desc = desc.Replace("<card>", string.Format("card {0}", CardUtil.GetCardString(_source.Card)));

			if (_source.Area == Area.Cascade)
				desc = desc.Replace("<source>", string.Format("Cascade {0}", _source.Index));
			else
				desc = desc.Replace("<source>", _source.Area.ToString());

			if (_target.Area == Area.Cascade)
				desc = desc.Replace("<target>", string.Format("Cascade {0}", _target.Index));
			else
				desc = desc.Replace("<target>", _target.Area.ToString());

			return desc;
		}
	}
}

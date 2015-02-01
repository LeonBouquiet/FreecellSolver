using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Security.Cryptography;

namespace FreecellSolver
{
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>Each card-entry is stored as an 8-bit value. Each cascade is delimited by the 
	/// value 0xFF. The sequence stored is 4 swapcells, 4 foundations, 8 cascades (with each 
	/// cascade followed by the delimiter 0xFF, except for the last one).</remarks>
	public class PackedGameState
	{
		private class DictionaryComparerImpl : IEqualityComparer<PackedGameState>
		{
			public bool Equals(PackedGameState left, PackedGameState right)
			{
				if (left == null && right == null)
					return true;

				if (left == null || right == null)
					return false;

				if (left._hash != right._hash || left._bytes.Length != right._bytes.Length)
					return false;

				for (int index = 0; index < left._bytes.Length; index++)
				{
					if (left._bytes[index] != right._bytes[index])
						return false;
				}

				return true;
			}

			public int GetHashCode(PackedGameState pgs)
			{
				return pgs._hash;
			}
		}

		private class PriorityQueueComparerImpl : IComparer<PackedGameState>
		{
			public int Compare(PackedGameState left, PackedGameState right)
			{
				if (left == null && right == null)
					return 0;
				if (left == null || right == null)
					return (left == null) ? -1 : 1;

				if (left.Priority != right.Priority)
					return left.Priority - right.Priority;

				if (left._hash != right._hash)
					return left._hash - right._hash;
				if (left._bytes.Length != right._bytes.Length)
					return left._bytes.Length - right._bytes.Length;

				for (int index = 0; index < left._bytes.Length; index++)
				{
					if (left._bytes[index] != right._bytes[index])
						return left._bytes[index] - right._bytes[index];
				}

				if (left.Level != right.Level)
					return left.Level - right.Level;

				return left.GetObjectHashCode() - right.GetObjectHashCode();
			}
		}

		/// <summary>
		/// Considers two PackedGameStates to be equal when they have the same hash code and bytes 
		/// (i.e. the same card positions). Other values, such as Priority or Level do not matter.
		/// Use this for duplicate detection.
		/// </summary>
		public static readonly IEqualityComparer<PackedGameState> DictionaryComparer = new DictionaryComparerImpl();

		/// <summary>
		/// Defines a complete ordering over PackedGameStates, based on Priority, bytes, Level and 
		/// GetObjectHashCode(). Use this to sort PackedGameStates in a priority queue.
		/// The fact that the ordering is complete is important, since PriorityQueue.Remove() will
		/// use this same Comparer to determine if a PackedGameState from the queue matches the one 
		/// in the removal list(!).
		/// </summary>
		public static readonly IComparer<PackedGameState> PriorityQueueComparer = new PriorityQueueComparerImpl();

		private const byte Delimiter = 0xFF;

		private int _level;
		private int _priority;
		private int _minimumSolutionCost;
		private byte[] _bytes;
		private int _hash;

		public PackedGameState ParentState { get; set; }

		public int Level
		{
			get { return _level; }
		}

		public int Priority
		{
			get { return _priority; }
		}

		public int MinimumSolutionCost
		{
			get { return _minimumSolutionCost; }
		}

		public int ByteCount
		{
			get { return _bytes.Length + 4; }
		}

		/// <summary>
		/// Returns all PackedGameState's ParentStates, starting with this one and ending with the root.
		/// </summary>
		public List<PackedGameState> PathToRoot
		{
			get
			{
				List<PackedGameState> result = new List<PackedGameState>();
				for (PackedGameState current = this; current.ParentState != null; current = current.ParentState)
					result.Add(current);

				return result;
			}
		}

		private PackedGameState(int level, int priority, int minimumSolutionCost, byte[] bytes, int hash)
		{
			_level = level;
			_priority = priority;
			_minimumSolutionCost = minimumSolutionCost;
			_bytes = bytes;
			_hash = hash;
		}

		public override bool Equals(object obj)
		{
			PackedGameState other = obj as PackedGameState;
			if (other == null)
				return false;

			if (this._hash != other._hash || this._bytes.Length != other._bytes.Length)
				return false;

			for (int index = 0; index < _bytes.Length; index++)
			{
				if (this._bytes[index] != other._bytes[index])
					return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			return _hash;
		}

		public int GetObjectHashCode()
		{
			int result = base.GetHashCode();
			return result;
		}

		private static MD5 _md5 = MD5.Create();

		public static PackedGameState Pack(GameState gameState)
		{
			List<byte> bytes = new List<byte>(70);

			//Write the swapcell contents, and pad with the None card if necessary.
			IList<int> swapCells = gameState.SwapCells;
			for (int index = 0; index < swapCells.Count; index++)
				bytes.Add((byte)swapCells[index]);
			for (int index = swapCells.Count; index < 4; index++)
				bytes.Add(0x00);

			//Append the Foundation contents (always 4 cards)
			foreach(int card in gameState.Foundations)
				bytes.Add((byte)card);

			//Write each of the cascades in turn. Delimit each Cascade with the Delimiter.
			foreach (Cascade cascade in gameState.Cascades)
			{
				foreach (int card in cascade.Cards)
					bytes.Add((byte)card);

				bytes.Add(Delimiter);
			}

			//Compute a 16-byte MD5 hash for these bytes.
			byte[] gameStateBytes = bytes.ToArray();
			byte[] hashBytes = _md5.ComputeHash(gameStateBytes);

			//Compress these into a single 31-bit UInt, so we can use it.
			int hash = 0x0;
			for (int block = 0; block < 4; block++)
			{
				hash ^=
					(hashBytes[block * 4]) | (hashBytes[block * 4 + 1] << 8) |
					(hashBytes[block * 4 + 2] << 16) ^ (hashBytes[block * 4 + 3] << 23);
			}

			return new PackedGameState(gameState.Level, gameState.Priority, gameState.MinimumSolutionCost, gameStateBytes, hash);
		}


		public GameState Unpack()
		{
			//The swapcells come first and are always 4 bytes.
			List<int> swapCells = _bytes.Take(4)
				.Where(b => b != 0x00)
				.Select(b => (int)b)
				.ToList();

			//Next come the 4 foundation cards.
			int[] foundations = _bytes.Skip(4).Take(4)
				.Select(b => (int)b)
				.ToArray();

			//Finally, the 8 cascades, each one ending in a Delimiter byte.
			List<Cascade> cascades = new List<Cascade>();
			List<int> currentCascadeCards = new List<int>();
			for(int index = 8; index < _bytes.Length; index++)
			{
				if (_bytes[index] != Delimiter)
				{
					currentCascadeCards.Add(_bytes[index]);
				}
				else
				{
					cascades.Add(new Cascade(currentCascadeCards));
					currentCascadeCards.Clear();
				}
			}

			GameState gameState = new GameState(swapCells, foundations, cascades.ToArray(), _level);
			return gameState;
		}
	}
}

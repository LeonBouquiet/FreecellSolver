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

		public bool Breakpoint { get; set; }

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

		private PackedGameState(int level, int priority, int minimumSolutionCost, byte[] bytes, int hash, bool breakpoint)
		{
			_level = level;
			_priority = priority;
			_minimumSolutionCost = minimumSolutionCost;
			_bytes = bytes;
			_hash = hash;
			Breakpoint = breakpoint;
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

			return new PackedGameState(gameState.Level, gameState.Priority, gameState.MinimumSolutionCost, gameStateBytes, hash, gameState.Breakpoint);
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

			GameState gameState = new GameState(swapCells, foundations, cascades.ToArray(), _level, Breakpoint);
			return gameState;
		}
	}
}

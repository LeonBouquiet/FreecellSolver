using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FreecellSolver
{
	public class LevelStatistics
	{
		public int Processed { get; set; }

		public int ChildStatesGenerated { get; set; }

		public int ChildStatesPruned { get; set; }

		public int ChildStatesDuplicates { get; set; }

		public int ChildStatesRemaining
		{
			get { return ChildStatesGenerated - ChildStatesPruned - ChildStatesDuplicates; }
		}

		public double RawBranchingFactor
		{
			get { return ChildStatesGenerated / (double)Processed; }
		}

		public double BranchingFactor
		{
			get { return ChildStatesRemaining / (double)Processed; }
		}

		public override string ToString()
		{
			return string.Format("GameStates: {0}, Child states: {1}, Remaining {2}, Branching factor: {3:N2}.",
				Processed, ChildStatesGenerated, ChildStatesRemaining, BranchingFactor);
		}
	}


	public class Statistics
	{
		public enum EntryType
		{
			Info,
			Progress,
			Solution,
			Result
		}

		private class Entry
		{
			public EntryType Type;
			public TimeSpan Offset;
			public long Queued;
			public long Processed;
			public long Pruned;
			public long Improved;

			public string Message;

			public override string ToString()
			{
				return string.Format("[{0:hh\\:mm\\:ss}] {1}", Offset, Message);
			}
		}

		private List<Entry> _entries;

		private DateTime? _stopTime;

		private List<LevelStatistics> _levelStatistics;

		public string Name { get; set; }

		public DateTime StartTime { get; set; }

		public long ProcessCount { get; set; }

		public long PruneCount { get; set; }

		public long ImproveCount { get; set; }

		public Func<long> GetQueueCountFunc { get; set; }

		public TimeSpan ElapsedTime
		{
			get
			{
				DateTime effectiveNow = _stopTime ?? DateTime.Now;
				return effectiveNow - StartTime;
			}
		}

		public LevelStatistics this[int level]
		{
			get
			{
				while(_levelStatistics.Count <= level)
					_levelStatistics.Add(new LevelStatistics());

				return _levelStatistics[level];
			}
		}

		public Statistics()
		{
			_entries = new List<Entry>();
			_levelStatistics = new List<LevelStatistics>();
			StartTime = DateTime.Now;
		}

		public void Initialize(Func<long> getQueueCountFunc)
		{
			StartTime = DateTime.Now;
			_stopTime = null;
			GetQueueCountFunc = getQueueCountFunc;
			ProcessCount = 0;
			PruneCount = 0;
			ImproveCount = 0;
		}

		public void StopTimer()
		{
			_stopTime = DateTime.Now;
		}

		public void LogProgress()
		{
			Entry entry = CreateEntry(EntryType.Progress, null);
			_entries.Add(entry);

			Console.WriteLine("[{0:hh\\:mm\\:ss}] {1,8} processed, {2,8} queued, {3} pruned, {4} improved.", entry.Offset, entry.Processed, entry.Queued, entry.Pruned, entry.Improved);
		}

		public void LogInfo(string format, params object[] args)
		{
			string message = (args.Length > 0)
				? string.Format(CultureInfo.InvariantCulture, format, args)
				: format;

			Entry entry = CreateEntry(EntryType.Info, message);
			_entries.Add(entry);

			Console.WriteLine("[{0:hh\\:mm\\:ss}] {1}", entry.Offset, message);
		}

		public void LogEventWithoutNewLine(string format, params object[] args)
		{
			string message = (args.Length > 0)
				? string.Format(CultureInfo.InvariantCulture, format, args)
				: format;

			Entry entry = CreateEntry(EntryType.Solution, message);
			_entries.Add(entry);

			Console.Write("[{0:hh\\:mm\\:ss}] {1}", entry.Offset, message);
		}

		public void LogEventAddition(string format, params object[] args)
		{
			string message = (args.Length > 0)
				? string.Format(CultureInfo.InvariantCulture, format, args)
				: format;

			_entries.Last().Message += message;

			Console.WriteLine(message);
		}

		public void LogResult(string solutionSteps)
		{
			Entry entry = CreateEntry(EntryType.Result, solutionSteps);
			_entries.Add(entry);

			Console.Write(solutionSteps);
		}

		private Entry CreateEntry(EntryType type, string message)
		{
			Entry result = new Entry();
			result.Type = type;
			result.Offset = ElapsedTime;
			result.Processed = ProcessCount;
			result.Pruned = PruneCount;
			result.Improved = ImproveCount;
			result.Queued = (GetQueueCountFunc != null) ? GetQueueCountFunc() : 0;
			result.Message = message;

			return result;
		}

		public void Save()
		{
			Save(string.Format("{0}.xml", Name));
		}

		public void Save(string xmlPath)
		{
			List<XElement> entryElts = _entries
				.Select(e => CreateEntryElement(e))
				.ToList();

			List<XElement> statisticsElts = _levelStatistics
				.Select((ls, index) => CreateStatisticsElement(index, ls))
				.ToList();

			XElement levelStatisticsElt = new XElement("levelStatistics", statisticsElts);

			XDocument xdoc = new XDocument(
				new XElement("FreeCell",
					new XAttribute("name", Name),
					new XAttribute("startTime", this.StartTime),
					new XElement("levelStatistics", statisticsElts),
					entryElts));

			xdoc.Save(xmlPath);
		}

		private XElement CreateEntryElement(Entry entry)
		{
			XElement entryElt = new XElement(entry.Type.ToString().ToLowerInvariant(),
				new XAttribute("offset", entry.Offset.ToString("hh\\:mm\\:ss")),
				new XAttribute("processed", entry.Processed),
				new XAttribute("queued", entry.Queued),
				new XAttribute("pruned", entry.Pruned),
				new XAttribute("improved", entry.Improved),
				entry.Message);

			return entryElt;
		}

		private XElement CreateStatisticsElement(int level, LevelStatistics stats)
		{
			XElement statisticsElt = new XElement("statistics",
				new XAttribute("level", level),
				new XAttribute("processed", stats.Processed),
				new XAttribute("rawBranchingFactor", string.Format(CultureInfo.InvariantCulture, "{0:N1}", stats.RawBranchingFactor)),
				new XAttribute("childStatesGenerated", stats.ChildStatesGenerated),
				new XAttribute("childStatesDuplicates", stats.ChildStatesDuplicates),
				new XAttribute("childStatesPruned", stats.ChildStatesPruned),
				new XAttribute("childStatesRemaining", stats.ChildStatesRemaining),
				new XAttribute("branchingFactor", string.Format(CultureInfo.InvariantCulture, "{0:N1}", stats.BranchingFactor)));

			return statisticsElt;
		}
	}
}

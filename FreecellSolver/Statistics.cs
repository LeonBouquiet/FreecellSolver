using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreecellSolver
{
	public class Statistics
	{
		private class Entry
		{
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

		public DateTime StartTime { get; set; }

		public TimeSpan ElapsedTime
		{
			get 
			{
				DateTime effectiveNow = _stopTime ?? DateTime.Now;
				return effectiveNow - StartTime; 
			}
		}

		public long ProcessCount { get; set; }

		public long PruneCount { get; set; }

		public long ImproveCount { get; set; }

		public Func<long> GetQueueCountFunc { get; set; }

		public Statistics()
		{
			_entries = new List<Entry>();
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
			Entry entry = CreateEntry(null);
			_entries.Add(entry);

			Console.WriteLine("[{0:hh\\:mm\\:ss}] {1,8} processed, {2,8} queued, {3} pruned, {4} improved.", entry.Offset, entry.Processed, entry.Queued, entry.Pruned, entry.Improved);
		}

		public void LogEvent(string format, params object[] args)
		{
			string message = string.Format(CultureInfo.InvariantCulture, format, args);
			Entry entry = CreateEntry(message);
			_entries.Add(entry);

			Console.WriteLine("[{0:hh\\:mm\\:ss}] {1}", entry.Offset, message);
		}

		//public void LogEventAddition(string format, params object[] args)
		//{
		//	string message = string.Format(CultureInfo.InvariantCulture, format, args);
		//	_entries.Last().Message += message;
		//}

		private Entry CreateEntry(string message)
		{
			Entry result = new Entry();
			result.Offset = ElapsedTime;
			result.Processed = ProcessCount;
			result.Pruned = PruneCount;
			result.Improved = ImproveCount;
			result.Queued = (GetQueueCountFunc != null) ? GetQueueCountFunc() : 0;
			result.Message = message;

			return result;
		}

	}
}

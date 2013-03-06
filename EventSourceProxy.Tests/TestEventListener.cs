using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy.Tests
{
	/// <summary>
	/// Listens to events and records them for testing.
	/// </summary>
	class TestEventListener : EventListener
	{
		public IReadOnlyCollection<EventWrittenEventArgs> Events { get { return new ReadOnlyCollection<EventWrittenEventArgs>(_events); } }
		private List<EventWrittenEventArgs> _events = new List<EventWrittenEventArgs>();

		public void Reset()
		{
			_events.Clear();
		}

		protected override void OnEventWritten(EventWrittenEventArgs eventData)
		{
			_events.Add(eventData);
		}
	}
}

using Microsoft.Diagnostics.Tracing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy.NuGet.Tests
{
	[TestFixture]
    public class NuGetTypeTests : BaseLoggingTest
    {
		public interface ITestLogWithExternalEnums
		{
			[Event(19, Message = "Event: {0}", Level = EventLevel.Informational, Channel = EventChannel.Admin)]
			void Event(string message);
		}

		[Test]
		public void AbstractClassWithExternalEnumsCanBeImplemented()
		{
			var testLog = EventSourceImplementer.GetEventSourceAs<ITestLogWithExternalEnums>();
			_listener.EnableEvents((EventSource)testLog, EventLevel.LogAlways, (EventKeywords)(-1));

			// do some logging
			testLog.Event("hello, world!");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);

			// check the event source
			// make sure that the EventSource attribute makes it to the event source
			var eventSource = events[0].EventSource;
			Assert.AreEqual("ITestLogWithExternalEnums", eventSource.Name);

			// check the individual events
			Assert.AreEqual(testLog, events[0].EventSource);
			Assert.AreEqual(19, events[0].EventId);
			Assert.AreEqual("Event: {0}", events[0].Message);
			Assert.AreEqual(EventLevel.Informational, events[0].Level);
			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual("hello, world!", events[0].Payload[0]);
		}

    }
}

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy.Tests
{
	[TestFixture]
	public class ParameterProviderTests : BaseLoggingTest
	{
		public interface IHaveTraceAs
		{
			[TraceAs("data")]
			void TraceAsData(string p1, string p2);
		}

		[Test]
		public void InterfaceMembersCanBeCollapsed()
		{
			var testLog = EventSourceImplementer.GetEventSourceAs<IHaveTraceAs>();
			_listener.EnableEvents((EventSource)testLog, EventLevel.LogAlways, (EventKeywords)(-1));

			// do some logging
			testLog.TraceAsData("p1", "p2");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
		}

		// TODO: proxy interface should be collapsable
		// TODO: concrete class should fail on traceas with collapse
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.Diagnostics.Tracing;

namespace EventSourceProxy.NuGet.Tests
{
	public class BaseLoggingTest
	{
		#region Setup and TearDown
		internal TestEventListener _listener;

		[SetUp]
		public void SetUp()
		{
			_listener = new TestEventListener();
		}
		#endregion

		protected void EnableLogging<TLog>() where TLog : class
		{
			// create the logger and make sure it is serializing the parameters properly
			var logger = EventSourceImplementer.GetEventSource<TLog>();
			_listener.EnableEvents(logger, EventLevel.LogAlways);
		}

		protected void EnableLogging(object proxy)
		{
			// create the logger and make sure it is serializing the parameters properly
			_listener.EnableEvents((EventSource)proxy, EventLevel.LogAlways);
		}
	}
}

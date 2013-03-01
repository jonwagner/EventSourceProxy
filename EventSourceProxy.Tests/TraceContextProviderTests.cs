using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace EventSourceProxy.Tests
{
	[TestFixture]
	public class TraceContextProviderTests : BaseLoggingTest
	{
		#region Test Classes
		public interface ILog
		{
			void DoSomething();
		}

		public interface ILog2 { }
		public interface ILog3 { }
		public interface ILog4
		{
			void DoSomething();
		}

		class TraceContextProvider : ITraceContextProvider
		{
			public bool WasCalled = false;

			public string ProvideContext()
			{
				WasCalled = true;
				return "context";
			}
		}
		#endregion

		[Test]
		public void ProviderShouldBeCalledOnLog()
		{
			var contextProvider = new TraceContextProvider();
			EventSourceImplementer.RegisterProvider<ILog>(contextProvider);

			var testLog = EventSourceImplementer.GetEventSourceAs<ILog>();
			_listener.EnableEvents((EventSource)testLog, EventLevel.LogAlways, (EventKeywords)(-1));

			testLog.DoSomething();

			Assert.IsTrue(contextProvider.WasCalled);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual("context", events[0].Payload[0]);
		}

		[Test]
		public void ProviderShouldNotBeCalledWhenLogIsDisabled()
		{
			var contextProvider = new TraceContextProvider();
			EventSourceImplementer.RegisterProvider<ILog4>(contextProvider);

			var testLog = EventSourceImplementer.GetEventSourceAs<ILog4>();
			testLog.DoSomething();

			Assert.IsFalse(contextProvider.WasCalled);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(0, events.Length);
		}
		
		[Test]
		public void RegisterProviderTwiceShouldFail()
		{
			var contextProvider = new TraceContextProvider();
			EventSourceImplementer.RegisterProvider<ILog2>(contextProvider);
			Assert.Throws<InvalidOperationException>(() => EventSourceImplementer.RegisterProvider<ILog>(contextProvider));
		}

		[Test]
		public void RegisterProviderAfterSourceCreationShouldFail()
		{
			var log = EventSourceImplementer.GetEventSource<ILog3>();

			var contextProvider = new TraceContextProvider();
			Assert.Throws<InvalidOperationException>(() => EventSourceImplementer.RegisterProvider<ILog>(contextProvider));
		}
	}
}

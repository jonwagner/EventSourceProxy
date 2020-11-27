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

		class MyTraceContextProvider : TraceContextProvider
		{
			public bool WasCalled = false;
			public string Method = null;

			public override string ProvideContext(InvocationContext context)
			{
				Method = context.MethodInfo.Name;
				WasCalled = true;
				return "context";
			}
		}
		#endregion

		#region Base Test Cases
		[Test]
		public void ProviderShouldBeCalledOnLog()
		{
			var contextProvider = new MyTraceContextProvider();
			EventSourceImplementer.RegisterProvider<ILog>(contextProvider);

			var testLog = EventSourceImplementer.GetEventSourceAs<ILog>();
			_listener.EnableEvents((EventSource)testLog, EventLevel.LogAlways);

			testLog.DoSomething();

			Assert.IsTrue(contextProvider.WasCalled);
			Assert.AreEqual("DoSomething", contextProvider.Method);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual("context", events[0].Payload[0]);
		}

		[Test]
		public void ProviderShouldNotBeCalledWhenLogIsDisabled()
		{
			var contextProvider = new MyTraceContextProvider();
			EventSourceImplementer.RegisterProvider<ILog4>(contextProvider);

			var testLog = EventSourceImplementer.GetEventSourceAs<ILog4>();
			testLog.DoSomething();

			Assert.IsFalse(contextProvider.WasCalled);
			Assert.IsNull(contextProvider.Method);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(0, events.Length);
		}
		
		[Test]
		public void RegisterProviderTwiceShouldFail()
		{
			var contextProvider = new MyTraceContextProvider();
			EventSourceImplementer.RegisterProvider<ILog2>(contextProvider);
			Assert.Throws<InvalidOperationException>(() => EventSourceImplementer.RegisterProvider<ILog>(contextProvider));
		}

		[Test]
		public void RegisterProviderAfterSourceCreationShouldFail()
		{
			var log = EventSourceImplementer.GetEventSource<ILog3>();

			var contextProvider = new MyTraceContextProvider();
			Assert.Throws<InvalidOperationException>(() => EventSourceImplementer.RegisterProvider<ILog>(contextProvider));
		}
		#endregion

		#region Attribute Tests
		[TraceContextProvider(typeof(MyTraceContextProvider))]
		public interface ILogWithProviderAttribute
		{
			void DoSomething();
		}

		[TraceContextProvider(typeof(MyTraceContextProvider))]
		[TraceContext(false)]
		public interface ILogWithAttributeDisabled
		{
			void DoSomething();
		}

		[TraceContextProvider(typeof(MyTraceContextProvider))]
		[TraceContext(false)]
		public interface ILogWithAttributeDisabledAndMethodEnabled
		{
			[TraceContext(true)]
			void DoSomething();
		}

		[Test]
		public void ProviderCanBeSpecifiedByAttribute()
		{
			var testLog = EventSourceImplementer.GetEventSourceAs<ILogWithProviderAttribute>();
			_listener.EnableEvents((EventSource)testLog, EventLevel.LogAlways);

			testLog.DoSomething();

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual("context", events[0].Payload[0]);
		}

		[Test]
		public void ContextCanBeControlledByClassAttribute()
		{
			var testLog = EventSourceImplementer.GetEventSourceAs<ILogWithAttributeDisabled>();
			_listener.EnableEvents((EventSource)testLog, EventLevel.LogAlways);

			testLog.DoSomething();

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(0, events[0].Payload.Count);
		}

		[Test]
		public void ContextCanBeControlledByMethodAttribute()
		{
			var testLog = EventSourceImplementer.GetEventSourceAs<ILogWithAttributeDisabledAndMethodEnabled>();
			_listener.EnableEvents((EventSource)testLog, EventLevel.LogAlways);

			testLog.DoSomething();

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].Payload.Count);
		}
		#endregion

		#region Rich Context Tests
		public interface IHaveRichContext
		{
			void Log(string message);
		}

		[Test]
		public void TestAddingContextToEachMethod()
		{
			TraceParameterProvider.Default.For<IHaveRichContext>()
				.AddContextData<string>("Site.Id")
				.AddContextData<string>("Site.Name")
				.AddContextData<int>("PID");

			var proxy = EventSourceImplementer.GetEventSourceAs<IHaveRichContext>();
			EnableLogging(proxy);

			using (var context = TraceContext.Begin())
			{
				context["PID"] = 1234;

				using (var context2 = TraceContext.Begin())
				{
					context2["Site.Id"] = "Ts1";
					context2["Site.Name"] = "TestSite1";

					proxy.Log("message");
				}
			}

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(4, events[0].Payload.Count);
			Assert.AreEqual("message", events[0].Payload[0]);
			Assert.AreEqual("Ts1", events[0].Payload[1]);
			Assert.AreEqual("TestSite1", events[0].Payload[2]);
			Assert.AreEqual(1234, events[0].Payload[3]);
		}
		#endregion
	}
}

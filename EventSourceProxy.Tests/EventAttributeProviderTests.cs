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
	public class EventAttributeProviderTests : BaseLoggingTest
	{
		#region Provider Attribute Tests
		[EventAttributeProvider(typeof(MyEventAttributeProvider))]
		public interface ILogInterfaceWithAttribute
		{
			void DoSomething();
		}

		public class LogInterfaceWithAttribute : ILogInterfaceWithAttribute
		{
			public void DoSomething() { throw new ApplicationException(); }
		}

		public class MyEventAttributeProvider : EventAttributeProvider
		{
			public MyEventAttributeProvider() : base(EventLevel.Warning, EventLevel.Critical)
			{
			}
		}

		[Test]
		public void AttributeShouldDetermineProvider()
		{
			var logger = EventSourceImplementer.GetEventSourceAs<ILogInterfaceWithAttribute>();
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways);

			logger.DoSomething();

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(EventLevel.Warning, events[0].Level);

			_listener.Reset();
			var proxy = TracingProxy.Create<ILogInterfaceWithAttribute>(new LogInterfaceWithAttribute());
			try { proxy.DoSomething(); }
			catch { }

			events = _listener.Events.ToArray();
			Assert.AreEqual(2, events.Length);
			Assert.AreEqual(EventLevel.Critical, events[1].Level);
		}
		#endregion

		#region Exception Attribute Tests
		[EventException(EventLevel.Critical)]
		public interface ILogInterfaceWithExceptionAttribute
		{
			void DoSomething();
		}

		public class LogInterfaceWithExceptionAttribute : ILogInterfaceWithExceptionAttribute
		{
			public void DoSomething() { throw new ApplicationException(); }
		}

		public interface ILogInterfaceWithExceptionMethodAttribute
		{
			[EventException(EventLevel.Critical)]
			void DoSomething();
		}

		public class LogInterfaceWithExceptionMethodAttribute : ILogInterfaceWithExceptionMethodAttribute
		{
			public void DoSomething() { throw new ApplicationException(); }
		}

		[Test]
		public void ExceptionAttributeShouldDetermineLevel()
		{
			Assert.AreEqual(EventLevel.Error, new EventAttributeProvider().ExceptionEventLevel);

			var logger = EventSourceImplementer.GetEventSourceAs<ILogInterfaceWithExceptionAttribute>();
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways);

			logger.DoSomething();

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(EventLevel.Informational, events[0].Level);

			_listener.Reset();
			var proxy = TracingProxy.Create<ILogInterfaceWithExceptionAttribute>(new LogInterfaceWithExceptionAttribute());
			try { proxy.DoSomething(); }
			catch { }

			events = _listener.Events.ToArray();
			Assert.AreEqual(2, events.Length);
			Assert.AreEqual(EventLevel.Critical, events[1].Level);
		}

		[Test]
		public void ExceptionAttributeOnMethodShouldDetermineLevel()
		{
			Assert.AreEqual(EventLevel.Error, new EventAttributeProvider().ExceptionEventLevel);

			var logger = EventSourceImplementer.GetEventSourceAs<ILogInterfaceWithExceptionMethodAttribute>();
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways);

			logger.DoSomething();

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(EventLevel.Informational, events[0].Level);

			_listener.Reset();
			var proxy = TracingProxy.Create<ILogInterfaceWithExceptionMethodAttribute>(new LogInterfaceWithExceptionMethodAttribute());
			try { proxy.DoSomething(); }
			catch { }

			events = _listener.Events.ToArray();
			Assert.AreEqual(2, events.Length);
			Assert.AreEqual(EventLevel.Critical, events[1].Level);
		}
		#endregion
	}
}

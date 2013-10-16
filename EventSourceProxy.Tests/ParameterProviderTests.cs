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
		#region Collapsing Parameters
		public interface IHaveTraceAs
		{
			[TraceAsData]
			void TraceAsData(string p1, string p2);

			void TraceSomeParameters(string p, [TraceAsData] string p1, [TraceAsData] string p2);
		}

		[Test]
		public void InterfaceParametersCanBeCollapsedTogether()
		{
			EnableLogging<IHaveTraceAs>();

			// do some logging
			var testLog = EventSourceImplementer.GetEventSourceAs<IHaveTraceAs>();
			testLog.TraceAsData("p1", "p2");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual("{\"p1\":\"p1\",\"p2\":\"p2\"}", events[0].Payload[0].ToString());
		}

		[Test]
		public void InterfaceParametersCanBeCollapsedIndividually()
		{
			EnableLogging<IHaveTraceAs>();

			// do some logging
			var testLog = EventSourceImplementer.GetEventSourceAs<IHaveTraceAs>();
			testLog.TraceSomeParameters("p", "p1", "p2");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(2, events[0].Payload.Count);
			Assert.AreEqual("p", events[0].Payload[0].ToString());
			Assert.AreEqual("{\"p1\":\"p1\",\"p2\":\"p2\"}", events[0].Payload[1].ToString());
		}
		#endregion

		#region Proxy Collapsing
		public class HasTraceAs : IHaveTraceAs
		{
			public void TraceAsData(string p1, string p2)
			{
			}

			public void TraceSomeParameters(string p, string p1, string p2)
			{
			}
		}

		public class VirtualHasTraceAs : IHaveTraceAs
		{
			public virtual void TraceAsData(string p1, string p2)
			{
			}

			public virtual void TraceSomeParameters(string p, string p1, string p2)
			{
			}
		}

		public class VirtualHasTraceAsWithoutInterface
		{
			[TraceAsData]
			public virtual void TraceAsData(string p1, string p2)
			{
			}

			public virtual void TraceSomeParameters(string p, [TraceAsData]string p1, [TraceAsData]string p2)
			{
			}
		}

		[Test]
		public void ProxyFromClassToInterfaceCanCollapseParameters()
		{
			EnableLogging<IHaveTraceAs>();

			var proxy = TracingProxy.Create<IHaveTraceAs>(new HasTraceAs());
			proxy.TraceAsData("p1", "p2");
			proxy.TraceSomeParameters("p", "p1", "p2");

			VerifyEvents();
		}

		[Test]
		public void ProxyFromVirtualClassToInterfaceCanCollapseParameters()
		{
			EnableLogging<IHaveTraceAs>();

			var proxy = TracingProxy.Create<IHaveTraceAs>(new VirtualHasTraceAs());
			proxy.TraceAsData("p1", "p2");
			proxy.TraceSomeParameters("p", "p1", "p2");

			VerifyEvents();
		}

		[Test]
		public void ProxyFromVirtualClassWithoutInterfaceCanCollapseParameters()
		{
			EnableLogging<VirtualHasTraceAsWithoutInterface>();

			var proxy = TracingProxy.Create<VirtualHasTraceAsWithoutInterface>(new VirtualHasTraceAsWithoutInterface());
			proxy.TraceAsData("p1", "p2");
			proxy.TraceSomeParameters("p", "p1", "p2");

			VerifyEvents();
		}

		[Test]
		public void ProxyFromVirtualClassWithoutInterfaceToUnrelatedInterfaceCanCollapseParameters()
		{
			EnableLogging<IHaveTraceAs>();

			var proxy = TracingProxy.Create<VirtualHasTraceAsWithoutInterface, IHaveTraceAs>(new VirtualHasTraceAsWithoutInterface());
			proxy.TraceAsData("p1", "p2");
			proxy.TraceSomeParameters("p", "p1", "p2");

			VerifyEvents();
		}

		private void EnableLogging<TLog>() where TLog : class
		{
			// create the logger and make sure it is serializing the parameters properly
			var logger = EventSourceImplementer.GetEventSource<TLog>();
			_listener.EnableEvents(logger, EventLevel.LogAlways, (EventKeywords)(-1));
		}

		private void VerifyEvents()
		{
			// look at the events again
			var events = _listener.Events.ToArray();
			Assert.AreEqual(4, events.Length);

			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual("{\"p1\":\"p1\",\"p2\":\"p2\"}", events[0].Payload[0].ToString());

			Assert.AreEqual(2, events[2].Payload.Count);
			Assert.AreEqual("p", events[2].Payload[0].ToString());
			Assert.AreEqual("{\"p1\":\"p1\",\"p2\":\"p2\"}", events[2].Payload[1].ToString());
		}
		#endregion

		// TODO: concrete class should fail on traceas with collapse
		// TODO: provider returning an empty parameter should fail
		// TODO: support for replacing default providers
	}
}

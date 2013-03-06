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
	public class TracingProxyTests : BaseLoggingTest
	{
		#region Test Classes
		public interface ICalculator
		{
			void Clear();
			int AddNumbers(int x, int y);
		}

		public class Calculator : ICalculator
		{
			public void Clear()
			{
			}

			public int AddNumbers(int x, int y)
			{
				return x + y;
			}
		}

		public class VirtualCalculator : ICalculator
		{
			public virtual void Clear()
			{
			}

			public virtual int AddNumbers(int x, int y)
			{
				return x + y;
			}
		}

		public class VirtualCalculatorWithoutInterface
		{
			public virtual void Clear()
			{
			}

			public virtual int AddNumbers(int x, int y)
			{
				return x + y;
			}
		}

		public interface ICalculatorWithCompleted
		{
			void Clear();
			int Clear_Completed();
			int AddNumbers(int x, int y);
			void AddNumbers_Completed(int result);
		}
		#endregion

		#region Create Proxy From Interface Tests
		[Test]
		public void TestLoggingProxyFromClassToInterface()
		{
			// create a logger for the interface and listen on it
			var logger = EventSourceImplementer.GetEventSource<ICalculator>();
			_listener.EnableEvents(logger, EventLevel.LogAlways, (EventKeywords)(-1));

			// create a calculator and a proxy
			var proxy = TracingProxy.Create<ICalculator>(new Calculator());

			// call the method through the proxy
			proxy.Clear();
			Assert.AreEqual(3, proxy.AddNumbers(1, 2));

			// look at the events in the log
			VerifyEvents(logger);
		}

		[Test]
		public void TestLoggingProxyFromVirtualClassToInterface()
		{
			// create a logger for the interface and listen on it
			var logger = EventSourceImplementer.GetEventSource<ICalculator>();
			_listener.EnableEvents(logger, EventLevel.LogAlways, (EventKeywords)(-1));

			// create a calculator and a proxy
			var proxy = TracingProxy.Create<ICalculator>(new VirtualCalculator());

			// call the method through the proxy
			proxy.Clear();
			Assert.AreEqual(3, proxy.AddNumbers(1, 2));

			// look at the events in the log
			VerifyEvents(logger);
		}

		[Test]
		public void TestLoggingProxyFromVirtualClassToVirtualClass()
		{
			var logger = EventSourceImplementer.GetEventSource<VirtualCalculatorWithoutInterface>();
			_listener.EnableEvents(logger, EventLevel.LogAlways, (EventKeywords)(-1));

			// create a calculator and a proxy
			var proxy = TracingProxy.Create<VirtualCalculatorWithoutInterface>(new VirtualCalculatorWithoutInterface());

			// call the method through the proxy
			proxy.Clear();
			Assert.AreEqual(3, proxy.AddNumbers(1, 2));

			VerifyEvents(logger);
		}

		[Test]
		public void TestLoggingProxyFromVirtualClassToUnrelatedInterface()
		{
			// create a logger for the interface and listen on it
			var logger = EventSourceImplementer.GetEventSource<ICalculator>();
			_listener.EnableEvents(logger, EventLevel.LogAlways, (EventKeywords)(-1));

			// create a calculator and a proxy
			var proxy = TracingProxy.Create<VirtualCalculatorWithoutInterface, ICalculator>(new VirtualCalculatorWithoutInterface());

			// call the method through the proxy
			proxy.Clear();
			Assert.AreEqual(3, proxy.AddNumbers(1, 2));

			// look at the events in the log
			VerifyEvents(logger);
		}

		[Test]
		public void TestLoggingProxyWithCompletedEvents()
		{
			// create a logger for the interface and listen on it
			var logger = EventSourceImplementer.GetEventSource<ICalculatorWithCompleted>();
			_listener.EnableEvents(logger, EventLevel.LogAlways, (EventKeywords)(-1));

			// create a calculator and a proxy
			var proxy = TracingProxy.Create<VirtualCalculator, ICalculatorWithCompleted>(new VirtualCalculator());

			// call the method through the proxy
			proxy.Clear();
			Assert.AreEqual(3, proxy.AddNumbers(1, 2));

			VerifyEvents(logger);

			// check the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(4, events.Length);
			var eventSource = events[0].EventSource;

			// check the individual events
			Assert.AreEqual(logger, events[0].EventSource);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual("Clear", events[0].Message);
			Assert.AreEqual(EventLevel.Informational, events[0].Level);
			Assert.AreEqual((EventKeywords)1, events[0].Keywords);
			Assert.AreEqual(0, events[0].Payload.Count);

			Assert.AreEqual(logger, events[1].EventSource);
			Assert.AreEqual(2, events[1].EventId);
			Assert.AreEqual("Clear_Completed", events[1].Message);
			Assert.AreEqual(EventLevel.Informational, events[1].Level);
			Assert.AreEqual((EventKeywords)2, events[1].Keywords);
			Assert.AreEqual(0, events[0].Payload.Count);

			Assert.AreEqual(logger, events[2].EventSource);
			Assert.AreEqual(3, events[2].EventId);
			Assert.AreEqual("AddNumbers", events[2].Message);
			Assert.AreEqual(EventLevel.Informational, events[2].Level);
			Assert.AreEqual((EventKeywords)4, events[2].Keywords);
			Assert.AreEqual(2, events[2].Payload.Count);
			Assert.AreEqual(1, events[2].Payload[0]);
			Assert.AreEqual(2, events[2].Payload[1]);

			// a fourth event for completed
			Assert.AreEqual(logger, events[3].EventSource);
			Assert.AreEqual(4, events[3].EventId);
			Assert.AreEqual("AddNumbers_Completed", events[3].Message);
			Assert.AreEqual(EventLevel.Informational, events[3].Level);
			Assert.AreEqual((EventKeywords)8, events[3].Keywords);
			Assert.AreEqual(1, events[3].Payload.Count);
			Assert.AreEqual(3, events[3].Payload[0]);
		}

		private void VerifyEvents(object logger)
		{
			// check the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(4, events.Length);
			var eventSource = events[0].EventSource;

			// check the individual events
			Assert.AreEqual(logger, events[0].EventSource);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual("Clear", events[0].Message);
			Assert.AreEqual(EventLevel.Informational, events[0].Level);
			Assert.AreEqual((EventKeywords)1, events[0].Keywords);
			Assert.AreEqual(0, events[0].Payload.Count);

			Assert.AreEqual(logger, events[1].EventSource);
			Assert.AreEqual(2, events[1].EventId);
			Assert.AreEqual("Clear_Completed", events[1].Message);
			Assert.AreEqual(EventLevel.Informational, events[1].Level);
			Assert.AreEqual(0, events[0].Payload.Count);

			Assert.AreEqual(logger, events[2].EventSource);
			Assert.AreEqual(3, events[2].EventId);
			Assert.AreEqual("AddNumbers", events[2].Message);
			Assert.AreEqual(EventLevel.Informational, events[2].Level);
			Assert.AreEqual(2, events[2].Payload.Count);
			Assert.AreEqual(1, events[2].Payload[0]);
			Assert.AreEqual(2, events[2].Payload[1]);

			// a fourth event for completed
			Assert.AreEqual(logger, events[3].EventSource);
			Assert.AreEqual(4, events[3].EventId);
			Assert.AreEqual("AddNumbers_Completed", events[3].Message);
			Assert.AreEqual(EventLevel.Informational, events[3].Level);
			Assert.AreEqual(1, events[3].Payload.Count);
			Assert.AreEqual(3, events[3].Payload[0]);
		}
		#endregion

		#region Proxy for Object with Instance Variables
		public interface IInstance
		{
			int GetValue();
		}
		public class Instance : IInstance
		{
			public int Value;
			public int GetValue() { return Value; }
		}

		[Test]
		public void TestThatInstancePointerIsPassedProperly()
		{
			Instance i = new Instance() { Value = 99 };
			Assert.AreEqual(99, i.GetValue());

			var proxy = TracingProxy.Create<IInstance>(i);
			Assert.AreEqual(99, proxy.GetValue());

			proxy = (IInstance)TracingProxy.Create(i, typeof(IInstance));
			Assert.AreEqual(99, proxy.GetValue());
		}
		#endregion

		#region Automatic Activity ID Tests
		public class AutomaticActivity
		{
			public Guid ActivityId { get; set; }

			public virtual void Method()
			{
				ActivityId = EventActivityScope.CurrentActivityId;
			}

			public virtual void Throws()
			{
				throw new Exception();
			}
		}

		[Test]
		public void MethodInterfaceShouldCreateActivity()
		{
			Assert.AreEqual(Guid.Empty, EventActivityScope.CurrentActivityId);

			var tester = new AutomaticActivity();
			var proxy = TracingProxy.Create<AutomaticActivity>(tester);
			proxy.Method();

			Assert.AreNotEqual(Guid.Empty, tester.ActivityId);

			Assert.AreEqual(Guid.Empty, EventActivityScope.CurrentActivityId);
		}

		[Test]
		public void MethodInterfaceShouldNotChangeActivity()
		{
			Assert.AreEqual(Guid.Empty, EventActivityScope.CurrentActivityId);

			using (EventActivityScope scope = new EventActivityScope())
			{
				Assert.AreNotEqual(Guid.Empty, EventActivityScope.CurrentActivityId);

				var tester = new AutomaticActivity();
				var proxy = TracingProxy.Create<AutomaticActivity>(tester);
				proxy.Method();

				Assert.AreEqual(scope.ActivityId, tester.ActivityId);
				Assert.AreEqual(scope.ActivityId, EventActivityScope.CurrentActivityId);
			}

			Assert.AreEqual(Guid.Empty, EventActivityScope.CurrentActivityId);
		}

		[Test]
		public void MethodThatThrowsShouldUnwindActivity()
		{
			Assert.AreEqual(Guid.Empty, EventActivityScope.CurrentActivityId);

			var tester = new AutomaticActivity();
			var proxy = TracingProxy.Create<AutomaticActivity>(tester);
			try
			{
				proxy.Throws();
			}
			catch
			{
			}

			Assert.AreEqual(Guid.Empty, EventActivityScope.CurrentActivityId);
		}
		#endregion

		#region Interface With Reference Parameters
		public class ReferenceData
		{
			public int Data;
		}

		public interface ITestServiceWithReferenceParameters
		{
			void GetItem(ref int value);
			void GetData(ref ReferenceData value);
		}

		public class TaskService : ITestServiceWithReferenceParameters
		{
			public void GetItem(ref int value) { value++; }
			public void GetData(ref ReferenceData value) { value = new ReferenceData() { Data = value.Data + 1 }; }
		}

		[Test]
		public void CanImplementInterfaceWithReferenceParameter()
		{
			int value;

			// test the service
			var service = new TaskService();
			value = 1;
			service.GetItem(ref value);
			Assert.AreEqual(2, value);

			// log a class reference
			var data = new ReferenceData() { Data = 5 };
			var resultData = data;
			service.GetData(ref resultData);
			Assert.AreNotEqual(data, resultData);
			Assert.AreEqual(data.Data + 1, resultData.Data);

			// turn on logging
			var log = EventSourceImplementer.GetEventSourceAs<ITestServiceWithReferenceParameters>();
			_listener.EnableEvents((EventSource)log, EventLevel.LogAlways, (EventKeywords)(-1));

			// log a built-in type reference
			value = 1;
			log.GetItem(ref value);

			// log a class reference
			data = new ReferenceData() { Data = 5 };
			resultData = data;
			log.GetData(ref resultData);

			// create a proxy
			var proxy = TracingProxy.Create<ITestServiceWithReferenceParameters>(service);

			// proxy a built-in type reference
			value = 1;
			proxy.GetItem(ref value);
			Assert.AreEqual(2, value);

			// proxy a class reference
			data = new ReferenceData() { Data = 7 };
			resultData = data;
			proxy.GetData(ref resultData);
			Assert.AreNotEqual(data, resultData);
			Assert.AreEqual(data.Data + 1, resultData.Data);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(6, events.Length);

			// check the individual events to make sure the data came back in the payload
			Assert.AreEqual(1, events[0].Payload[0]);
			Assert.AreEqual(new JsonObjectSerializer().SerializeObject(data, new RuntimeMethodHandle(), 0), events[4].Payload[0]);
		}
		#endregion

		#region Interface with Generic Methods
		public interface ITestServiceWithGenericMethods
		{
			T GetItem<T>(T value);
			void GetItem2<TIn, TIn2>(TIn value, TIn2 value2);
			TOut GetItem3<TIn, TOut>(TIn value);
			T Constrained<T>(T t) where T : class;
		}

		public class TestServiceWithGenericMethods : ITestServiceWithGenericMethods
		{
			public T GetItem<T>(T value) { return value; }
			public void GetItem2<TIn, TIn2>(TIn value, TIn2 value2) { }
			public TOut GetItem3<TIn, TOut>(TIn value) { return default(TOut); }
			public T Constrained<T>(T t) where T : class { return t; }
		}

		[Test]
		public void CanImplementInterfaceWithGenericMethods()
		{
			// turn on logging
			var log = EventSourceImplementer.GetEventSourceAs<ITestServiceWithGenericMethods>();
			_listener.EnableEvents((EventSource)log, EventLevel.LogAlways, (EventKeywords)(-1));

			log.GetItem((int)1);
			log.GetItem((string)"s");
			log.GetItem((decimal)1);
			log.GetItem2<int, int>((int)1, (int)2);
			log.GetItem2<string, string>("x", "y");
			log.GetItem2<decimal, decimal>((decimal)1, (decimal)2);
			log.GetItem3<int, int>((int)1);
			log.GetItem3<string, string>("x");
			log.Constrained<string>("y");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(9, events.Length);

			// check the individual events to make sure the data came back in the payload
			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual(1, events[1].Payload.Count);

			// create a proxy on the interface
			_listener.Reset();
			var proxy = TracingProxy.Create<ITestServiceWithGenericMethods>(new TestServiceWithGenericMethods());
			proxy.GetItem(1);
			proxy.GetItem((string)"s");
			proxy.GetItem((decimal)1);
			proxy.GetItem2<int, int>((int)1, (int)2);
			proxy.GetItem2<string, string>("x", "y");
			proxy.GetItem2<decimal, decimal>((decimal)1, (decimal)2);
			proxy.GetItem3<int, int>((int)1);
			proxy.GetItem3<string, string>("x");
			proxy.Constrained<string>("y");

			events = _listener.Events.ToArray();
			Assert.AreEqual(18, events.Length);

			// check the individual events to make sure the data came back in the payload
			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual(1, events[1].Payload.Count);
		}
		#endregion

		#region You Can't Do This Tests
		public class DoesNotImplement
		{
		}

		[Test]
		public void ProxyingAnInvalidClassShouldThrow()
		{
			Assert.Throws<ArgumentException>(() => TracingProxy.Create<ITestServiceWithGenericMethods>(new DoesNotImplement()));
		}
		#endregion
	}
}

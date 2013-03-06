using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace EventSourceProxy.Tests
{
	[TestFixture]
    public class EventSourceImplementerTests : BaseLoggingTest
	{
		#region Abstract Class Implementation
		/// <summary>
		/// This class is from the EventSource documentation.
		/// </summary>
		[EventSource(Name = "TestLog", Guid = "0469abfa-1bb2-466a-b645-e3e15a02f3ff")]
		public abstract class TestLog : EventSource
		{
			public class Keywords
			{
				public const EventKeywords Page = (EventKeywords)1;
				public const EventKeywords DataBase = (EventKeywords)2;
				public const EventKeywords Diagnostic = (EventKeywords)4;
				public const EventKeywords Perf = (EventKeywords)8;
			}

			public class Tasks
			{
				public const EventTask Page = (EventTask)1;
				public const EventTask DBQuery = (EventTask)2;
			}

			[Event(1, Message = "Application Failure: {0}", Level = EventLevel.Error, Keywords = Keywords.Diagnostic, Task = Tasks.DBQuery)]
			public abstract void Failure(string message);

			// this tests that multiple parameters can be sent, with different types
			[Event(4, Message = "Other Information: {0} {1}", Level = EventLevel.Informational, Keywords = Keywords.Perf)]
			public abstract void Other(string message, int number);

			// This tests that a direct method can be called.
			[Event(5, Message = "Direct Call: {0}", Level = EventLevel.Warning, Keywords = Keywords.Diagnostic)]
			public void Direct(string message)
			{
				WriteEvent(5, message);
			}
		}

		[Test]
		public void AbstractClassCanBeImplemented()
		{
			var testLog = EventSourceImplementer.GetEventSourceAs<TestLog>();
			_listener.EnableEvents(testLog, EventLevel.LogAlways, (EventKeywords)(-1));

			// do some logging
			testLog.Failure("whoops!");
			testLog.Other("doing something", 19);
			testLog.Direct("factory direct");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(3, events.Length);

			// check the event source
			// make sure that the EventSource attribute makes it to the event source
			var eventSource = events[0].EventSource;
			Assert.AreEqual("TestLog", eventSource.Name);
			Assert.AreEqual(new Guid ("0469abfa-1bb2-466a-b645-e3e15a02f3ff"), eventSource.Guid);

			// check the individual events
			Assert.AreEqual(testLog, events[0].EventSource);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual("Application Failure: {0}", events[0].Message);
			Assert.AreEqual(EventLevel.Error, events[0].Level);
			Assert.AreEqual(TestLog.Keywords.Diagnostic, events[0].Keywords);
			Assert.AreEqual(TestLog.Tasks.DBQuery, events[0].Task);
			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual("whoops!", events[0].Payload[0]);

			Assert.AreEqual(testLog, events[1].EventSource);
			Assert.AreEqual(4, events[1].EventId);
			Assert.AreEqual("Other Information: {0} {1}", events[1].Message);
			Assert.AreEqual(EventLevel.Informational, events[1].Level);
			Assert.AreEqual(TestLog.Keywords.Perf, events[1].Keywords);
			Assert.AreEqual(2, events[1].Payload.Count);
			Assert.AreEqual("doing something", events[1].Payload[0]);
			Assert.AreEqual(19, events[1].Payload[1]);

			Assert.AreEqual(testLog, events[2].EventSource);
			Assert.AreEqual(5, events[2].EventId);
			Assert.AreEqual("Direct Call: {0}", events[2].Message);
			Assert.AreEqual(EventLevel.Warning, events[2].Level);
			Assert.AreEqual(TestLog.Keywords.Diagnostic, events[2].Keywords);
			Assert.AreEqual(1, events[2].Payload.Count);
			Assert.AreEqual("factory direct", events[2].Payload[0]);
		}
		#endregion

		#region Derived Abstract Class Implementation
		/// <summary>
		/// This class is from the EventSource documentation.
		/// </summary>
		[EventSource(Name = "TestLog", Guid = "0569abfa-1bb2-466a-b645-e3e15a02f3ff")]
		public abstract class TestLogDerived : TestLog
		{
			// all methods and enums are retrieved from the base class
		}

		[Test]
		public void DerivedAbstractClassCanBeImplemented()
		{
			var testLog = EventSourceImplementer.GetEventSourceAs<TestLogDerived>();
			_listener.EnableEvents(testLog, EventLevel.LogAlways, (EventKeywords)(-1));

			// do some logging
			testLog.Failure("whoops!");
			testLog.Other("doing something", 19);
			testLog.Direct("factory direct");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(3, events.Length);

			// check the event source
			// make sure that the EventSource attribute makes it to the event source
			var eventSource = events[0].EventSource;
			Assert.AreEqual("TestLog", eventSource.Name);
			Assert.AreEqual(new Guid("0569abfa-1bb2-466a-b645-e3e15a02f3ff"), eventSource.Guid);

			// check the individual events
			Assert.AreEqual(testLog, events[0].EventSource);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual("Application Failure: {0}", events[0].Message);
			Assert.AreEqual(EventLevel.Error, events[0].Level);
			Assert.AreEqual(TestLog.Keywords.Diagnostic, events[0].Keywords);
			Assert.AreEqual(TestLog.Tasks.DBQuery, events[0].Task);
			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual("whoops!", events[0].Payload[0]);

			Assert.AreEqual(testLog, events[1].EventSource);
			Assert.AreEqual(4, events[1].EventId);
			Assert.AreEqual("Other Information: {0} {1}", events[1].Message);
			Assert.AreEqual(EventLevel.Informational, events[1].Level);
			Assert.AreEqual(TestLog.Keywords.Perf, events[1].Keywords);
			Assert.AreEqual(2, events[1].Payload.Count);
			Assert.AreEqual("doing something", events[1].Payload[0]);
			Assert.AreEqual(19, events[1].Payload[1]);

			Assert.AreEqual(testLog, events[2].EventSource);
			Assert.AreEqual(5, events[2].EventId);
			Assert.AreEqual("Direct Call: {0}", events[2].Message);
			Assert.AreEqual(EventLevel.Warning, events[2].Level);
			Assert.AreEqual(TestLog.Keywords.Diagnostic, events[2].Keywords);
			Assert.AreEqual(1, events[2].Payload.Count);
			Assert.AreEqual("factory direct", events[2].Payload[0]);
		}
		#endregion

		#region Abstract Class with External Enums
		[EventSourceImplementation(Keywords = typeof(KeywordsFoo))]
		public abstract class TestLogWithExternalEnums : EventSource
		{
			[Event(1, Message = "Event: {0}", Level = EventLevel.Informational, Keywords = KeywordsFoo.Startup)]
			public abstract void Event(string message);
		}

		[Test]
		public void AbstractClassWithExternalEnumsCanBeImplemented()
		{
			var testLog = EventSourceImplementer.GetEventSourceAs<TestLogWithExternalEnums>();
			_listener.EnableEvents(testLog, EventLevel.LogAlways, (EventKeywords)(-1));

			// do some logging
			testLog.Event("hello, world!");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);

			// check the event source
			// make sure that the EventSource attribute makes it to the event source
			var eventSource = events[0].EventSource;
			Assert.AreEqual("TestLogWithExternalEnums", eventSource.Name);

			// check the individual events
			Assert.AreEqual(testLog, events[0].EventSource);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual("Event: {0}", events[0].Message);
			Assert.AreEqual(EventLevel.Informational, events[0].Level);
			Assert.AreEqual(KeywordsFoo.Startup, events[0].Keywords);
			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual("hello, world!", events[0].Payload[0]);
		}
		#endregion

		#region Interface Implementation
		// this tests that the eventsourceimplementation makes it to the event source
		// also tests that interfaces can have external enums
		[EventSourceImplementation(
			Name = "TestLog2", 
			Guid = "ff69abfa-1bb2-466a-b645-e3e15a02f3ff",
			Keywords = typeof(KeywordsFoo), 
			Tasks = typeof(TasksFoo), 
			OpCodes = typeof(OpcodesFoo))]
		public interface ITestLog
		{
			[Event(10, Message = "Startup: {0}", Level = EventLevel.Warning, Keywords = KeywordsFoo.Startup, Task = TasksFoo.Task1, Opcode=OpcodesFoo.Opcode1)]
			void Startup(string message);

			// this tests that the interface can return a value and things still work
			[Event(5, Message = "WithReturn: {0} {1}", Level = EventLevel.Warning, Keywords = KeywordsFoo.Return, Task = TasksFoo.Task2, Opcode = OpcodesFoo.Opcode2)]
			int WithReturn(string message, int value);
		}

		public class KeywordsFoo
		{
			public const EventKeywords Startup = (EventKeywords)(1 << 5);
			public const EventKeywords Return = (EventKeywords)(1 << 7);
		}

		public class OpcodesFoo
		{
			public const EventOpcode Opcode1 = (EventOpcode)16;
			public const EventOpcode Opcode2 = (EventOpcode)20;
		}

		public class TasksFoo
		{
			public const EventTask Task1 = (EventTask)2;
			public const EventTask Task2 = (EventTask)4;
		}

		[Test]
		public void InterfaceCanBeImplemented()
		{
			var testLog = EventSourceImplementer.GetEventSourceAs<ITestLog>();
			_listener.EnableEvents((EventSource)testLog, EventLevel.LogAlways, (EventKeywords)(-1));

			// do some logging
			testLog.Startup("starting!");
			testLog.WithReturn("return", 9);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(2, events.Length);

			// check the event source
			// make sure that the EventSource attribute makes it to the event source
			var eventSource = events[0].EventSource;
			Assert.AreEqual("TestLog2", eventSource.Name);
			Assert.AreEqual(new Guid("ff69abfa-1bb2-466a-b645-e3e15a02f3ff"), eventSource.Guid);

			// check the individual events
			Assert.AreEqual(testLog, events[0].EventSource);
			Assert.AreEqual(10, events[0].EventId);
			Assert.AreEqual("Startup: {0}", events[0].Message);
			Assert.AreEqual(EventLevel.Warning, events[0].Level);
			Assert.AreEqual(KeywordsFoo.Startup, events[0].Keywords);
			Assert.AreEqual(TasksFoo.Task1, events[0].Task);
			Assert.AreEqual(OpcodesFoo.Opcode1, events[0].Opcode);
			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual("starting!", events[0].Payload[0]);

			Assert.AreEqual(testLog, events[1].EventSource);
			Assert.AreEqual(5, events[1].EventId);
			Assert.AreEqual("WithReturn: {0} {1}", events[1].Message);
			Assert.AreEqual(EventLevel.Warning, events[1].Level);
			Assert.AreEqual(KeywordsFoo.Return, events[1].Keywords);
			Assert.AreEqual(TasksFoo.Task2, events[1].Task);
			Assert.AreEqual(OpcodesFoo.Opcode2, events[1].Opcode);
			Assert.AreEqual(2, events[1].Payload.Count);
			Assert.AreEqual("return", events[1].Payload[0]);
			Assert.AreEqual(9, events[1].Payload[1]);
		}
		#endregion

		#region Simple Interface Implementation
		// this is any old interface with no decoration
		public interface IJustAnInterface
		{
			int AddNumbers(int x, int y);
			int SubtractNumbers(int x, int y);
		}

		[Test]
		public void PlainInterfaceCanBeImplemented()
		{
			var testLog = EventSourceImplementer.GetEventSourceAs<IJustAnInterface>();
			_listener.EnableEvents((EventSource)testLog, EventLevel.LogAlways, (EventKeywords)(-1));

			// do some logging
			testLog.AddNumbers(2, 3);
			testLog.SubtractNumbers(4, 5);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(2, events.Length);

			// check the event source
			// make sure that the EventSource attribute makes it to the event source
			var eventSource = events[0].EventSource;
			Assert.AreEqual("IJustAnInterface", eventSource.Name);

			// check the individual events
			Assert.AreEqual(testLog, events[0].EventSource);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual("AddNumbers", events[0].Message);
			Assert.AreEqual(EventLevel.Informational, events[0].Level);
			Assert.AreEqual((EventKeywords)1, events[0].Keywords);
			Assert.AreEqual(2, events[0].Payload.Count);
			Assert.AreEqual(2, events[0].Payload[0]);
			Assert.AreEqual(3, events[0].Payload[1]);

			Assert.AreEqual(testLog, events[1].EventSource);
			Assert.AreEqual(3, events[1].EventId);
			Assert.AreEqual("SubtractNumbers", events[1].Message);
			Assert.AreEqual(EventLevel.Informational, events[1].Level);
			Assert.AreEqual((EventKeywords)2, events[1].Keywords);
			Assert.AreEqual(2, events[1].Payload.Count);
			Assert.AreEqual(4, events[1].Payload[0]);
			Assert.AreEqual(5, events[1].Payload[1]);
		}
		#endregion

		#region Derived Interface Implementation
		// this is any old interface with no decoration
 		public interface IJustAnInterfaceDerived : IJustAnInterface
		{
		}

		[Test]
		public void DerivedInterfaceCanBeImplemented()
		{
			var testLog = EventSourceImplementer.GetEventSourceAs<IJustAnInterfaceDerived>();
			_listener.EnableEvents((EventSource)testLog, EventLevel.LogAlways, (EventKeywords)(-1));

			// do some logging
			testLog.AddNumbers(2, 3);
			testLog.SubtractNumbers(4, 5);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(2, events.Length);

			// check the event source
			// make sure that the EventSource attribute makes it to the event source
			var eventSource = events[0].EventSource;
			Assert.AreEqual("IJustAnInterfaceDerived", eventSource.Name);

			// check the individual events
			Assert.AreEqual(testLog, events[0].EventSource);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual("AddNumbers", events[0].Message);
			Assert.AreEqual(EventLevel.Informational, events[0].Level);
			Assert.AreEqual((EventKeywords)1, events[0].Keywords);
			Assert.AreEqual(2, events[0].Payload.Count);
			Assert.AreEqual(2, events[0].Payload[0]);
			Assert.AreEqual(3, events[0].Payload[1]);

			Assert.AreEqual(testLog, events[1].EventSource);
			Assert.AreEqual(3, events[1].EventId);
			Assert.AreEqual("SubtractNumbers", events[1].Message);
			Assert.AreEqual(EventLevel.Informational, events[1].Level);
			Assert.AreEqual((EventKeywords)2, events[1].Keywords);
			Assert.AreEqual(2, events[1].Payload.Count);
			Assert.AreEqual(4, events[1].Payload[0]);
			Assert.AreEqual(5, events[1].Payload[1]);
		}
		#endregion

		#region Mixed EventIDs and non-EventIDs
		public interface ITestLogWithIds
		{
			// the system should generate this with ID=2, since ID=1 is already in use
			void FirstWithNoId(string message);

			[Event(1)]
			void SecondWithId(string message);
		}

		[Test]
		public void InterfaceWithSomeEventIdsCanBeImplemented()
		{
			var testLog = EventSourceImplementer.GetEventSourceAs<ITestLogWithIds>();
			_listener.EnableEvents((EventSource)testLog, EventLevel.LogAlways, (EventKeywords)(-1));

			// do some logging
			testLog.FirstWithNoId("first");
			testLog.SecondWithId("second");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(2, events[0].EventId);
			Assert.AreEqual(1, events[1].EventId);
		}
		#endregion

		#region NonEvent Tests
		public interface INonEvent
		{
			[NonEvent]
			void NonEvent();

			[Event(99)]
			void Event();
		}

		[Test]
		public void NonEventsShouldNotBeLogged()
		{
			var listener = new TestEventListener();
			var testLog = EventSourceImplementer.GetEventSourceAs<INonEvent>();
			listener.EnableEvents((EventSource)testLog, EventLevel.LogAlways, (EventKeywords)(-1));

			// do some logging
			testLog.NonEvent();
			testLog.Event();

			// look at the events
			var events = listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(99, events[0].EventId);
		}
		#endregion

		#region General Tests
		public class NotAnEventSource
		{
		}

		[Test]
		public void ImplementFailsForNonEventSourceClass()
		{
			Assert.Throws<InvalidOperationException>(() => EventSourceImplementer.GetEventSourceAs<NotAnEventSource>());
		}
		#endregion

		#region Interface With Generic Types
		public interface ITaskService
		{
			Task<string> GetItem(string value);
		}

		public class TaskService : ITaskService
		{
			public Task<string> GetItem(string value) { return Task.FromResult(value); }
		}

		[Test]
		public void CanImplementInterfaceWithTaskReturn()
		{
			// this was causing issues with the completed method
			var log = EventSourceImplementer.GetEventSourceAs<ITaskService>();
			_listener.EnableEvents((EventSource)log, EventLevel.LogAlways, (EventKeywords)(-1));

			// check the service
			var service = new TaskService();
			Assert.AreEqual("foo", service.GetItem("foo").Result);

			// try a proxy
			var proxy = TracingProxy.Create<ITaskService>(service);
			var task = proxy.GetItem("foo");
			Assert.AreEqual("foo", task.Result);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(2, events.Length);

			// check the individual events to make sure the task came back in the payload
			Assert.AreEqual(1, events[1].Payload.Count);
			Assert.AreEqual(new JsonObjectSerializer().SerializeObject(task, new RuntimeMethodHandle(), 0), events[1].Payload[0]);
		}
		#endregion
	}
}
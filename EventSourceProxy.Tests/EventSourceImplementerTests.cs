﻿using EventSourceProxy.Tests.Properties;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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
			Assert.IsTrue(events[0].Keywords.HasFlag(TestLog.Keywords.Diagnostic));
			Assert.AreEqual(TestLog.Tasks.DBQuery, events[0].Task);
			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual("whoops!", events[0].Payload[0]);

			Assert.AreEqual(testLog, events[1].EventSource);
			Assert.AreEqual(4, events[1].EventId);
			Assert.AreEqual("Other Information: {0} {1}", events[1].Message);
			Assert.AreEqual(EventLevel.Informational, events[1].Level);
			Assert.IsTrue(events[1].Keywords.HasFlag(TestLog.Keywords.Perf));
			Assert.AreEqual(2, events[1].Payload.Count);
			Assert.AreEqual("doing something", events[1].Payload[0]);
			Assert.AreEqual(19, events[1].Payload[1]);

			Assert.AreEqual(testLog, events[2].EventSource);
			Assert.AreEqual(5, events[2].EventId);
			Assert.AreEqual("Direct Call: {0}", events[2].Message);
			Assert.AreEqual(EventLevel.Warning, events[2].Level);
			Assert.IsTrue(events[2].Keywords.HasFlag(TestLog.Keywords.Diagnostic));
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
			Assert.IsTrue(events[0].Keywords.HasFlag(TestLog.Keywords.Diagnostic));
			Assert.AreEqual(TestLog.Tasks.DBQuery, events[0].Task);
			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual("whoops!", events[0].Payload[0]);

			Assert.AreEqual(testLog, events[1].EventSource);
			Assert.AreEqual(4, events[1].EventId);
			Assert.AreEqual("Other Information: {0} {1}", events[1].Message);
			Assert.AreEqual(EventLevel.Informational, events[1].Level);
			Assert.IsTrue(events[1].Keywords.HasFlag(TestLog.Keywords.Perf));
			Assert.AreEqual(2, events[1].Payload.Count);
			Assert.AreEqual("doing something", events[1].Payload[0]);
			Assert.AreEqual(19, events[1].Payload[1]);

			Assert.AreEqual(testLog, events[2].EventSource);
			Assert.AreEqual(5, events[2].EventId);
			Assert.AreEqual("Direct Call: {0}", events[2].Message);
			Assert.AreEqual(EventLevel.Warning, events[2].Level);
			Assert.IsTrue(events[2].Keywords.HasFlag(TestLog.Keywords.Diagnostic));
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
			Assert.IsTrue(events[0].Keywords.HasFlag(KeywordsFoo.Startup));
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
			[Event(10, Message = "Startup: {0}", Level = EventLevel.Warning, Task = TasksFoo.Task1, Opcode = OpcodesFoo.Opcode1, Keywords = KeywordsFoo.Startup)]
			void Startup(string message);

			// this tests that the interface can return a value and things still work
			[Event(5, Message = "WithReturn: {0} {1}", Level = EventLevel.Warning, Task = TasksFoo.Task2, Opcode = OpcodesFoo.Opcode2, Keywords = KeywordsFoo.Return)]
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
			Assert.IsTrue(events[0].Keywords.HasFlag(KeywordsFoo.Startup));
			Assert.AreEqual(TasksFoo.Task1, events[0].Task);
			Assert.AreEqual(OpcodesFoo.Opcode1, events[0].Opcode);
			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual("starting!", events[0].Payload[0]);

			Assert.AreEqual(testLog, events[1].EventSource);
			Assert.AreEqual(5, events[1].EventId);
			Assert.AreEqual("WithReturn: {0} {1}", events[1].Message);
			Assert.AreEqual(EventLevel.Warning, events[1].Level);
			Assert.IsTrue(events[1].Keywords.HasFlag(KeywordsFoo.Return));
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
			_listener.EnableEvents((EventSource)testLog, EventLevel.LogAlways);

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
			Assert.AreEqual("{0} {1}", events[0].Message);
			Assert.AreEqual(EventLevel.Informational, events[0].Level);
			Assert.AreEqual(2, events[0].Payload.Count);
			Assert.AreEqual(2, events[0].Payload[0]);
			Assert.AreEqual(3, events[0].Payload[1]);

			Assert.AreEqual(testLog, events[1].EventSource);
			Assert.AreEqual(4, events[1].EventId);
			Assert.AreEqual("{0} {1}", events[1].Message);
			Assert.AreEqual(EventLevel.Informational, events[1].Level);
			Assert.AreEqual(2, events[1].Payload.Count);
			Assert.AreEqual(4, events[1].Payload[0]);
			Assert.AreEqual(5, events[1].Payload[1]);
		}
		#endregion

		#region Derived Interface Implementation
		// this is any old interface with no decoration
 		public interface IJustAnInterfaceDerived : IJustAnInterface
		{
			void DerivedMethod();
		}

		[Test]
		public void DerivedInterfaceCanBeImplemented()
		{
			var testLog = EventSourceImplementer.GetEventSourceAs<IJustAnInterfaceDerived>();
			_listener.EnableEvents((EventSource)testLog, EventLevel.LogAlways);

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
			Assert.AreEqual("{0} {1}", events[0].Message);
			Assert.AreEqual(EventLevel.Informational, events[0].Level);
			Assert.AreEqual(2, events[0].Payload.Count);
			Assert.AreEqual(2, events[0].Payload[0]);
			Assert.AreEqual(3, events[0].Payload[1]);

			Assert.AreEqual(testLog, events[1].EventSource);
			Assert.AreEqual(4, events[1].EventId);
			Assert.AreEqual("{0} {1}", events[1].Message);
			Assert.AreEqual(EventLevel.Informational, events[1].Level);
			Assert.AreEqual(2, events[1].Payload.Count);
			Assert.AreEqual(4, events[1].Payload[0]);
			Assert.AreEqual(5, events[1].Payload[1]);
		}

		// this is any old interface with no decoration
		public interface IJustAnInterfaceDerivedAgain : IJustAnInterfaceDerived
		{
			void DerivedAgainMethod();
		}

		[Test]
		public void DerivedAgainInterfaceCanBeImplemented()
		{
			var testLog = EventSourceImplementer.GetEventSourceAs<IJustAnInterfaceDerivedAgain>();
			_listener.EnableEvents((EventSource)testLog, EventLevel.LogAlways);

			// do some logging
			testLog.AddNumbers(2, 3);
			testLog.SubtractNumbers(4, 5);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(2, events.Length);

			// check the event source
			// make sure that the EventSource attribute makes it to the event source
			var eventSource = events[0].EventSource;
			Assert.AreEqual("IJustAnInterfaceDerivedAgain", eventSource.Name);

			// check the individual events
			Assert.AreEqual(testLog, events[0].EventSource);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual("{0} {1}", events[0].Message);
			Assert.AreEqual(EventLevel.Informational, events[0].Level);
			Assert.AreEqual(2, events[0].Payload.Count);
			Assert.AreEqual(2, events[0].Payload[0]);
			Assert.AreEqual(3, events[0].Payload[1]);

			Assert.AreEqual(testLog, events[1].EventSource);
			Assert.AreEqual(4, events[1].EventId);
			Assert.AreEqual("{0} {1}", events[1].Message);
			Assert.AreEqual(EventLevel.Informational, events[1].Level);
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
			_listener.EnableEvents((EventSource)testLog, EventLevel.LogAlways);

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
			listener.EnableEvents((EventSource)testLog, EventLevel.LogAlways);

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
			EventSourceImplementer.RegisterProvider<ITaskService>(new JsonObjectSerializer());
			var log = EventSourceImplementer.GetEventSourceAs<ITaskService>();
			_listener.EnableEvents((EventSource)log, EventLevel.LogAlways);

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
			Assert.AreEqual(new JsonObjectSerializer().SerializeObject(task, null), events[1].Payload[0]);
		}
		#endregion

		#region Interface with Duplicate Names
		public interface IHaveDuplicateNames
		{
			int Get(int i);
			string Get(string s);
		}

		public class HaveDuplicateNames : IHaveDuplicateNames
		{
			public int Get(int i) { return i; }
			public string Get(string s) { return s; }
		}

		[Test]
		public void CanImplementInterfaceWithDuplicateNames()
		{
			// this was causing issues with the completed method
			var log = EventSourceImplementer.GetEventSourceAs<IHaveDuplicateNames>();
			_listener.EnableEvents((EventSource)log, EventLevel.LogAlways);

			log.Get(1);
			log.Get("s");

			// try a proxy
			var proxy = TracingProxy.Create<IHaveDuplicateNames>(new HaveDuplicateNames());
			Assert.AreEqual(2, proxy.Get(2));
			Assert.AreEqual("foo", proxy.Get("foo"));

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(6, events.Length);
		}
		#endregion

		#region Interface Keywords
		public interface InterfaceWith45Methods
		{
			void Method01();
			void Method02();
			void Method03();
			void Method04();
			void Method05();
			void Method06();
			void Method07();
			void Method08();
			void Method09();
			void Method10();
			void Method11();
			void Method12();
			void Method13();
			void Method14();
			void Method15();
			void Method16();
			void Method17();
			void Method18();
			void Method19();
			void Method20();
			void Method21();
			void Method22();
			void Method23();
			void Method24();
			void Method25();
			void Method26();
			void Method27();
			void Method28();
			void Method29();
			void Method30();
			void Method31();
			void Method32();
			void Method33();
			void Method34();
			void Method35();
			void Method36();
			void Method37();
			void Method38();
			void Method39();
			void Method40();
			void Method41();
			void Method42();
			void Method43();
			void Method44();
			void Method45();
		}

		[Test]
		public void InterfaceWithManyMethodsDoesNotBreakKeywords()
		{
			// max keyword value in windows 8.1 is 0x0000100000000000 (44 bits)
			Assert.DoesNotThrow(() => EventSourceManifest.GenerateManifest(typeof(InterfaceWith45Methods)));
		}

		[EventSourceImplementation(AutoKeywords = true)]
		public interface InterfaceThatFolds
		{
			void BeginFoo();
			void EndFoo();
			void Foo();
			void FooAsync();
		}

		[Test]
		public void InterfaceWithSimilarMethodsFolds()
		{
			var manifest = EventSourceManifest.GenerateManifest(typeof(InterfaceThatFolds));

			// make sure there is only one keyword
			Assert.That(manifest.Contains("<keywords>\r\n  <keyword name=\"Foo\"  message=\"$(string.keyword_Foo)\" mask=\"0x1\"/>\r\n </keywords>"));
		}
		#endregion

		#region Complement Methods
		[EventSourceImplementation(ImplementComplementMethods=false)]
		class Foo
		{
		}
		#endregion

		#region Keyword Methods
		[EventSourceImplementation(AutoKeywords = true)]
		public interface IFoo
		{
			void Foo();
			void Bar();
		}

		[Test]
		public void Test()
		{
			Assert.AreEqual((EventKeywords)1, EventSourceImplementer.GetKeywordValue<IFoo>("Foo"));
			Assert.AreEqual((EventKeywords)2, EventSourceImplementer.GetKeywordValue<IFoo>("Bar"));
		}
		#endregion

		[EventSourceImplementation( Name = "ThrowsLog", ThrowOnEventWriteErrors = true )]
        public interface IThrowsLog
        {
            void Throw( string message );
        }

        [Test]
        public void EventSourceThrowsNotConfigured()
        {
            var subject = EventSourceImplementer.GetEventSourceAs<IFoo>();
            var fieldInfo = typeof(EventSource).GetField( "m_throwOnEventWriteErrors", BindingFlags.Instance | BindingFlags.NonPublic );
            var actual = (bool)fieldInfo.GetValue( subject );
            Assert.IsFalse( actual );
        }

        [Test]
        public void EventSourceThrowsConfigured()
        {
            var subject = EventSourceImplementer.GetEventSourceAs<IThrowsLog>();
            var fieldInfo = typeof(EventSource).GetField( "m_throwOnEventWriteErrors", BindingFlags.Instance | BindingFlags.NonPublic );
            var actual = (bool)fieldInfo.GetValue( subject );
            Assert.IsTrue( actual );
        }

        class ExternalTracerContext<T> : ExternalTracerContext
        {
            public ExternalTracerContext() : base( typeof(T) )
            {}
        }

        class ExternalTracerContext : IDisposable 
        {
            readonly Type sourceType;

            protected ExternalTracerContext( Type sourceType )
            {
                this.sourceType = sourceType;
                Initialize();
            }

            void Initialize()
            {
                Clear( false );
                Create();
                Start();
            }

            void Create()
            {
                var name = sourceType.Name;
                var path = Path.Combine( TestContext.CurrentContext.WorkDirectory, string.Format( "{0}.etl", name ) );
                if ( File.Exists( path ) )
                {
                    File.Delete( path );
                }
                var arguments = string.Format( @"create trace {0} -p {{{1}}} -o ""{2}"" -a", name, EventSourceManifest.GetGuid( sourceType ), path );
                Command( arguments );
            }

            void Start()
            {
                Command( string.Format( @"start {0}", sourceType.Name ) );
            }

            void Stop( bool throwOnException )
            {
                Command( string.Format( @"stop {0}", sourceType.Name ), throwOnException );
            }

            void Delete( bool throwOnException )
            {
                Command( string.Format( @"delete {0}", sourceType.Name ), throwOnException );
            }

            static void Command( string arguments, bool throwOnException = true )
            {
                var process = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false, 
                        RedirectStandardOutput = true, 
                        FileName = "logman", 
                        Arguments = arguments
                    }
                };
                process.Start();
                process.WaitForExit();

                if ( throwOnException && process.ExitCode != 0 )
                {
                    var output = process.StandardOutput.ReadToEnd();
                    throw new InvalidOperationException( output );
                }

            }

            public void Dispose()
            {
                Clear();
            }

            void Clear( bool throwOnException = true )
            {
                Stop( throwOnException );
                Delete( throwOnException );
            }
        }

        [Test, Ignore]
        public void EventSourceThrows()
        {
            var subject = EventSourceImplementer.GetEventSourceAs<IThrowsLog>();
            EnableLogging( subject );

            using ( new ExternalTracerContext<IThrowsLog>() )
            {
                subject.Throw( "Basic text" );

                Assert.Throws<EventSourceException>( () => subject.Throw( Resources.TooBig ) );

                Assert.AreEqual( 2, _listener.Events.Count );
            }
        }
	}
}
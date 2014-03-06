using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace EventSourceProxy.Tests
{
	[TestFixture]
	public class SerializationTests : BaseLoggingTest
	{
		#region Test Classes
		public class ClassData
		{
			public string Name { get; set; }
			public int Age { get; set; }

			public static ClassData Test = new ClassData() { Name = "Fred", Age = 38 };
			public static string TestJson = JsonConvert.SerializeObject(Test);
		}

		public struct StructData
		{
			public string Name { get; set; }
			public int Age { get; set; }

			public static StructData Test = new StructData() { Name = "Fred", Age = 38 };
			public static string TestJson = JsonConvert.SerializeObject(Test);
		}

		public interface ILogInterfaceWithClassData
		{
			void SendData(ClassData data);
		}

		public interface ILogInterfaceWithStructData
		{
			void SendData(StructData data);
		}

		public abstract class LogClassWithClassData : EventSource
		{
			public abstract void SendData(ClassData data);
		}

		public abstract class LogClassWithStructData : EventSource
		{
			public abstract void SendData(StructData data);
		}

		public interface ILogInterfaceWithClassData2
		{
			void SendData(ClassData data);
		}

		public class ILogClassWithClassData : ILogInterfaceWithClassData2
		{
			public void SendData(ClassData data)
			{
			}
		}
		#endregion

		#region Test Cases
		[Test]
		public void InterfaceWithClassShouldSerializeAsJson()
		{
			EventSourceImplementer.RegisterProvider<ILogInterfaceWithClassData>(new JsonObjectSerializer());
			var logger = EventSourceImplementer.GetEventSourceAs<ILogInterfaceWithClassData>();
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways);

			logger.SendData(ClassData.Test);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual(ClassData.TestJson, events[0].Payload[0]);
		}

		[Test]
		public void InterfaceWithStructShouldSerializeAsJson()
		{
			EventSourceImplementer.RegisterProvider<ILogInterfaceWithStructData>(new JsonObjectSerializer());
			var logger = EventSourceImplementer.GetEventSourceAs<ILogInterfaceWithStructData>();
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways);

			logger.SendData(StructData.Test);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual(StructData.TestJson, events[0].Payload[0]);
		}

		[Test]
		public void ClassWithClassShouldSerializeAsJson()
		{
			EventSourceImplementer.RegisterProvider<LogClassWithClassData>(new JsonObjectSerializer());
			var logger = EventSourceImplementer.GetEventSourceAs<LogClassWithClassData>();
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways);

			logger.SendData(ClassData.Test);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual(ClassData.TestJson, events[0].Payload[0]);
		}

		[Test]
		public void ClassWithStructShouldSerializeAsJson()
		{
			EventSourceImplementer.RegisterProvider<LogClassWithStructData>(new JsonObjectSerializer());
			var logger = EventSourceImplementer.GetEventSourceAs<LogClassWithStructData>();
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways);

			logger.SendData(StructData.Test);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual(StructData.TestJson, events[0].Payload[0]);
		}

		[Test]
		public void ClassImplementingAnInterfaceShouldSerializeData()
		{
			EventSourceImplementer.RegisterProvider<ILogInterfaceWithClassData2>(new JsonObjectSerializer());
			var logger = EventSourceImplementer.GetEventSourceAs<ILogInterfaceWithClassData2>();
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways);

			var proxy = TracingProxy.Create<ILogInterfaceWithClassData2>(new ILogClassWithClassData());

			proxy.SendData(ClassData.Test);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(2, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual(ClassData.TestJson, events[0].Payload[0]);
		}
		#endregion

		#region ToStringSerializer Tests
		public interface ILogInterfaceWithClassDataToString
		{
			void SendData(ClassData data);
		}

		[Test]
		public void InterfaceWithClassShouldSerializeToString()
		{
			// register the provider
			EventSourceImplementer.RegisterProvider<ILogInterfaceWithClassDataToString>(new ToStringObjectSerializer());

			var logger = EventSourceImplementer.GetEventSourceAs<ILogInterfaceWithClassDataToString>();
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways);

			logger.SendData(ClassData.Test);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual(ClassData.Test.ToString(), events[0].Payload[0]);
		}
		#endregion

		#region NullSerializer Tests
		public interface ILogInterfaceWithClassDataToNull
		{
			void SendData(ClassData data);
		}

		[Test]
		public void InterfaceWithClassShouldSerializeAsNull()
		{
			// register the provider
			EventSourceImplementer.RegisterProvider<ILogInterfaceWithClassDataToNull>(new NullObjectSerializer());

			var logger = EventSourceImplementer.GetEventSourceAs<ILogInterfaceWithClassDataToNull>();
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways);

			logger.SendData(ClassData.Test);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual(null, events[0].Payload[0]);
		}
		#endregion

		#region CustomSerializer Tests
		public interface ILogInterfaceWithClassDataToCustom
		{
			void SendData(ClassData data);
		}

		class CustomSerializer : TraceSerializationProvider
		{
			public override string SerializeObject(object value, TraceSerializationContext context)
			{
				throw new NotImplementedException();
			}

			public override EventLevel? GetEventLevelForContext(TraceSerializationContext context)
			{
				return null;
			}

			public override bool ShouldSerialize(TraceSerializationContext context)
			{
				return false;
			}
		}

		[Test]
		public void InterfaceWithClassShouldSerializeToCustom()
		{
			// register the provider
			EventSourceImplementer.RegisterProvider<ILogInterfaceWithClassDataToCustom>(new CustomSerializer());

			var logger = EventSourceImplementer.GetEventSourceAs<ILogInterfaceWithClassDataToCustom>();
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways);

			logger.SendData(ClassData.Test);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual(null, events[0].Payload[0]);
		}
		#endregion

		#region Provider Attribute Tests
		[TraceSerializationProvider(typeof(FakeSerializer))]
		public interface ILogInterfaceWithSerializationAttribute
		{
			void SendData(ClassData data);
		}

		[TraceSerializationProvider(typeof(FakeSerializer))]
		public interface ILogInterfaceWithAttribute2
		{
			void SendData(ClassData data);
		}

		public class FakeSerializer : TraceSerializationProvider
		{
			public override string SerializeObject(object value, TraceSerializationContext context)
			{
				return "nope";
			}
		}

		[Test]
		public void AttributeShouldDetermineSerializer()
		{
			var logger = EventSourceImplementer.GetEventSourceAs<ILogInterfaceWithSerializationAttribute>();
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways);

			logger.SendData(ClassData.Test);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual("nope", events[0].Payload[0]);
		}

		[Test]
		public void RegisterProviderShouldOverrideAttribute()
		{
			EventSourceImplementer.RegisterProvider<ILogInterfaceWithAttribute2>(new JsonObjectSerializer(EventLevel.Verbose));
			var logger = EventSourceImplementer.GetEventSourceAs<ILogInterfaceWithAttribute2>();

			_listener.EnableEvents((EventSource)logger, EventLevel.Informational);
			logger.SendData(ClassData.Test);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual(null, events[0].Payload[0]);

			_listener.Reset();
			_listener.EnableEvents((EventSource)logger, EventLevel.Verbose);
			logger.SendData(ClassData.Test);

			// look at the events
			events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual(ClassData.TestJson, events[0].Payload[0]);
		}
		#endregion

		#region Level Attribute Tests
		public interface ISerializeNormally
		{
			void SendData(ClassData data);
		}

		[TraceSerialization(EventLevel.Informational)]
		public interface ISerializeVerbosely
		{
			void SendData(ClassData data);
		}

		public interface ISerializeVerboselyByMethod
		{
			[TraceSerialization(EventLevel.Informational)]
			void SendData(ClassData data);
		}

		public interface ISerializeVerboselyByParameter
		{
			void SendData([TraceSerialization(EventLevel.Informational)]ClassData data);
		}

		[TraceSerialization(EventLevel.Informational)]
		public class ClassData2 : ClassData
		{
		}

		public interface ISerializeVerboselyByParameterClass
		{
			void SendData(ClassData2 data);
		}

		[Test]
		public void ShouldNormallyBeDisabledAtInfoAndEnabledAtVerbose()
		{
			var logger = EventSourceImplementer.GetEventSourceAs<ISerializeNormally>();
			_listener.EnableEvents((EventSource)logger, EventLevel.Informational);

			logger.SendData(ClassData.Test);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.IsNull(events[0].Payload[0]);

			_listener.Reset();
			_listener.EnableEvents((EventSource)logger, EventLevel.Verbose);
			logger.SendData(ClassData.Test);
			events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.IsNotNull(events[0].Payload[0]);
		}

		[Test]
		public void AttributeCanChangeLevelAtClassLevel()
		{
			var logger = EventSourceImplementer.GetEventSourceAs<ISerializeVerbosely>();
			_listener.EnableEvents((EventSource)logger, EventLevel.Informational);

			logger.SendData(ClassData.Test);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.IsNotNull(events[0].Payload[0]);
		}

		[Test]
		public void AttributeCanChangeLevelAtMethodLevel()
		{
			var logger = EventSourceImplementer.GetEventSourceAs<ISerializeVerboselyByMethod>();
			_listener.EnableEvents((EventSource)logger, EventLevel.Informational);

			logger.SendData(ClassData.Test);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.IsNotNull(events[0].Payload[0]);
		}

		[Test]
		public void AttributeCanChangeLevelAtParameterLevel()
		{
			var logger = EventSourceImplementer.GetEventSourceAs<ISerializeVerboselyByParameter>();
			_listener.EnableEvents((EventSource)logger, EventLevel.Informational);

			logger.SendData(ClassData.Test);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.IsNotNull(events[0].Payload[0]);
		}

		[Test]
		public void AttributeCanChangeLevelAtParameterTypeLevel()
		{
			var logger = EventSourceImplementer.GetEventSourceAs<ISerializeVerboselyByParameterClass>();
			_listener.EnableEvents((EventSource)logger, EventLevel.Informational);

			logger.SendData(new ClassData2() { Name = "Fred", Age = 38 });

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.IsNotNull(events[0].Payload[0]);
		}
		#endregion
	}
}

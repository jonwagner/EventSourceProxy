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
	public class SerializationTests : BaseLoggingTest
	{
		#region Test Classes
		public class ClassData
		{
			public string Name { get; set; }
			public int Age { get; set; }
		}

		public struct StructData
		{
			public string Name { get; set; }
			public int Age { get; set; }
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

		public class ILogClassWithClassData : ILogInterfaceWithClassData
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
			var logger = EventSourceImplementer.GetEventSourceAs<ILogInterfaceWithClassData>();
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways, (EventKeywords)(-1));

			logger.SendData(new ClassData() { Name = "Fred", Age = 38 });

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual("{\"Name\":\"Fred\",\"Age\":38}", events[0].Payload[0]);
		}

		[Test]
		public void InterfaceWithStructShouldSerializeAsJson()
		{
			EventSourceImplementer.RegisterProvider<ILogInterfaceWithStructData>(new JsonObjectSerializer());
			var logger = EventSourceImplementer.GetEventSourceAs<ILogInterfaceWithStructData>();
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways, (EventKeywords)(-1));

			logger.SendData(new StructData() { Name = "Fred", Age = 38 });

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual("{\"Name\":\"Fred\",\"Age\":38}", events[0].Payload[0]);
		}

		[Test]
		public void ClassWithClassShouldSerializeAsJson()
		{
			EventSourceImplementer.RegisterProvider<LogClassWithClassData>(new JsonObjectSerializer());
			var logger = EventSourceImplementer.GetEventSourceAs<LogClassWithClassData>();
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways, (EventKeywords)(-1));

			logger.SendData(new ClassData() { Name = "Fred", Age = 38 });

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual("{\"Name\":\"Fred\",\"Age\":38}", events[0].Payload[0]);
		}

		[Test]
		public void ClassWithStructShouldSerializeAsJson()
		{
			EventSourceImplementer.RegisterProvider<LogClassWithStructData>(new JsonObjectSerializer());
			var logger = EventSourceImplementer.GetEventSourceAs<LogClassWithStructData>();
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways, (EventKeywords)(-1));

			logger.SendData(new StructData() { Name = "Fred", Age = 38 });

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual("{\"Name\":\"Fred\",\"Age\":38}", events[0].Payload[0]);
		}

		[Test]
		public void ClassImplementingAnInterfaceShouldSerializeData()
		{
			EventSourceImplementer.RegisterProvider<ILogInterfaceWithClassData>(new JsonObjectSerializer());
			var logger = EventSourceImplementer.GetEventSourceAs<ILogInterfaceWithClassData>();
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways, (EventKeywords)(-1));

			var proxy = TracingProxy.Create<ILogInterfaceWithClassData>(new ILogClassWithClassData());

			proxy.SendData(new ClassData() { Name = "Fred", Age = 38 });

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(2, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual("{\"Name\":\"Fred\",\"Age\":38}", events[0].Payload[0]);
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
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways, (EventKeywords)(-1));

			var data = new ClassData() { Name = "Fred", Age = 38 };
			logger.SendData(data);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual(data.ToString(), events[0].Payload[0]);
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
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways, (EventKeywords)(-1));

			var data = new ClassData() { Name = "Fred", Age = 38 };
			logger.SendData(data);

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

		class CustomSerializer : ObjectSerializationProvider
		{
			public override string SerializeObject(object value, RuntimeMethodHandle methodHandle, int parameterIndex)
			{
				throw new NotImplementedException();
			}
			public override bool ShouldSerialize(System.Reflection.MethodInfo method, int parameterIndex)
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
			_listener.EnableEvents((EventSource)logger, EventLevel.LogAlways, (EventKeywords)(-1));

			var data = new ClassData() { Name = "Fred", Age = 38 };
			logger.SendData(data);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].EventId);
			Assert.AreEqual(null, events[0].Payload[0]);
		}
		#endregion
	}
}

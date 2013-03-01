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
	public class EventDataTypesTests
	{
		#region Tests for Built-in Types
		public enum FooEnum
		{
			Foo,
			Bar
		}

		public interface ITypeLog<T>
		{
			void Log(T t);
		}

		public static class TypeLogTester<T>
		{
			public static void Test(T t)
			{
				using (var testLog = (EventSource)EventSourceImplementer.GetEventSourceAs<ITypeLog<T>>())
				using (var listener = new TestEventListener())
				{
					listener.EnableEvents(testLog, EventLevel.LogAlways, (EventKeywords)(-1));

					ITypeLog<T> tLog = (ITypeLog<T>)testLog;
					tLog.Log(t);

					object value = listener.Events.Last().Payload[0];
					if (TypeImplementer.TypeIsSupportedByEventSource(typeof(T)))
						Assert.AreEqual(t, value);
					else
						Assert.AreEqual(t.ToString(), value);

					listener.DisableEvents(testLog);
				}
			}
		}

		[Test]
		public void BuiltInTypesCanBeLogged()
		{
			TypeLogTester<string>.Test("string");
			TypeLogTester<int>.Test(5);
			TypeLogTester<long>.Test(0x800000000);
			TypeLogTester<ulong>.Test(0x1800000000);
			TypeLogTester<byte>.Test(0x78);
			TypeLogTester<sbyte>.Test(0x20);
			TypeLogTester<short>.Test(0x1001);
			TypeLogTester<ushort>.Test(0x8010);
			TypeLogTester<float>.Test(1.234f);
			TypeLogTester<double>.Test(2.3456);
			TypeLogTester<bool>.Test(true);
			TypeLogTester<Guid>.Test(Guid.NewGuid());
			TypeLogTester<FooEnum>.Test(FooEnum.Bar);
			TypeLogTester<IntPtr>.Test(new IntPtr(1234));
			TypeLogTester<char>.Test('c');
			TypeLogTester<decimal>.Test(3.456m);

			TypeLogTester<int?>.Test(5);
			TypeLogTester<long?>.Test(0x800000000);
			TypeLogTester<ulong?>.Test(0x1800000000);
			TypeLogTester<byte?>.Test(0x78);
			TypeLogTester<sbyte?>.Test(0x20);
			TypeLogTester<short?>.Test(0x1001);
			TypeLogTester<ushort?>.Test(0x8010);
			TypeLogTester<float?>.Test(1.234f);
			TypeLogTester<double?>.Test(2.3456);
			TypeLogTester<bool?>.Test(true);
			TypeLogTester<Guid?>.Test(Guid.NewGuid());
			TypeLogTester<FooEnum?>.Test(FooEnum.Bar);
			TypeLogTester<IntPtr?>.Test(new IntPtr(1234));
			TypeLogTester<char?>.Test('c');
			TypeLogTester<decimal?>.Test(3.456m);

			TypeLogTester<int?>.Test(null);
			TypeLogTester<long?>.Test(null);
			TypeLogTester<ulong?>.Test(null);
			TypeLogTester<byte?>.Test(null);
			TypeLogTester<sbyte?>.Test(null);
			TypeLogTester<short?>.Test(null);
			TypeLogTester<ushort?>.Test(null);
			TypeLogTester<float?>.Test(null);
			TypeLogTester<double?>.Test(null);
			TypeLogTester<bool?>.Test(null);
			TypeLogTester<Guid?>.Test(null);
			TypeLogTester<FooEnum?>.Test(null);
			TypeLogTester<IntPtr?>.Test(null);
			TypeLogTester<char?>.Test(null);
			TypeLogTester<decimal?>.Test(null);
		}
		#endregion

		#region Serialized Types in Abstract Methods Tests
		public abstract class TypeLogWithSerializedTypesInAbstractMethod : EventSource
		{
			public abstract void LogIntPtr(IntPtr p);
			public abstract void LogChar(char c);
			public abstract void LogDecimal(decimal d);
		}

		[Test]
		public void BuiltInSerializedTypesCanBeLoggedInAbstractMethods()
		{
			var listener = new TestEventListener();
			var testLog = EventSourceImplementer.GetEventSourceAs<TypeLogWithSerializedTypesInAbstractMethod>();
			listener.EnableEvents(testLog, EventLevel.LogAlways, (EventKeywords)(-1));

			testLog.LogIntPtr(new IntPtr(1234)); Assert.AreEqual("1234", listener.Events.Last().Payload[0].ToString());
			testLog.LogChar('c'); Assert.AreEqual("c", listener.Events.Last().Payload[0].ToString());
			testLog.LogDecimal(3.456m); Assert.AreEqual("3.456", listener.Events.Last().Payload[0].ToString());
		}
		#endregion

		#region Serialized Types in Direct Methods Tests
		public class TypeLogWithSerializedTypesInDirectMethod : EventSource
		{
			public void LogIntPtr(IntPtr p) { WriteEvent(1, p.ToString()); }
			public void LogChar(char c) { WriteEvent(2, c.ToString()); }
			public void LogDecimal(decimal d) { WriteEvent(3, d.ToString()); }
		}

		[Test]
		public void BuiltInSerializedTypesCanBeLoggedInDirectMethods()
		{
			var listener = new TestEventListener();
			var testLog = EventSourceImplementer.GetEventSourceAs<TypeLogWithSerializedTypesInDirectMethod>();
			listener.EnableEvents(testLog, EventLevel.LogAlways, (EventKeywords)(-1));

			testLog.LogIntPtr(new IntPtr(1234)); Assert.AreEqual("1234", listener.Events.Last().Payload[0].ToString());
			testLog.LogChar('c'); Assert.AreEqual("c", listener.Events.Last().Payload[0].ToString());
			testLog.LogDecimal(3.456m); Assert.AreEqual("3.456", listener.Events.Last().Payload[0].ToString());
		}
		#endregion
	}
}

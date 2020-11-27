using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
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

		#region EventSource Implementation
		public abstract class EventSourceWithCollapse : EventSource
		{
			[TraceAsData]
			public abstract void TraceAsData(string p1, string p2);
		}

		[Test]
		public void CanApplyTraceAsDataToEventSourceMethods()
		{
			EnableLogging<EventSourceWithCollapse>();

			// do some logging
			var testLog = EventSourceImplementer.GetEventSourceAs<EventSourceWithCollapse>();
			testLog.TraceAsData("p1", "p2");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual("{\"p1\":\"p1\",\"p2\":\"p2\"}", events[0].Payload[0].ToString());
		}
		#endregion

		#region Provider Tests
		[TraceParameterProvider(typeof(MyCollapseAllTPP))]
		public interface InterfaceWithProvider
		{
			void TraceAsData(string p1, string p2);
		}

		public class MyCollapseAllTPP : TraceParameterProvider
		{
			public override IReadOnlyCollection<ParameterMapping> ProvideParameterMapping(System.Reflection.MethodInfo methodInfo)
			{
				var mappings = new List<ParameterMapping>();
				var mapping = new ParameterMapping("data");
				mappings.Add(mapping);
				foreach (var p in methodInfo.GetParameters().Reverse())
					mapping.AddSource(p);

				return mappings.AsReadOnly();
			}
		}

		[Test]
		public void CanReplaceParameterProvider()
		{
			EnableLogging<InterfaceWithProvider>();

			var log = EventSourceImplementer.GetEventSourceAs<InterfaceWithProvider>();
			log.TraceAsData("p1", "p2");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual("{\"p2\":\"p2\",\"p1\":\"p1\"}", events[0].Payload[0].ToString());
		}

		[TraceParameterProvider(typeof(MyEmptyTPP))]
		public interface InterfaceWithEmptyProvider
		{
			void TraceAsData(string p1, string p2);
		}

		public class MyEmptyTPP : TraceParameterProvider
		{
			public override IReadOnlyCollection<ParameterMapping> ProvideParameterMapping(System.Reflection.MethodInfo methodInfo)
			{
				var mappings = new List<ParameterMapping>();
				var mapping = new ParameterMapping("data");
				mappings.Add(mapping);

				// return a mapping with an empty source

				return mappings.AsReadOnly();
			}
		}

		[Test]
		public void CanReturnEmptySourceList()
		{
			EnableLogging<InterfaceWithEmptyProvider>();

			var log = EventSourceImplementer.GetEventSourceAs<InterfaceWithEmptyProvider>();
			log.TraceAsData("p1", "p2");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(0, events[0].Payload.Count);
		}

		public interface UntaggedInterface
		{
			void TraceAsData(string p1, string p2);
		}

		[Test]
		public void CanSetDefaultProvider()
		{
			EventSourceImplementer.RegisterDefaultProvider(new MyCollapseAllTPP());
			try
			{
				EnableLogging<UntaggedInterface>();

				var log = EventSourceImplementer.GetEventSourceAs<UntaggedInterface>();
				log.TraceAsData("p1", "p2");

				// look at the events
				var events = _listener.Events.ToArray();
				Assert.AreEqual(1, events.Length);
				Assert.AreEqual(1, events[0].Payload.Count);
				Assert.AreEqual("{\"p2\":\"p2\",\"p1\":\"p1\"}", events[0].Payload[0].ToString());
			}
			finally
			{
				EventSourceImplementer.RegisterDefaultProvider((TraceParameterProvider)null);
			}
		}
		#endregion


		#region Trace Transformations
		public class TraceMaskCreditCardsAttribute : TraceTransformAttribute
		{		
			public override MethodInfo GetTransformMethod(Type inputType)
			{
				return GetType().GetMethod("MaskAccount");
			}

			public static string MaskAccount(string input)
			{
				return input.Substring(0, 4) + new string('x', input.Length - 4);
			}
		}


		public interface ILogMaskingWithAttributes
		{
			void LogPayment(
				string MerchantName,
				[TraceMaskCreditCardsAttribute]
				string CreditCard,
				decimal Amount
				);
		}

		public interface ILogMaskingWithAttributes_InvalidApplication
		{
			void LogPayment(
				string MerchantName,
				string CreditCard,
				[TraceMaskCreditCardsAttribute]
				decimal Amount
				);
		}

		public class TraceTransformNullAttribute : TraceTransformAttribute
		{
			public override MethodInfo GetTransformMethod(Type inputType)
			{
				return null;
			}
		}

		public interface ILogWithNullAttribute
		{
			void Log([TraceTransformNull] string message);
		}

		public class TraceTransformNoInputAttribute : TraceTransformAttribute
		{
			public override MethodInfo GetTransformMethod(Type inputType)
			{
				return GetType().GetMethod("TestMethod");
			}

			public static string TestMethod()
			{
				return String.Empty;
			}
		}

		public interface ILogWithNoInputAttribute
		{
			void Log([TraceTransformNoInput] string message);
		}

		public class TraceTransformNoResponseAttribute : TraceTransformAttribute
		{
			public override MethodInfo GetTransformMethod(Type inputType)
			{
				return GetType().GetMethod("TestMethod");
			}

			public static void TestMethod(string input)
			{
				return;
			}
		}

		public interface ILogWithNoResponseAttribute
		{
			void Log([TraceTransformNoResponse] string message);
		}

		[Test]
		public void ValuesCanBeModifiedViaAttributes()
		{
			EnableLogging<ILogMaskingWithAttributes>();

			// do some logging
			var log = EventSourceImplementer.GetEventSourceAs<ILogMaskingWithAttributes>();
			log.LogPayment(MerchantName: "A Merchant", CreditCard: "1234123412341234", Amount: 10);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(3, events[0].Payload.Count);
			Assert.Contains("A Merchant", events[0].Payload);
			Assert.Contains("1234xxxxxxxxxxxx", events[0].Payload);
			Assert.Contains("10", events[0].Payload);
		}

		[Test]
		public void ValuesCanBeModifiedViaAttributes_InvalidApplication()
		{
			Assert.Throws<ArgumentException>(EnableLogging<ILogMaskingWithAttributes_InvalidApplication>);
		}

		[Test]
		public void ValuesCanBeModifiedViaAttributes_BadAttributes()
		{
			Assert.Throws<ArgumentNullException>(EnableLogging<ILogWithNullAttribute>);
			Assert.Throws<ArgumentException>(EnableLogging<ILogWithNoInputAttribute>);
			Assert.Throws<ArgumentException>(EnableLogging<ILogWithNoResponseAttribute>);
		}
		#endregion

		#region Exploding Parameters
		public class EmailChange
		{
			public string From { get; set; }
			public string To;
			public DateTime When { get; set; }

			public string GetDomain() { return To; }
		}

		public interface ILogEmailChangesWithAttributes
		{
			void LogChange(
				[TraceMember("From")]
				[TraceMember("To")]
				[TraceMember("When")]
				EmailChange email,
				[TraceIgnore] string ignored);
		}

		[TraceParameterProvider(typeof(ExpressionTPP))]
		public interface ILogEmailChangesWithTPP
		{
			void LogChange(EmailChange email);
		}

		[Test]
		public void ParametersCanBeExplodedByAttributes()
		{
			EnableLogging<ILogEmailChangesWithAttributes>();

			// do some logging
			var log = EventSourceImplementer.GetEventSourceAs<ILogEmailChangesWithAttributes>();
			var change = new EmailChange() { From = "me", To = "you", When = new DateTime(2010, 1, 1) };
			log.LogChange(change, "ignore me");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(3, events[0].Payload.Count);
			Assert.Contains(change.From, events[0].Payload);
			Assert.Contains(change.To, events[0].Payload);
			Assert.Contains(change.When.ToString(), events[0].Payload);
		}

		[Test]
		public void ParametersCanBeExplodedByProvider()
		{
			EnableLogging<ILogEmailChangesWithTPP>();

			// do some logging
			var log = EventSourceImplementer.GetEventSourceAs<ILogEmailChangesWithTPP>();
			var change = new EmailChange() { From = "me", To = "you", When = new DateTime(2010, 1, 1) };
			log.LogChange(change);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(4, events[0].Payload.Count);
			Assert.AreEqual(change.From, events[0].Payload[0].ToString());
			Assert.AreEqual(change.To, events[0].Payload[1].ToString());
			Assert.AreEqual(change.When.ToString(), events[0].Payload[2].ToString());
			Assert.AreEqual(change.To, events[0].Payload[3].ToString());
		}

		class ExpressionTPP : TraceParameterProvider
		{
			public override IReadOnlyCollection<ParameterMapping> ProvideParameterMapping(System.Reflection.MethodInfo methodInfo)
			{
				List<ParameterMapping> mapping = new List<ParameterMapping>();

				var email = methodInfo.GetParameters()[0];

				var from = new ParameterMapping("from");
				mapping.Add(from);
				from.AddSource(email, (EmailChange e) => e.From);

				var to = new ParameterMapping("to");
				to.AddSource(email, (EmailChange e) => e.To);
				mapping.Add(to);

				var when = new ParameterMapping("when");
				when.AddSource(email, (EmailChange e) => e.When);
				mapping.Add(when);

				var domain = new ParameterMapping("domain");
				domain.AddSource(email, (EmailChange e) => e.GetDomain());
				mapping.Add(domain);

				return mapping.AsReadOnly();
			}
		}

		[TraceParameterProvider(typeof(BadExpressionTPP))]
		public interface ILogEmailChangesWithBadExpressionTPP
		{
			void LogChange(EmailChange email);
		}

		class BadExpressionTPP : TraceParameterProvider
		{
			class OtherClass
			{
				public string AMethodNotShared()
				{
					return "boo";
				}
			}

			public override IReadOnlyCollection<ParameterMapping> ProvideParameterMapping(System.Reflection.MethodInfo methodInfo)
			{
				List<ParameterMapping> mapping = new List<ParameterMapping>();

				var email = methodInfo.GetParameters()[0];

				var from = new ParameterMapping("from");
				mapping.Add(from);
				from.AddSource(email, (OtherClass o) => o.AMethodNotShared());

				return mapping.AsReadOnly();
			}
		}

		[Test]
		public void BadExpressionGeneratesMeaningfulException()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				EnableLogging<ILogEmailChangesWithBadExpressionTPP>();

				// do some logging
				var log = EventSourceImplementer.GetEventSourceAs<ILogEmailChangesWithBadExpressionTPP>();
				var change = new EmailChange() { From = "me", To = "you", When = new DateTime(2010, 1, 1) };
				log.LogChange(change);
			});

		}
		#endregion

		#region Context Parameters
		[TraceParameterProvider(typeof(TPPWithContext))]
		public interface IHaveNoContext
		{
			void DoSomething(string message);
		}

		public class TPPWithContext : TraceParameterProvider
		{
			public override IReadOnlyCollection<ParameterMapping> ProvideParameterMapping(MethodInfo methodInfo)
			{
				List<ParameterMapping> mappings = base.ProvideParameterMapping(methodInfo).ToList();

				var mapping = new ParameterMapping("context");
				mapping.AddContext("context", () => GetContext());
				mappings.Add(mapping);

				return mappings.AsReadOnly();
			}

			public static int ContextCalls { get; private set; }

			public static string GetContext()
			{
				ContextCalls = ContextCalls + 1;
				return "testing";
			}
		}

		[Test]
		public void ContextCanBeProvidedByParameterProvider()
		{
			EnableLogging<IHaveNoContext>();

			var log = EventSourceImplementer.GetEventSourceAs<IHaveNoContext>();
			log.DoSomething("message");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(2, events[0].Payload.Count);
			Assert.AreEqual("message", events[0].Payload[0].ToString());
			Assert.AreEqual("testing", events[0].Payload[1].ToString());

			// make sure there is only one call to the context provider
			Assert.AreEqual(1, TPPWithContext.ContextCalls);
		}
		#endregion

		#region SubClass Tests
		public class BaseClass
		{
			public string Message { get; set; }
		}

		public class SubClass : BaseClass
		{
		}

		public interface ISubClassEventSource
		{
			void TestMessage(SubClass sc);
		}

		[Test]
		public void ParameterProviderAddsMappingForSubClassWhenBaseClassIsMapped()
		{
			var tpp = new TraceParameterProvider();
			tpp.ForAnything()
				.With<BaseClass>()
				.Trace(c => c.Message);

			var proxy2 = new TypeImplementer(typeof(ISubClassEventSource), tpp).EventSource;                

			Assert.DoesNotThrow(delegate
			{
				var proxy = new TypeImplementer(typeof(ISubClassEventSource), tpp).EventSource;                
			});
		}
		#endregion

		#region Issue33 Tests
		public enum RagStatus
		{
			Up,
			Down
		}

		public interface ITest33
		{
			[Event(1, Level = EventLevel.Informational)]
			void LogItServiceStatusEvent(string itService, RagStatus status);
		}

		[Test]
		public void Test33()
		{
			var tpp = new TraceParameterProvider();
			tpp.ForAnything().Trace((RagStatus r) => r.ToString());

			var proxy = (ITest33)new TypeImplementer(typeof(ITest33), tpp).EventSource;
			EnableLogging(proxy);

			// do some logging
			proxy.LogItServiceStatusEvent("it", RagStatus.Up);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(2, events[0].Payload.Count);
			Assert.AreEqual("it", events[0].Payload[0]);
			Assert.AreEqual("Up", events[0].Payload[1]);
		}
		#endregion
	}
}

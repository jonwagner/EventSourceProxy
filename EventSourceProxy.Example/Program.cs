using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using EventSourceProxy;

namespace EventSourceProxy.Example
{
	public class Foo
	{
		public virtual void Bar() {}
		public virtual int Bar2() { return 1; }
	}

	public class TestListener : EventListener
	{
		protected override void OnEventWritten(EventWrittenEventArgs eventData)
		{
			Console.Write("Activity: {0} ", Trace.CorrelationManager.ActivityId);
			Console.WriteLine(eventData.Message, eventData.Payload.ToArray());
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			// create the log
			var log = EventSourceImplementer.GetEventSourceAs<IExampleLogSource>();
			EventSource es = (EventSource)log;
			Console.WriteLine("Provider GUID = {0}", es.Guid);

			// create a listener
			var listener = new TestListener();
			listener.EnableEvents(es, EventLevel.LogAlways, (EventKeywords)(-1));

			using (new EventActivityScope())
			{
				log.Starting();
				for (int i = 0; i < 10; i++)
				{
					using (new EventActivityScope())
					{
						log.AnEvent(String.Format("i = {0}", i));
					}
				}
				log.Stopping();
			}

			TracingProxy.Create<Foo>(new Foo()).Bar();
			TracingProxy.Create<Foo>(new Foo()).Bar2();
		}
	}
}

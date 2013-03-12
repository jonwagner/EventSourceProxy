using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy.Example
{
	public interface IExampleLogSource
	{
		[Event(1, Message="Starting")]
		void Starting();
		void AnEvent(string data);
		[Event(2, Message = "Stopping")]
		void Stopping();
	}
}

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
		void Starting();
		void AnEvent(string data);
		void Stopping();
	}
}

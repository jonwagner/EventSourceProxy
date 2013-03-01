using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace EventSourceProxy.Tests
{
	public class BaseLoggingTest
	{
		#region Setup and TearDown
		internal TestEventListener _listener;

		[SetUp]
		public void SetUp()
		{
			_listener = new TestEventListener();
		}
		#endregion
	}
}

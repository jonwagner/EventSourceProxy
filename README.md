# EventSourceProxy #

**EventSourceProxy** (ESP) is the easiest way to add scalable Event Tracing for Windows (ETW) logging to your .NET program.

**Now in NuGet!**

[Get EventSourceProxy (ESP) from NuGet](http://nuget.org/packages/EventSourceProxy)


Follow [@jonwagnerdotcom](http://twitter.com/#!jonwagnerdotcom) for latest updates on this library or [code.jonwagner.com](http://code.jonwagner.com) for more detailed writeups.

## Why You Want This ##

- You really should be logging more than you do now.
- ETW is the best way to log in Windows.
- It's about zero effort to add logging to new code.
- It's zero effort to add logging to existing interfaces.
- Generated IL keeps overhead low, and it's almost nothing if tracing is off.

Here is ESP implementing a logging interface for you automatically:

	public interface ILog
	{
		void SomethingIsStarting(string message);
		void SomethingIsFinishing(string message);
	}

	// yeah, this is it
	var log = EventSourceImplementer.GetEventSourceAs<ILog>();

	log.SomethingIsStarting("hello");
	log.SomethingIsFinishing("goodbye");

Here is ESP doing the hard work of implementing an EventSource if you really want to do that:

	public abstract MyEventSource : EventSource
	{
		public abstract void SomethingIsStarting(string message);
		public abstract void SomethingIsFinishing(string message);
	}

	// ESP does the rest
	var log = EventSourceImplementer.GetEventSourceAs<MyEventSource>();

Here is ESP wrapping an existing interface for tracing:

	public interface ICalculator
	{
		void Clear();
		int Add(int x, int y);
		int Multiple(int x, int y);
	}

	public class Calculator : ICalculator
	{
		// blah blah
	}

	Calculator calculator = new Calculator();

	// again, that's it
	ICalculator proxy = TracingProxy.CreateWithActivityScope<ICalculator>(calculator);

	// all calls are automatically logged when the ETW source is enabled
	int total = proxy.Add(1, 2);

# Features #

* Automatically implement logging for any interface, class derived from EventSource.
* Takes all of the boilerplate code out of implementing an EventSource.
* Supports EventSource and Event attributes for controlling the generated events.
* Supports reusing Keyword, Opcode, and Task enums in multiple log sources.
* Automatically proxy any interface and create a logging source.
* Proxies also implement _Completed and _Faulted events.
* Automatically convert complex types to JSON strings for logging.
* Optionally override the object serializer.
* Optionally provide a logging context across an entire logging interface.
* Easily manage Activity IDs with EventActivityScope.

# Documentation #

**Full documentation is available on the [wiki](https://github.com/jonwagner/EventSourceProxy/wiki)!**

# Good References #

* Want to get the data out of ETW? Use the [Microsoft Enterprise Library Semantic Logging Application Block](http://nuget.org/packages/EnterpriseLibrary.SemanticLogging/). It has ETW listeners to log to the console, rolling flat file, and databases, so you can integrate ETW with your existing log destinations.
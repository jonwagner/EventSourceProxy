using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// A thread-static bag of data that can be dropped into any trace.
	/// </summary>
	public sealed class TraceContext : IDisposable
	{
		#region Properties
		/// <summary>
		/// The slot we use to store our data.
		/// </summary>
		private static AsyncLocal<TraceContext> _storage = new AsyncLocal<TraceContext>();

		/// <summary>
		/// An outer context.
		/// </summary>
		private TraceContext _baseContext;

		/// <summary>
		/// The dictionary containing the data.
		/// </summary>
		private Dictionary<string, object> _data = new Dictionary<string, object>();
		#endregion

		/// <summary>
		/// Initializes a new instance of the TraceContext class.
		/// </summary>
		/// <param name="baseContext">The base context.</param>
		private TraceContext(TraceContext baseContext)
		{
			_baseContext = baseContext;
		}

		/// <summary>
		/// Gets or sets logging values in this scope.
		/// </summary>
		/// <param name="key">The key in the data dictionary.</param>
		/// <returns>The value associated with the key.</returns>
		public object this[string key]
		{
			get
			{
				object value = null;
				if (_data.TryGetValue(key, out value))
					return value;

				if (_baseContext != null)
					return _baseContext[key];

				return null;
			}

			set
			{
				_data[key] = value;
			}
		}

		/// <summary>
		/// Starts a new TraceContext scope.
		/// </summary>
		/// <returns>The new TraceContext that can be filled in.</returns>
		public static TraceContext Begin()
		{
			return _storage.Value = new TraceContext(_storage.Value);
		}

		/// <summary>
		/// Gets a value associated with the given key in the current scope.
		/// </summary>
		/// <param name="key">The key to look up.</param>
		/// <returns>The value associated with the key, or null of the value was not set.</returns>
		public static object GetValue(string key)
		{
			return _storage.Value?[key];
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			End();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Ends the TraceContext scope.
		/// </summary>
		private void End()
		{
			if (_storage.Value != this)
				throw new ApplicationException("TraceContext chain has been broken.");

			_storage.Value = _baseContext;
		}
	}
}

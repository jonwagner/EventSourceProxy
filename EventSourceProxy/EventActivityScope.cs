using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Manages the lifetime of an ETW Activity ID.
	/// </summary>
	public sealed class EventActivityScope : IDisposable
	{
		#region Private Members
		/// <summary>
		/// The current activity scope.
		/// </summary>
		[ThreadStatic]
		private static EventActivityScope _currentActivityScope = null;

		/// <summary>
		/// The Activity ID outside of this scope. It is restored on the disposal of the scope.
		/// </summary>
		private Guid _previousActivityId;

		/// <summary>
		/// The Activity ID of this scope.
		/// </summary>
		private Guid _activityId;

		/// <summary>
		/// The previous activity scope.
		/// </summary>
		private EventActivityScope _previousActivityScope;
		#endregion

		#region Constructors
		/// <summary>
		/// Initializes a new instance of the EventActivityScope class.
		/// A new Activity ID is generated and assigned to the current thread.
		/// </summary>
		public EventActivityScope()
			: this(false)
		{
		}

		/// <summary>
		/// Initializes a new instance of the EventActivityScope class.
		/// A new Activity ID is generated and assigned to the current thread.
		/// </summary>
		/// <param name="reuseExistingActivityId">
		/// True to reuse an existing Activity ID if one is already in use.
		/// Since EventSource currently does not support Activity ID transfer, you
		/// may want to call this constructor with 'true' so you are sure to have an Activity ID,
		/// but to keep the current activity if one has already been established.
		/// </param>
		public EventActivityScope(bool reuseExistingActivityId)
		{
			_previousActivityScope = _currentActivityScope;
			_currentActivityScope = this;

			_previousActivityId = UnsafeNativeMethods.GetActivityId();

			if (!reuseExistingActivityId || _previousActivityId == Guid.Empty)
			{
				_activityId = Guid.NewGuid();
				UnsafeNativeMethods.SetActivityId(_activityId);
			}
			else
				_activityId = _previousActivityId;
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets the current Activity scope.
		/// </summary>
		public static EventActivityScope Current
		{
			get
			{
				return _currentActivityScope;
			}
		}

		/// <summary>
		/// Gets the current Activity Id.
		/// </summary>
		public static Guid CurrentActivityId
		{
			get
			{
				if (_currentActivityScope != null)
					return _currentActivityScope._activityId;

				return UnsafeNativeMethods.GetActivityId();
			}
		}

		/// <summary>
		/// Gets the Activity ID of the enclosing scope.
		/// </summary>
		public Guid PreviousActivityId { get { return _previousActivityId; } }

		/// <summary>
		/// Gets a value indicating whether the Activity ID of the enclosing scope.
		/// </summary>
		public bool IsNewScope { get { return _previousActivityId == Guid.Empty; } }

		/// <summary>
		/// Gets the Activity ID of this scope.
		/// </summary>
		public Guid ActivityId { get { return _activityId; } }
		#endregion

		/// <summary>
		/// Disposes the current Activity Scope by restoring the previous scope.
		/// </summary>
		public void Dispose()
		{
			UnsafeNativeMethods.SetActivityId(_previousActivityId);

			_currentActivityScope = _previousActivityScope;
		}
	}
}

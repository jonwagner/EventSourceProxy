using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Implements a given type as an EventSource.
	/// </summary>
	[SuppressMessage("Microsoft.StyleCop.CSharp.OrderingRules", "SA1204:StaticElementsMustAppearBeforeInstanceElements", Justification = "This ordering is most appropriate for the structure of the code.")]
	class TypeImplementer
	{
		#region Private Fields
		/// <summary>
		/// The suffix for the _Completed methods.
		/// </summary>
		internal const string CompletedSuffix = "_Completed";

		/// <summary>
		/// The suffix for the _Faulted methods.
		/// </summary>
		internal const string FaultedSuffix = "_Faulted";

		/// <summary>
		/// The name of the Keywords class.
		/// </summary>
		private const string Keywords = "Keywords";
		
		/// <summary>
		/// The name of the Opcodes class.
		/// </summary>
		private const string Opcodes = "Opcodes";

		/// <summary>
		/// The name of the Tasks class.
		/// </summary>
		private const string Tasks = "Tasks";

		/// <summary>
		/// The name of the context parameter.
		/// </summary>
		private const string Context = "Context";

		/// <summary>
		/// The WriteEvent method for EventSource.
		/// </summary>
		private static MethodInfo _writeEvent = typeof(EventSource).GetMethod("WriteEvent", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, new[] { typeof(int), typeof(object[]) }, null);

		/// <summary>
		/// The IsEnabled method for EventSource.
		/// </summary>
		private static MethodInfo _isEnabled = typeof(EventSource).GetMethod("IsEnabled", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, new[] { typeof(EventLevel), typeof(EventKeywords) }, null);

		/// <summary>
		/// The type being implemented.
		/// </summary>
		private Type _interfaceType;

		/// <summary>
		/// The type builder being used for the new type.
		/// </summary>
		private TypeBuilder _typeBuilder;

		/// <summary>
		/// The field containing the context provider.
		/// </summary>
		private FieldBuilder _contextProviderField;

		/// <summary>
		/// The field containing the serialization provider.
		/// </summary>
		private FieldBuilder _serializationProviderField;

		/// <summary>
		/// Provides the event attributes for the event methods.
		/// </summary>
		private EventAttributeProvider _eventAttributeProvider;

		/// <summary>
		/// The context provider for this type.
		/// </summary>
		private TraceContextProvider _contextProvider;

		/// <summary>
		/// The serialization provider for this type.
		/// </summary>
		private TraceSerializationProvider _serializationProvider;

		/// <summary>
		/// The list of invocation contexts during code generation.
		/// </summary>
		private List<InvocationContext> _invocationContexts = new List<InvocationContext>();

		/// <summary>
		/// The static field holding the invocation contexts at runtime.
		/// </summary>
		private FieldBuilder _invocationContextsField;
		#endregion

		#region Public Methods
		/// <summary>
		/// Initializes a new instance of the TypeImplementer class.
		/// </summary>
		/// <param name="interfaceType">The type to implement.</param>
		public TypeImplementer(Type interfaceType)
		{
			_interfaceType = interfaceType;
			_contextProvider = ProviderManager.GetProvider<TraceContextProvider>(interfaceType, typeof(TraceContextProviderAttribute), null);
			_serializationProvider = TraceSerializationProvider.GetSerializationProvider(interfaceType);
			_eventAttributeProvider = ProviderManager.GetProvider<EventAttributeProvider>(interfaceType, typeof(EventAttributeProviderAttribute), () => new EventAttributeProvider());

			// only interfaces support context
			if (_contextProvider != null && !_interfaceType.IsInterface)
				throw new InvalidOperationException("Context Providers can only be added to interface-based logs.");

			ImplementType();
		}
		#endregion

		/// <summary>
		/// Gets the EventSource created by this implementer.
		/// </summary>
		public EventSource EventSource { get; private set; }

		#region Helper Functions
		/// <summary>
		/// Returns true if a given type is supported as a parameter to EventSource.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>True if the type can be sent to EventSource, false if it needs to be encoded.</returns>
		internal static bool TypeIsSupportedByEventSource(Type type)
		{
			if (type == typeof(string)) return true;
			if (type == typeof(int)) return true;
			if (type == typeof(long)) return true;
			if (type == typeof(ulong)) return true;
			if (type == typeof(byte)) return true;
			if (type == typeof(sbyte)) return true;
			if (type == typeof(short)) return true;
			if (type == typeof(ushort)) return true;
			if (type == typeof(float)) return true;
			if (type == typeof(double)) return true;
			if (type == typeof(bool)) return true;
			if (type == typeof(Guid)) return true;
			if (type.IsEnum) return true;

			return false;
		}

		/// <summary>
		/// Given a type, returns the type that EventSource supports.
		/// This dereferences pointers and converts unsupported types to strings.
		/// </summary>
		/// <param name="type">The type to translate.</param>
		/// <returns>The associated type that EventSource supports.</returns>
		internal static Type GetTypeSupportedByEventSource(Type type)
		{
			if (TypeIsSupportedByEventSource(type))
				return type;

			if (type.IsByRef && TypeIsSupportedByEventSource(type.GetElementType()))
				return type.GetElementType();

			return typeof(string);
		}
		#endregion

		#region Type Implementation
		/// <summary>
		/// Implement the type.
		/// </summary>
		private void ImplementType()
		{
			// create a new assembly
			AssemblyName an = Assembly.GetExecutingAssembly().GetName();
			an.Name = ProxyHelper.AssemblyName;
			AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
			ModuleBuilder mb = ab.DefineDynamicModule(an.Name);

			// create a type based on EventSource and call the default constructor
			if (_interfaceType.IsSubclassOf(typeof(EventSource)))
				_typeBuilder = mb.DefineType(_interfaceType.FullName + "_Implemented", TypeAttributes.Class | TypeAttributes.Public, _interfaceType);
			else if (_interfaceType.IsInterface)
				_typeBuilder = mb.DefineType(_interfaceType.FullName + "_Implemented", TypeAttributes.Class | TypeAttributes.Public, typeof(EventSource), new Type[] { _interfaceType });
			else
				_typeBuilder = mb.DefineType(_interfaceType.FullName + "_Implemented", TypeAttributes.Class | TypeAttributes.Public, typeof(EventSource));

			// assign an EventSource attribute to the type so it gets the original name and guid
			_typeBuilder.SetCustomAttribute(EventSourceAttributeHelper.GetEventSourceAttributeBuilder(_interfaceType));

			// implement the fields and constructor
			EmitFields();

			// find all of the methods that need to be implemented
			var interfaceMethods = ProxyHelper.DiscoverMethods(_interfaceType);
			var implementationAttribute = _interfaceType.GetCustomAttribute<EventSourceImplementationAttribute>() ?? new EventSourceImplementationAttribute();

			// find the first free event id, in case we need to assign some ourselves
			int eventId = interfaceMethods
				.Select(m => m.GetCustomAttribute<EventAttribute>())
				.Where(a => a != null)
				.Select(a => a.EventId)
				.DefaultIfEmpty(0)
				.Max() + 1;

			// if there isn't a keyword class, then auto-generate the keywords
			bool hasKeywords = (implementationAttribute.Keywords != null) || (FindNestedType(_interfaceType, "Keywords") != null);
			ulong autoKeyword = hasKeywords ? (ulong)0 : 1;

			// for each method on the interface, try to implement it with a call to eventsource
			Dictionary<string, ulong> autoKeywords = new Dictionary<string, ulong>();
			foreach (MethodInfo interfaceMethod in interfaceMethods)
			{
				var invocationContext = new InvocationContext(interfaceMethod, InvocationContextType.MethodCall);

				var beginMethod = EmitMethodImpl(invocationContext, ref eventId, (EventKeywords)autoKeyword);
				var faultedMethod = EmitMethodFaultedImpl(invocationContext, beginMethod, ref eventId, (EventKeywords)autoKeyword);
				EmitMethodCompletedImpl(invocationContext, beginMethod, ref eventId, (EventKeywords)autoKeyword, faultedMethod);

				// shift to the next keyword
				autoKeywords.Add(beginMethod.Name, autoKeyword);
				autoKeyword <<= 1;
			}

			// create the type
			Type t = _typeBuilder.CreateType();

			// define the internal enum classes if they are defined
			if (hasKeywords)
				EmitEnumImplementation(implementationAttribute.Keywords, Keywords, typeof(EventKeywords));
			else
				EmitKeywordImpl(autoKeywords);
			EmitEnumImplementation(implementationAttribute.OpCodes, Opcodes, typeof(EventOpcode));
			EmitEnumImplementation(implementationAttribute.Tasks, Tasks, typeof(EventTask));

			// initialize the static fields
			t.GetField(_invocationContextsField.Name, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, _invocationContexts.ToArray());
			t.GetField(_serializationProviderField.Name, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, _serializationProvider);
			t.GetField(_contextProviderField.Name, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, _contextProvider);

			// instantiate the singleton
			EventSource = (EventSource)t.GetConstructor(Type.EmptyTypes).Invoke(null);

			// fill in the event source for all of the invocation contexts
			foreach (var context in _invocationContexts)
				context.EventSource = EventSource;
		}

		/// <summary>
		/// Emit the internal fields.
		/// </summary>
		private void EmitFields()
		{
			// static fields
			_invocationContextsField = _typeBuilder.DefineField("_invocationContexts", typeof(InvocationContext[]), FieldAttributes.Private | FieldAttributes.Static);
			_serializationProviderField = _typeBuilder.DefineField("_serializationProvider", typeof(TraceSerializationProvider), FieldAttributes.Private | FieldAttributes.Static);
			_contextProviderField = _typeBuilder.DefineField("_contextProvider", typeof(TraceContextProvider), FieldAttributes.Private | FieldAttributes.Static);
		}
		#endregion

		#region Method Implementation
		/// <summary>
		/// Emits an implementation of a given method.
		/// </summary>
		/// <param name="invocationContext">The InvocationContext for this call.</param>
		/// <param name="eventId">The next eventID to use.</param>
		/// <param name="autoKeyword">The auto-keyword to use if enabled.</param>
		/// <returns>The method that is implemented.</returns>
		private MethodBuilder EmitMethodImpl(InvocationContext invocationContext, ref int eventId, EventKeywords autoKeyword)
		{
			var interfaceMethod = invocationContext.MethodInfo;

			// look at the parameters on the interface
			var parameters = interfaceMethod.GetParameters();
			var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

			// some types aren't supported in event source. we will convert them to strings
			var targetParameters = parameterTypes.Select(t => GetTypeSupportedByEventSource(t)).ToList();
			
			// if we are implementing an interface, then add an string context parameter
			bool supportsContext = SupportsContext(invocationContext);
			if (supportsContext)
				targetParameters.Add(typeof(string));
			var targetParameterTypes = targetParameters.ToArray();

			// calculate the method name
			// if there is more than one method with the given name, then append an ID to it
			var methodName = interfaceMethod.Name;
			var matchingMethods = interfaceMethod.DeclaringType.GetMethods().AsEnumerable().Where(im => String.Compare(im.Name, methodName, StringComparison.OrdinalIgnoreCase) == 0).ToArray();
			if (matchingMethods.Length > 1)
				methodName += "_" + Array.IndexOf(matchingMethods, interfaceMethod).ToString(CultureInfo.InvariantCulture);

			// determine if this is a non-event or an event
			// if an event, but there is no event attribute, just add one to the last event id
			EventAttribute eventAttribute = null;
			if (interfaceMethod.GetCustomAttribute<NonEventAttribute>() == null)
			{
				eventAttribute = _eventAttributeProvider.GetEventAttribute(invocationContext, eventId);
				eventId = Math.Max(eventId, eventAttribute.EventId + 1);
			}

			// if auto-keywords are enabled, use them
			if (eventAttribute != null && eventAttribute.Keywords == EventKeywords.None)
				eventAttribute.Keywords = autoKeyword;

			// create the internal method that calls WriteEvent
			// this cannot be virtual or static, or the manifest builder will skip it
			// it also cannot return a value
			MethodBuilder m = _typeBuilder.DefineMethod(methodName, MethodAttributes.Public, typeof(void), targetParameterTypes);

			// copy the Event or NonEvent attribute from the interface
			if (eventAttribute != null)
				m.SetCustomAttribute(EventAttributeHelper.ConvertEventAttributeToAttributeBuilder(eventAttribute));
			else
				m.SetCustomAttribute(EventAttributeHelper.CreateNonEventAttribute());

			// add the parameter names
			for (int i = 0; i < parameters.Length; i++)
			{
				var parameter = parameters[i];
				m.DefineParameter(i + 1, parameter.Attributes, parameter.Name);
			}

			// add the context parameter
			if (supportsContext)
				m.DefineParameter(parameters.Length + 1, ParameterAttributes.In, Context);

			if (interfaceMethod.IsAbstract)
			{
				// for interface methods, implement a call to write event
				EmitCallWriteEvent(invocationContext, m, eventAttribute, parameterTypes, targetParameterTypes);

				// since EventSource only accepts non-virtual methods, and we need a virtual method to implement the abstract method
				// we need to implement a wrapper method on the interface that calls into the base method
				MethodBuilder im = _typeBuilder.DefineMethod("_" + methodName, MethodAttributes.Public | MethodAttributes.Virtual);
				ProxyHelper.CopyMethodSignature(interfaceMethod, im);
				ProxyHelper.EmitDefaultValue(im.GetILGenerator(), im.ReturnType);
				if (EmitIsEnabled(im, eventAttribute))
					EmitDirectProxy(invocationContext, im, m, parameterTypes, targetParameterTypes);

				// map our method to the interface implementation
				_typeBuilder.DefineMethodOverride(im, interfaceMethod);
			}
			else if (interfaceMethod.DeclaringType.IsSubclassOf(typeof(EventSource)))
			{
				// if we are implementing an event source, then
				// for non-abstract methods we just proxy the base implementation
				EmitDirectProxy(invocationContext, m, interfaceMethod, parameterTypes, targetParameterTypes);
			}
			else
			{
				// the base class is not an event source, so we are creating an eventsource-derived class
				// that just logs the event
				// so we need to call write event
				// call IsEnabled with the given event level and keywords to check whether we should log
				ProxyHelper.EmitDefaultValue(m.GetILGenerator(), m.ReturnType);
				if (EmitIsEnabled(m, eventAttribute))
					EmitCallWriteEvent(invocationContext, m, eventAttribute, parameterTypes, targetParameterTypes);
			}

			return m;
		}

		/// <summary>
		/// Emits a _Completed version of a given event that logs the result of an operation.
		/// The _Completed event is used by TracingProxy to signal the end of a method call.
		/// </summary>
		/// <param name="invocationContext">The InvocationContext for this call.</param>
		/// <param name="beginMethod">The begin method for this interface call.</param>
		/// <param name="eventId">The next available event ID.</param>
		/// <param name="autoKeyword">The auto-keyword to use if enabled.</param>
		/// <param name="faultedMethod">A faulted method to call or null if no other faulted method is available.</param>
		/// <returns>The MethodBuilder for the method.</returns>
		private MethodBuilder EmitMethodCompletedImpl(InvocationContext invocationContext, MethodInfo beginMethod, ref int eventId, EventKeywords autoKeyword, MethodBuilder faultedMethod)
		{
			return EmitMethodComplementImpl(invocationContext.SpecifyType(InvocationContextType.MethodCompletion), CompletedSuffix, invocationContext.MethodInfo.ReturnType, beginMethod, ref eventId, autoKeyword, faultedMethod);
		}

		/// <summary>
		/// Emits a _Faulted version of a given event that logs the result of an operation.
		/// The _Completed event is used by TracingProxy to signal an exception in a method call.
		/// </summary>
		/// <param name="invocationContext">The InvocationContext for this call.</param>
		/// <param name="beginMethod">The begin method for this interface call.</param>
		/// <param name="eventId">The next available event ID.</param>
		/// <param name="autoKeyword">The auto-keyword to use if enabled.</param>
		/// <returns>The MethodBuilder for the method.</returns>
		private MethodBuilder EmitMethodFaultedImpl(InvocationContext invocationContext, MethodInfo beginMethod, ref int eventId, EventKeywords autoKeyword)
		{
			return EmitMethodComplementImpl(invocationContext.SpecifyType(InvocationContextType.MethodFaulted), FaultedSuffix, typeof(Exception), beginMethod, ref eventId, autoKeyword, null);
		}

		/// <summary>
		/// Emits a method to complement an interface method. The complement method will have a suffix such as _Completed,
		/// and will take one parameter.
		/// </summary>
		/// <param name="invocationContext">The InvocationContext for this call.</param>
		/// <param name="suffix">The suffix to use on the method.</param>
		/// <param name="parameterType">The type of the parameter of the method.</param>
		/// <param name="beginMethod">The begin method for this interface call.</param>
		/// <param name="eventId">The next available event ID.</param>
		/// <param name="autoKeyword">The auto-keyword to use if enabled.</param>
		/// <param name="faultedMethod">A faulted method to call or null if no other faulted method is available.</param>
		/// <returns>The MethodBuilder for the method.</returns>
		private MethodBuilder EmitMethodComplementImpl(InvocationContext invocationContext, string suffix, Type parameterType, MethodInfo beginMethod, ref int eventId, EventKeywords autoKeyword, MethodBuilder faultedMethod)
		{
			var interfaceMethod = invocationContext.MethodInfo;

			// if there is a NonEvent attribute, then no need to emit this method
			if (interfaceMethod.GetCustomAttribute<NonEventAttribute>() != null)
				return null;

			// if the method ends in _Completed, then don't emit another one
			if (interfaceMethod.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
				return null;

			var methodName = beginMethod.Name + suffix;

			// if the interface already has a _Completed method, don't emit a new one
			var parameterTypes = parameterType == typeof(void) ? Type.EmptyTypes : new Type[] { parameterType };
			if (interfaceMethod.DeclaringType.GetMethod(methodName, parameterTypes) != null)
				return null;

			var targetParameterType = GetTypeSupportedByEventSource(parameterType);
			var targetParameters = parameterTypes.Select(t => TypeIsSupportedByEventSource(t) ? t : typeof(string)).ToList();
			bool supportsContext = SupportsContext(invocationContext);
			if (supportsContext)
				targetParameters.Add(typeof(string));
			var targetParameterTypes = targetParameters.ToArray();

			// determine if this is a non-event or an event
			// if an event, but there is no event attribute, just add one to the last event id
			EventAttribute startEventAttribute = interfaceMethod.GetCustomAttribute<EventAttribute>() ?? new EventAttribute(eventId);
			EventAttribute eventAttribute = _eventAttributeProvider.CopyEventAttribute(startEventAttribute, invocationContext, eventId);
			eventId = Math.Max(eventId, eventAttribute.EventId + 1);
			if (eventAttribute.Keywords == EventKeywords.None)
				eventAttribute.Keywords = autoKeyword;

			// if we have a return type, then we need to implement two methods
			if (parameterTypes.Length == 1)
			{
				// emit the internal method
				MethodBuilder m = _typeBuilder.DefineMethod(methodName, MethodAttributes.Public, typeof(void), targetParameterTypes);
				m.SetCustomAttribute(EventAttributeHelper.ConvertEventAttributeToAttributeBuilder(eventAttribute));
				EmitCallWriteEvent(invocationContext, m, eventAttribute, targetParameterTypes, targetParameterTypes);

				// emit an overloaded wrapper method that calls the method when it's enabled
				// note this is a non-event so EventSource doesn't try to log it
				MethodBuilder im = _typeBuilder.DefineMethod(methodName, MethodAttributes.Public);
				ProxyHelper.CopyGenericSignature(interfaceMethod, im);
				im.SetReturnType(parameterTypes[0]);
				im.SetParameters(parameterTypes);

				// mark the method as a non-event
				im.SetCustomAttribute(EventAttributeHelper.CreateNonEventAttribute());

				// put the return value on the stack so we can return the value as a passthrough
				im.GetILGenerator().Emit(OpCodes.Ldarg_1);

				if (EmitIsEnabled(im, eventAttribute))
				{
					EmitTaskCompletion(im, parameterType, faultedMethod);
					EmitDirectProxy(invocationContext, im, m, parameterTypes, targetParameterTypes);
				}

				return im;
			}
			else
			{
				// the method does not have a return value
				// so create the internal method that calls WriteEvent
				MethodBuilder m = _typeBuilder.DefineMethod(methodName, MethodAttributes.Public, typeof(void), targetParameterTypes);
				m.SetCustomAttribute(EventAttributeHelper.ConvertEventAttributeToAttributeBuilder(eventAttribute));
				ProxyHelper.EmitDefaultValue(m.GetILGenerator(), m.ReturnType);
				if (EmitIsEnabled(m, eventAttribute))
					EmitCallWriteEvent(invocationContext, m, eventAttribute, targetParameterTypes, targetParameterTypes);

				return m;
			}
		}

		/// <summary>
		/// Emit the code required to defer the logging of a task until completion.
		/// </summary>
		/// <param name="methodBuilder">The method to append to.</param>
		/// <param name="parameterType">The type of parameter being passed.</param>
		/// <param name="faultedMethod">A faulted method to call if the task is faulted. null if there is no handler.</param>
		private static void EmitTaskCompletion(MethodBuilder methodBuilder, Type parameterType, MethodBuilder faultedMethod)
		{
			// this only applies to tasks
			if (parameterType != typeof(Task) && !parameterType.IsSubclassOf(typeof(Task)))
				return;

			/* we have a task, so we want to implement:
			 *	if (!task.IsCompleted)
			 *	{
			 *		return task.ContinueWith(t => Foo_Completed(t), TaskContinuationOptions.ExecuteSynchronously);
			 *	}
			 *	else if (task.IsFaulted)
			 *	{
			 *		this.Foo_Faulted(t.Exception);
			 *		return task;
			 *	}
			 *	else
			 *		...whatever gets emitted next
			 */
			var mIL = methodBuilder.GetILGenerator();

			// if (task.IsCompleted) skip this whole thing
			var isCompleted = mIL.DefineLabel();
			mIL.Emit(OpCodes.Ldarg_1);
			mIL.Emit(OpCodes.Call, typeof(Task).GetProperty("IsCompleted").GetGetMethod());
			mIL.Emit(OpCodes.Brtrue, isCompleted);

			// it's not completed, so
			//		task.ContinueWith(t => Foo_Completed(t), TaskContinuationOptions.ExecuteSynchronously)

			// first clear the return value off the stack
			mIL.Emit(OpCodes.Pop);

			var actionType = typeof(Action<>).MakeGenericType(parameterType);
			mIL.Emit(OpCodes.Ldarg_1);
			mIL.Emit(OpCodes.Ldarg_0);
			var callbackMethod = methodBuilder.IsGenericMethod ? methodBuilder.MakeGenericMethod(parameterType.GetGenericArguments()) : methodBuilder;
			mIL.Emit(OpCodes.Ldftn, callbackMethod);
			mIL.Emit(OpCodes.Newobj, actionType.GetConstructor(new Type[] { typeof(object), typeof(IntPtr) }));
			mIL.Emit(OpCodes.Ldc_I4, (int)TaskContinuationOptions.ExecuteSynchronously);
			var continuation = parameterType.GetMethod("ContinueWith", new Type[] { actionType, typeof(TaskContinuationOptions) });
			mIL.Emit(OpCodes.Call, continuation);

			// the new return value is this continuation
			// return it
			mIL.Emit(OpCodes.Ret);

			mIL.MarkLabel(isCompleted);

			// time to check if it is faulted
			if (faultedMethod != null)
			{
				/*
				 * if (task.IsFaulted)
				 * {
				 *		Foo_Faulted(task.Exception);
				 *		return task;
				 * }
				 */
				var isNotFaulted = mIL.DefineLabel();
				mIL.Emit(OpCodes.Ldarg_1);
				mIL.Emit(OpCodes.Call, typeof(Task).GetProperty("IsFaulted").GetGetMethod());
				mIL.Emit(OpCodes.Brfalse, isNotFaulted);

				// call _Faulted to record the exception
				mIL.Emit(OpCodes.Ldarg_0); // this
				mIL.Emit(OpCodes.Ldarg_1); // task.Exception
				mIL.Emit(OpCodes.Call, typeof(Task).GetProperty("Exception").GetGetMethod());
				mIL.Emit(OpCodes.Call, faultedMethod);
				if (faultedMethod.ReturnType != typeof(void))
					mIL.Emit(OpCodes.Pop);

				// return the task
				mIL.Emit(OpCodes.Ret);

				mIL.MarkLabel(isNotFaulted);
			}
		}

		/// <summary>
		/// Emits the code to determine whether logging is enabled.
		/// For NonEvents, this emits the default return value to the method, and returns false here.
		/// </summary>
		/// <param name="methodBuilder">The MethodBuilder to implement.</param>
		/// <param name="eventAttribute">The EventAttribute.</param>
		/// <returns>True if events could possibly be enabled, false if this method is a NonEvent.</returns>
		private static bool EmitIsEnabled(MethodBuilder methodBuilder, EventAttribute eventAttribute)
		{
			/*
			 * This method assumes that a default return value is already on the stack
			 *
			 * if a nonevent:
			 *
			 *		return (top of stack)
			 *
			 * if an event:
			 *
			 *		if (!IsEnabled(level, keywords)
			 *		{
			 *			return (top of stack)
			 *		}
			 *		else
			 *			...whatever gets emitted next
			 */

			ILGenerator mIL = methodBuilder.GetILGenerator();

			// if there is no event attribute, then this is a non-event, so just return silently
			if (eventAttribute == null)
			{
				mIL.Emit(OpCodes.Ret);
				return false;
			}

			// call IsEnabled with the given event level and keywords to check whether we should log
			mIL.Emit(OpCodes.Ldarg_0);
			mIL.Emit(OpCodes.Ldc_I4, (int)eventAttribute.Level);
			mIL.Emit(OpCodes.Ldc_I8, (long)eventAttribute.Keywords);
			mIL.Emit(OpCodes.Call, _isEnabled);
			Label enabledLabel = mIL.DefineLabel();
			mIL.Emit(OpCodes.Brtrue, enabledLabel);
			mIL.Emit(OpCodes.Ret);
			mIL.MarkLabel(enabledLabel);

			return true;
		}

		/// <summary>
		/// Emit a call to WriteEvent(param object[]).
		/// </summary>
		/// <param name="invocationContext">The InvocationContext for this call.</param>
		/// <param name="methodBuilder">The MethodBuilder to append to.</param>
		/// <param name="eventAttribute">The EventAttribute to use as values in the method.</param>
		/// <param name="sourceParameterTypes">The types of parameters on the source method.</param>
		/// <param name="targetParameterTypes">The types of the parameters on the target method.</param>
		private void EmitCallWriteEvent(InvocationContext invocationContext, MethodBuilder methodBuilder, EventAttribute eventAttribute, Type[] sourceParameterTypes, Type[] targetParameterTypes)
		{
			ILGenerator mIL = methodBuilder.GetILGenerator();

			// if there is no event attribute, then this is a non-event, so just return silently
			if (eventAttribute == null)
			{
				ProxyHelper.EmitDefaultValue(mIL, methodBuilder.ReturnType);
				mIL.Emit(OpCodes.Ret);
				return;
			}

			// call write event with the parameters in an object array
			mIL.Emit(OpCodes.Ldarg_0);
			mIL.Emit(OpCodes.Ldc_I4, eventAttribute.EventId);

			// create a new array of the proper length to pass the values in
			mIL.Emit(OpCodes.Ldc_I4, targetParameterTypes.Length);
			mIL.Emit(OpCodes.Newarr, typeof(object));
			for (int i = 0; i < sourceParameterTypes.Length; i++)
			{
				mIL.Emit(OpCodes.Dup);
				mIL.Emit(OpCodes.Ldc_I4, (int)i);

				// load the argument and box it
				mIL.Emit(OpCodes.Ldarg, (int)i + 1);

				// if the target is a value type, then we can box the source type
				// and the CLR will apply conversions for us
				// at this point, all invalid types have been converted to strings in EmitDirectProxy
				// and references have been dereferenced
				if (targetParameterTypes[i].IsValueType)
				{
					var sourceType = sourceParameterTypes[i];
					if (sourceType.IsByRef)
						sourceType = sourceType.GetElementType();
					mIL.Emit(OpCodes.Box, sourceType);
				}

				mIL.Emit(OpCodes.Stelem, typeof(object));
			}

			// if there is a context, call the context provider and add the context parameter
			if (SupportsContext(invocationContext))
			{
				// load the array and index onto the stack
				mIL.Emit(OpCodes.Dup);
				mIL.Emit(OpCodes.Ldc_I4, (int)targetParameterTypes.Length - 1);

				// load the context provider
				mIL.Emit(OpCodes.Ldsfld, _contextProviderField);

				// get the invocation context from the array on the provider
				mIL.Emit(OpCodes.Ldsfld, _invocationContextsField);
				mIL.Emit(OpCodes.Ldc_I4, _invocationContexts.Count);
				mIL.Emit(OpCodes.Ldelem, typeof(InvocationContext));
				mIL.Emit(OpCodes.Callvirt, typeof(TraceContextProvider).GetMethod("ProvideContext"));
				_invocationContexts.Add(invocationContext);

				// put the result into the array
				mIL.Emit(OpCodes.Stelem, typeof(object));
			}

			// prepare for write event by setting the ETW activity ID
			mIL.Emit(OpCodes.Call, typeof(EventActivityScope).GetMethod("PrepareForWriteEvent"));

			// call writeevent
			mIL.Emit(OpCodes.Call, _writeEvent);
			mIL.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Emits a proxy to a method that just calls the base method.
		/// </summary>
		/// <param name="invocationContext">The current invocation context.</param>
		/// <param name="methodBuilder">The method to implement.</param>
		/// <param name="baseMethod">The base method.</param>
		/// <param name="sourceParameterTypes">The types of the parameters on the source method.</param>
		/// <param name="targetParameterTypes">The types of the parameters on the target method.</param>
		private void EmitDirectProxy(InvocationContext invocationContext, MethodBuilder methodBuilder, MethodInfo baseMethod, Type[] sourceParameterTypes, Type[] targetParameterTypes)
		{
			/*
			 * This method assume that a default return value has been pushed on the stack.
			 *
			 *		base(params);
			 *		return (top of stack);
			 */

			ILGenerator mIL = methodBuilder.GetILGenerator();

			// copy the parameters to the stack
			// arg.0 = this
			// so we go to length+1
			mIL.Emit(OpCodes.Ldarg_0);
			for (int i = 0; i < sourceParameterTypes.Length; i++)
			{
				ProxyHelper.EmitSerializeValue(
					methodBuilder,
					invocationContext,
					_invocationContexts,
					_invocationContextsField,
					i,
					sourceParameterTypes[i],
					targetParameterTypes[i],
					_serializationProvider,
					_serializationProviderField);
			}

			// if this method supports context, then add a context parameter
			// note that we pass null in here and then build the context from within EmitCallWriteEvent
			if (SupportsContext(invocationContext))
				mIL.Emit(OpCodes.Ldnull);

			// now that all of the parameters have been loaded, call the base method
			mIL.Emit(OpCodes.Call, baseMethod);

			mIL.Emit(OpCodes.Ret);
		}
		#endregion

		#region Nested Enum Type Methods
		/// <summary>
		/// Emits an implementation of an enum class.
		/// </summary>
		/// <param name="enumSourceType">The source enum class to copy.</param>
		/// <param name="className">The name of the class.</param>
		/// <param name="codeType">The type of code to extract.</param>
		private void EmitEnumImplementation(Type enumSourceType, string className, Type codeType)
		{
			if (enumSourceType == null)
				enumSourceType = FindNestedType(_interfaceType, className);
			if (enumSourceType == null)
				return;

			// create a new nested type
			TypeBuilder nt = _typeBuilder.DefineNestedType(className, TypeAttributes.NestedPublic | TypeAttributes.Class);

			// go through the type containing the codes and copy them to our new type
			foreach (FieldInfo info in enumSourceType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
			{
				if (info.FieldType == codeType)
				{
					FieldBuilder fb = nt.DefineField(info.Name, info.FieldType, info.Attributes);
					fb.SetConstant(info.GetRawConstantValue());
				}
			}

			// force the type to be created
			nt.CreateType();
		}

		/// <summary>
		/// Find a nested type defined in this class or a base class.
		/// </summary>
		/// <param name="searchType">The class to search.</param>
		/// <param name="className">The name of the class to find.</param>
		/// <returns>The given nested type or null if not found.</returns>
		private static Type FindNestedType(Type searchType, string className)
		{
			Type nestedType = null;

			// if an enumSource type has not been defined, look up the original class hierarchy until we find it
			for (; nestedType == null && searchType != null; searchType = searchType.BaseType)
				nestedType = searchType.GetNestedType(className);

			return nestedType;
		}

		/// <summary>
		/// When the Keywords enum is not defined, emit the implementation of the Keywords enum, automatically generated from the interface.
		/// </summary>
		/// <param name="autoKeywords">The list of logging methods to use to generate the enum.</param>
		private void EmitKeywordImpl(Dictionary<string, ulong> autoKeywords)
		{
			// create a new nested type
			TypeBuilder nt = _typeBuilder.DefineNestedType(Keywords, TypeAttributes.NestedPublic | TypeAttributes.Class);

			// go through the type containing the codes and copy them to our new type
			foreach (var pair in autoKeywords)
			{
				FieldBuilder fb = nt.DefineField(pair.Key, typeof(EventKeywords), FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault);
				fb.SetConstant((EventKeywords)pair.Value);
			}

			// force the type to be created
			nt.CreateType();
		}
		#endregion

		#region Helper Methods
		/// <summary>
		/// Determines whether the given invocation supports a context provider.
		/// </summary>
		/// <param name="invocationContext">The current InvocationContext.</param>
		/// <returns>True if the context provider should be invoked for the context.</returns>
		private bool SupportsContext(InvocationContext invocationContext)
		{
			return _contextProvider != null && _contextProvider.ShouldProvideContext(invocationContext);
		}
		#endregion
	}
}
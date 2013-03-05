using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Constructs a proxy out of a logger and an object to give you automatic logging of an interface.
	/// </summary>
	public static class TracingProxy
	{
		#region Private Members
		/// <summary>
		/// The name of the field to store the log instance.
		/// </summary>
		private const string LogFieldName = "_log";

		/// <summary>
		/// The name of the field to store the execute instance.
		/// </summary>
		private const string ExecuteFieldName = "_execute";

		/// <summary>
		/// The name of the field to store the serializer instance.
		/// </summary>
		private const string SerializerFieldName = "_serializer";

		/// <summary>
		/// A cache of the constructors for the proxies.
		/// </summary>
		private static ConcurrentDictionary<Tuple<Type, Type>, Func<object, object, object, object>> _constructors =
			new ConcurrentDictionary<Tuple<Type, Type>, Func<object, object, object, object>>();

		/// <summary>
		/// The types of parameters passed to the tracing proxy constructor.
		/// </summary>
		private static Type[] _constructorParameterTypes = new Type[] { typeof(object), typeof(object), typeof(object) };
		#endregion

		#region Public Members
		/// <summary>
		/// Creates a tracing proxy around the T interface or class of the given object.
		/// Events will log to the EventSource defined for type T.
		/// The proxy will trace any virtual or interface methods of type T.
		/// </summary>
		/// <typeparam name="T">The interface or class to proxy and log.</typeparam>
		/// <param name="instance">The instance of the object to log.</param>
		/// <returns>A proxy object of type T that traces calls.</returns>
		public static T Create<T>(object instance)
			where T : class
		{
			var logger = EventSourceImplementer.GetEventSource<T>();

			return (T)CreateInternal(instance, typeof(T), logger, logger.GetType());
		}

		/// <summary>
		/// Creates a tracing proxy around the T interface or class of the given object
		/// and attempts to log to an alternate EventSource defined by TEventSource.
		/// Events will log to the EventSource defined for type TEventSource.
		/// The proxy will trace any methods that match the signatures of methods on TEventSource.
		/// </summary>
		/// <typeparam name="T">The interface or class to proxy and log.</typeparam>
		/// <typeparam name="TEventSource">The matching interface to log to.</typeparam>
		/// <param name="instance">The instance of the object to log.</param>
		/// <returns>A proxy object of type T that traces calls.</returns>
		public static T Create<T, TEventSource>(T instance)
			where T : class
			where TEventSource : class
		{
			var logger = EventSourceImplementer.GetEventSourceAs<TEventSource>();
			return (T)CreateInternal(instance, typeof(T), logger, logger.GetType());
		}

		/// <summary>
		/// Creates a tracing proxy around the T interface or class of the given object.
		/// Events will log to the EventSource defined for type T.
		/// The proxy will trace any virtual or interface methods of type T.
		/// </summary>
		/// <param name="instance">The instance of the object to log.</param>
		/// <param name="interfaceType">The type of interface to log on.</param>
		/// <returns>A proxy object of type interfaceType that traces calls.</returns>
		public static object Create(object instance, Type interfaceType)
		{
			var logger = EventSourceImplementer.GetEventSource(interfaceType);

			return CreateInternal(instance, interfaceType, logger, logger.GetType());
		}
		#endregion

		#region Implementation
		/// <summary>
		/// Creates a proxy out of a logger and an object to give you automatic logging of an instance.
		/// The logger and object must implement the same interface.
		/// </summary>
		/// <typeparam name="T">The interface that is shared.</typeparam>
		/// <param name="execute">The instance of the object that executes the interface.</param>
		/// <param name="executeType">The type of the execute object.</param>
		/// <param name="log">The instance of the logging interface.</param>
		/// <param name="logType">The type on the log object that should be mapped to the execute object.</param>
		/// <returns>A proxy object of type T that logs to the log object and executes on the execute object.</returns>
		private static object CreateInternal(object execute, Type executeType, object log, Type logType)
		{
			// cache constructors based on tuple of types, including logoverride
			var tuple = Tuple.Create(executeType, logType);

			// get the serialization provider
			var serializer = ProviderManager.GetProvider<ITraceSerializationProvider>(logType) ?? new JsonObjectSerializer();

			// get the constructor
			var creator = _constructors.GetOrAdd(
				tuple,
				t => (Func<object, object, object, object>)ImplementProxy(t.Item1, t.Item2, serializer).CreateDelegate(typeof(Func<object, object, object, object>)));

			return creator(execute, log, serializer);
		}

		/// <summary>
		/// Implements a logging proxy around a given interface type.
		/// </summary>
		/// <param name="executeType">The type of the interface to proxy.</param>
		/// <param name="logType">The type of the log interface to proxy.</param>
		/// <param name="serializationProvider">The serialization provider to use.</param>
		/// <returns>A static method that can be used to construct the proxy.</returns>
		private static MethodInfo ImplementProxy(Type executeType, Type logType, ITraceSerializationProvider serializationProvider)
		{
			// create a new assembly
			AssemblyName an = Assembly.GetExecutingAssembly().GetName();
			an.Name = TypeImplementer.AssemblyName;
			AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
			ModuleBuilder mb = ab.DefineDynamicModule(an.Name);

			// create a type that implements the given interface
			TypeBuilder tb;
			if (executeType.IsInterface)
				tb = mb.DefineType(executeType.FullName + "_LoggingProxy", TypeAttributes.Class | TypeAttributes.Public, typeof(Object), new Type[] { executeType });
			else
				tb = mb.DefineType(executeType.FullName + "_LoggingProxy", TypeAttributes.Class | TypeAttributes.Public, executeType);

			// define the fields
			var logField = tb.DefineField(LogFieldName, logType, FieldAttributes.InitOnly | FieldAttributes.Private);
			var executeField = tb.DefineField(ExecuteFieldName, executeType, FieldAttributes.InitOnly | FieldAttributes.Private);
			var serializerField = tb.DefineField(SerializerFieldName, typeof(ITraceSerializationProvider), FieldAttributes.InitOnly | FieldAttributes.Private);

			// create a constructor for the type
			// we just store the log and execute interfaces in the fields
			/*
			 * public T (I log, I execute, ITraceSerializationProvoder serializer)
			 * {
			 *		_log = log;
			 *		_execute = execute;
			 *		_serializer = serializer
			 * }
			 */
			var constructorParameterTypes = new Type[] { executeType, logType, typeof(ITraceSerializationProvider) };
			ConstructorBuilder ctor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, constructorParameterTypes);
			ILGenerator ctorIL = ctor.GetILGenerator();
			ctorIL.Emit(OpCodes.Ldarg_0);
			ctorIL.Emit(OpCodes.Ldarg_1);
			ctorIL.Emit(OpCodes.Stfld, executeField);
			ctorIL.Emit(OpCodes.Ldarg_0);
			ctorIL.Emit(OpCodes.Ldarg_2);
			ctorIL.Emit(OpCodes.Castclass, logType);
			ctorIL.Emit(OpCodes.Stfld, logField);
			ctorIL.Emit(OpCodes.Ldarg_0);
			ctorIL.Emit(OpCodes.Ldarg_3);
			ctorIL.Emit(OpCodes.Stfld, serializerField);
			ctorIL.Emit(OpCodes.Ret);

			// create a method that invokes the constructor so we can return a fast delegate
			var createMethod = EmitCreateImpl(tb, ctor);

			// for each method on the interface, try to implement it with a call to eventsource
			var interfaceMethods = executeType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly);
			foreach (MethodInfo interfaceMethod in interfaceMethods.Where(m => !m.IsFinal))
				EmitMethodImpl(tb, executeField, interfaceMethod, logField, serializerField, serializationProvider);

			// create a class
			Type t = tb.CreateType();

			// return the create method
			return t.GetMethod(createMethod.Name, BindingFlags.Static | BindingFlags.Public, null, constructorParameterTypes, null);
		}

		/// <summary>
		/// Emits a method to construct a new proxy.
		/// </summary>
		/// <param name="tb">The TypeBuilder to append to.</param>
		/// <param name="constructorInfo">The constructor to call.</param>
		/// <returns>A static method to construct the proxy.</returns>
		private static MethodBuilder EmitCreateImpl(TypeBuilder tb, ConstructorInfo constructorInfo)
		{
			/*
			 * public static T Create (I log, I execute)
			 * {
			 *		return new T (log, execute);
			 * }
			 */
			MethodBuilder mb = tb.DefineMethod("Create", MethodAttributes.Static | MethodAttributes.Public, typeof(object), _constructorParameterTypes);
			ILGenerator mIL = mb.GetILGenerator();
			for (int i = 0; i < _constructorParameterTypes.Length; i++)
				mIL.Emit(OpCodes.Ldarg, (int)i);
			mIL.Emit(OpCodes.Newobj, constructorInfo);
			mIL.Emit(OpCodes.Ret);

			return mb;
		}

		/// <summary>
		/// Emits the implementation of a given interface method.
		/// </summary>
		/// <param name="tb">The TypeBuilder to append to.</param>
		/// <param name="executeField">The field containing the execute interface.</param>
		/// <param name="executeMethod">The execute method to implement.</param>
		/// <param name="logField">The field containing the logging interface.</param>
		/// <param name="serializerField">The field containing the serialization provider for the log type.</param>
		/// <param name="serializationProvider">The serialization provider to use.</param>
		private static void EmitMethodImpl(TypeBuilder tb, FieldInfo executeField, MethodInfo executeMethod, FieldInfo logField, FieldBuilder serializerField, ITraceSerializationProvider serializationProvider)
		{
			/*
			 * public TReturn Method (params)
			 * {
			 *		// make sure that there is an Activity ID wrapped around the calls
			 *		using (var EventActivityScope = new EventActivityScope(true))
			 *		{
			 *			_log.Method(params);
			 *			object value = _execute.Method(params);
			 *			_log.Method_Completed(value);
			 *		}
			 * }
			 */

			// look at the parameters on the interface
			var parameters = executeMethod.GetParameters();
			var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

			MethodBuilder m = tb.DefineMethod(executeMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual, executeMethod.ReturnType, parameterTypes);
			ILGenerator mIL = m.GetILGenerator();

			// set up the locals
			mIL.DeclareLocal(typeof(EventActivityScope));
			if (executeMethod.ReturnType != typeof(void)) // loc.1 - local variable for storing the results
				mIL.DeclareLocal(executeMethod.ReturnType);

			// set up the activity scope into loc.0
			mIL.Emit(OpCodes.Ldc_I4_1);
			mIL.Emit(OpCodes.Newobj, typeof(EventSourceProxy.EventActivityScope).GetConstructor(new Type[] { typeof(bool) }));
			mIL.Emit(OpCodes.Stloc_0);

			// start the try block
			Label endOfMethod = mIL.DefineLabel();
			mIL.BeginExceptionBlock();

			// call the method on the log that matches the execute method
			var logMethod = DiscoverMethod(logField.FieldType, executeMethod.Name, parameterTypes);
			if (logMethod != null)
			{
				// call the log method and throw away the result if there is one
				EmitBaseMethodCall(mIL, logField, logMethod);
				if (logMethod.ReturnType != typeof(void))
					mIL.Emit(OpCodes.Pop);
			}

			// call execute
			EmitBaseMethodCall(mIL, executeField, executeMethod);
			if (executeMethod.ReturnType != typeof(void))
				mIL.Emit(OpCodes.Stloc_1);

			var completedParameterTypes = new Type[] { TypeImplementer.TypeIsSupportedByEventSource(executeMethod.ReturnType) ? executeMethod.ReturnType : typeof(string) };

			// if there is a completed method, then call that
			var completedMethod = DiscoverMethod(logField.FieldType, executeMethod.Name + "_Completed", completedParameterTypes) ??
				DiscoverMethod(logField.FieldType, executeMethod.Name + "_Completed", Type.EmptyTypes);
			if (completedMethod != null)
			{
				// load this._log
				mIL.Emit(OpCodes.Ldarg_0);
				mIL.Emit(OpCodes.Ldfld, logField);

				// load the value from the local variable
				var completedParameters = completedMethod.GetParameters();
				if (completedParameters.Length == 1)
				{
					if (executeMethod.ReturnType == completedParameters[0].ParameterType)
					{
						mIL.Emit(OpCodes.Ldloc_1);
					}
					else
					{
						// if the types don't match, then serialize the object
						// non-fundamental types use the object serializer
						if (serializationProvider.ShouldSerialize(completedMethod, 0))
						{
							// get the object serializer from the this pointer
							mIL.Emit(OpCodes.Ldarg_0);
							mIL.Emit(OpCodes.Ldfld, serializerField);

							// load the value
							mIL.Emit(OpCodes.Ldloc_1);
							if (executeMethod.ReturnType.IsValueType)
								mIL.Emit(OpCodes.Box, executeMethod.ReturnType);

							// add the method builder and parameter index
							mIL.Emit(OpCodes.Ldtoken, completedMethod);
							mIL.Emit(OpCodes.Ldc_I4_0);

							mIL.Emit(OpCodes.Callvirt, typeof(ITraceSerializationProvider).GetMethod("SerializeObject", BindingFlags.Instance | BindingFlags.Public));
						}
						else
							mIL.Emit(OpCodes.Ldnull);
					}
				}

				mIL.Emit(OpCodes.Call, completedMethod);
				if (completedMethod.ReturnType != typeof(void))
					mIL.Emit(OpCodes.Pop);
			}

			// clean up the activity scope
			mIL.BeginFinallyBlock();
			mIL.Emit(OpCodes.Ldloc_0);
			mIL.Emit(OpCodes.Call, typeof(EventActivityScope).GetMethod("Dispose"));
			mIL.EndExceptionBlock();

			// return the result
			mIL.MarkLabel(endOfMethod);
			if (executeMethod.ReturnType != typeof(void))
				mIL.Emit(OpCodes.Ldloc_1);
			mIL.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Discover the given method on the type or a base interface.
		/// </summary>
		/// <param name="type">The type to analyze.</param>
		/// <param name="methodName">The name of the method to look up.</param>
		/// <param name="parameterTypes">The types of parameters.</param>
		/// <returns>The method information or null.</returns>
		private static MethodInfo DiscoverMethod(Type type, string methodName, Type[] parameterTypes)
		{
			var methods = type.GetMethods();
			var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public, null, parameterTypes, null) ??
				type.GetMethod("_" + methodName, BindingFlags.Instance | BindingFlags.Public, null, parameterTypes, null);
			if (method == null && type.IsInterface)
			{
				foreach (Type baseInterface in type.GetInterfaces())
				{
					method = DiscoverMethod(baseInterface, methodName, parameterTypes);
					if (method != null)
						return method;
				}
			}

			return method;
		}

		/// <summary>
		/// Emits a call to the base method by pushing all of the arguments.
		/// </summary>
		/// <param name="mIL">The ILGenerator to append to.</param>
		/// <param name="field">The field containing the interface to call.</param>
		/// <param name="baseMethod">The method to implement.</param>
		private static void EmitBaseMethodCall(ILGenerator mIL, FieldInfo field, MethodInfo baseMethod)
		{
			// load the pointer from the field, push the parameters and call the method
			// this is an instance method, so arg.0 is the this pointer
			mIL.Emit(OpCodes.Ldarg_0);
			mIL.Emit(OpCodes.Ldfld, field);
			for (int i = 1; i <= baseMethod.GetParameters().Length; i++)
				mIL.Emit(OpCodes.Ldarg, (int)i);
			mIL.Emit(OpCodes.Call, baseMethod);
		}
		#endregion
	}
}
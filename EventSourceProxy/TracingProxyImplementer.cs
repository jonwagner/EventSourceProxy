using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Generates a class that implements a TracingProxy.
	/// </summary>
	class TracingProxyImplementer
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
		/// The types of parameters passed to the tracing proxy constructor.
		/// </summary>
		private static Type[] _createParameterTypes = new Type[] { typeof(object) };

		/// <summary>
		/// The type of object to execute.
		/// </summary>
		private Type _executeType;

		/// <summary>
		/// The type of object to log.
		/// </summary>
		private Type _logType;

		/// <summary>
		/// True when the proxy should create a new activity scope around its method calls.
		/// </summary>
		private bool _callWithActivityScope;

		/// <summary>
		/// The serialization provider for the type.
		/// </summary>
		private TraceSerializationProvider _serializationProvider;

		/// <summary>
		/// The type builder for the type.
		/// </summary>
		private TypeBuilder _typeBuilder;

		/// <summary>
		/// The field containing the object to execute.
		/// </summary>
		private FieldBuilder _executeField;

		/// <summary>
		/// The static field containing the logger.
		/// </summary>
		private FieldBuilder _logField;

		/// <summary>
		/// The static field containing the serialization provider.
		/// </summary>
		private FieldBuilder _serializerField;

		/// <summary>
		/// The list of invocation contexts during code generation.
		/// </summary>
		private List<InvocationContext> _invocationContexts = new List<InvocationContext>();

		/// <summary>
		/// The static field holding the invocation contexts at runtime.
		/// </summary>
		private FieldBuilder _invocationContextsField;
		#endregion

		#region Constructors
		/// <summary>
		/// Initializes a new instance of the TracingProxyImplementer class.
		/// </summary>
		/// <param name="executeType">The type of object to execute.</param>
		/// <param name="logType">The type of object to log.</param>
		/// <param name="callWithActivityScope">True to generate a proxy that wraps an activity scope around all calls.</param>
		public TracingProxyImplementer(Type executeType, Type logType, bool callWithActivityScope)
		{
			_executeType = executeType;
			_callWithActivityScope = callWithActivityScope;
			_serializationProvider = TraceSerializationProvider.GetSerializationProvider(logType);

			// create a log of the given log type
			var log = EventSourceImplementer.GetEventSource(logType);
			_logType = log.GetType();

			CreateMethod = ImplementProxy(log);
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets a method that can be used to create the proxy.
		/// </summary>
		public MethodInfo CreateMethod { get; private set; }
		#endregion

		#region Implementation
		/// <summary>
		/// Implements a logging proxy around a given interface type.
		/// </summary>
		/// <param name="log">The log to use for the proxy.</param>
		/// <returns>A static method that can be used to construct the proxy.</returns>
		private MethodInfo ImplementProxy(EventSource log)
		{
			// create a new assembly
			AssemblyName an = Assembly.GetExecutingAssembly().GetName();
			an.Name = ProxyHelper.AssemblyName;
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
			ModuleBuilder mb = ab.DefineDynamicModule(an.Name);

			// create a type that implements the given interface
			if (_executeType.IsInterface)
				_typeBuilder = mb.DefineType(_executeType.FullName + "_LoggingProxy", TypeAttributes.Class | TypeAttributes.Public, typeof(Object), new Type[] { _executeType });
			else
				_typeBuilder = mb.DefineType(_executeType.FullName + "_LoggingProxy", TypeAttributes.Class | TypeAttributes.Public, _executeType);

			// emit a constructor and a create method that invokes the constructor so we can return a fast delegate
			var ctor = EmitFieldsAndConstructor();
			var createMethod = EmitCreateImpl(ctor);

			// for each method on the interface, try to implement it with a call to eventsource
			var interfaceMethods = ProxyHelper.DiscoverMethods(_executeType);
			foreach (MethodInfo interfaceMethod in interfaceMethods.Where(m => !m.IsFinal))
				EmitMethodImpl(interfaceMethod);

			// create the class
			Type t = _typeBuilder.CreateTypeInfo().AsType();


			// initialize the logger and serializer fields to the static logger and serializer
			// so we never need to create them again
			t.GetField(_logField.Name, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, log);
			t.GetField(_serializerField.Name, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, _serializationProvider);
			t.GetField(_invocationContextsField.Name, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, _invocationContexts.ToArray());

			// fill in the event source for all of the invocation contexts
			foreach (var context in _invocationContexts)
				context.EventSource = log;

			// return the create method
			return t.GetMethod(createMethod.Name, BindingFlags.Static | BindingFlags.Public, null, _createParameterTypes, null);
		}
		
		/// <summary>
		/// Emits the proxy fields and a constructor.
		/// </summary>
		/// <returns>The constructor.</returns>
		private ConstructorInfo EmitFieldsAndConstructor()
		{
			// static fields
			_logField = _typeBuilder.DefineField(LogFieldName, _logType, FieldAttributes.Static | FieldAttributes.Private);
			_serializerField = _typeBuilder.DefineField(SerializerFieldName, typeof(TraceSerializationProvider), FieldAttributes.Static | FieldAttributes.Private);
			_invocationContextsField = _typeBuilder.DefineField("_invocationContexts", typeof(InvocationContext[]), FieldAttributes.Static | FieldAttributes.Private);

			// instance fields
			_executeField = _typeBuilder.DefineField(ExecuteFieldName, _executeType, FieldAttributes.InitOnly | FieldAttributes.Private);

			// create a constructor for the type
			/*
			 * public T (I execute)
			 * {
			 *		_execute = execute;
			 * }
			 */
			ConstructorBuilder ctor = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { _executeType });
			ILGenerator ctorIL = ctor.GetILGenerator();
			ctorIL.Emit(OpCodes.Ldarg_0);
			ctorIL.Emit(OpCodes.Ldarg_1);
			ctorIL.Emit(OpCodes.Stfld, _executeField);
			ctorIL.Emit(OpCodes.Ret);

			return ctor;
		}

		/// <summary>
		/// Emits a method to construct a new proxy.
		/// </summary>
		/// <param name="constructorInfo">The constructor to call.</param>
		/// <returns>A static method to construct the proxy.</returns>
		private MethodBuilder EmitCreateImpl(ConstructorInfo constructorInfo)
		{
			/*
			 * public static T Create (I execute)
			 * {
			 *		return new T (execute);
			 * }
			 */
			MethodBuilder mb = _typeBuilder.DefineMethod("Create", MethodAttributes.Static | MethodAttributes.Public, typeof(object), _createParameterTypes);
			ILGenerator mIL = mb.GetILGenerator();
			mIL.Emit(OpCodes.Ldarg_0);
			mIL.Emit(OpCodes.Newobj, constructorInfo);
			mIL.Emit(OpCodes.Ret);

			return mb;
		}

		/// <summary>
		/// Emits the implementation of a given interface method.
		/// </summary>
		/// <param name="executeMethod">The execute method to implement.</param>
		private void EmitMethodImpl(MethodInfo executeMethod)
		{
			/*
			 * public TReturn Method (params)
			 * {
			 *		var scope = new EventActivityScope(true);
			 *		try
			 *		{
			 *			_log.Method(params);
			 *			object value = _execute.Method(params);
			 *			_log.Method_Completed(value);
			 *		}
			 *		catch (Exception e)
			 *		{
			 *			_log.Method_Faulted(e);
			 *			throw;
			 *		}
			 *		finally
			 *		{
			 *			scope.Dispose();
			 *		}
			 * }
			 */

			var invocationContext = new InvocationContext(executeMethod, InvocationContextTypes.MethodCall);

			// start building the interface
			MethodBuilder m = _typeBuilder.DefineMethod(executeMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual);
			ProxyHelper.CopyMethodSignature(executeMethod, m);
			var parameterTypes = executeMethod.GetParameters().Select(p => p.ParameterType).ToArray();

			ILGenerator mIL = m.GetILGenerator();

			// set up a place to hold the return value
			LocalBuilder returnValue = null;
			if (m.ReturnType != typeof(void))
				returnValue = mIL.DeclareLocal(m.ReturnType);

			// set up the activity scope
			LocalBuilder scope = null;
			if (_callWithActivityScope)
			{
				scope = mIL.DeclareLocal(typeof(EventActivityScope));
				mIL.Emit(OpCodes.Ldc_I4_1);
				mIL.Emit(OpCodes.Newobj, typeof(EventActivityScope).GetConstructor(new Type[] { typeof(bool) }));
				mIL.Emit(OpCodes.Stloc, scope);
			}

			// start the try block
			mIL.BeginExceptionBlock();

			// call the method on the log that matches the execute method
			var targetParameterTypes = parameterTypes.Select(p => p.IsGenericParameter ? TypeImplementer.GetTypeSupportedByEventSource(p) : p).ToArray();
			var logMethod = DiscoverMethod(_logField.FieldType, executeMethod.Name, String.Empty, targetParameterTypes);
			if (logMethod != null)
			{
				// call the log method and throw away the result if there is one
				EmitBaseMethodCall(m, invocationContext, _logField, executeMethod, logMethod);
				if (logMethod.ReturnType != typeof(void))
					mIL.Emit(OpCodes.Pop);
			}

			// call execute
			EmitBaseMethodCall(m, invocationContext, _executeField, executeMethod, executeMethod);
			if (executeMethod.ReturnType != typeof(void))
				mIL.Emit(OpCodes.Stloc, returnValue);

			// if there is a completed method, then call that
			var completedParameterTypes = (executeMethod.ReturnType == typeof(void)) ? Type.EmptyTypes : new Type[] { executeMethod.ReturnType };
			var completedMethod = DiscoverMethod(_logField.FieldType, executeMethod.Name, TypeImplementer.CompletedSuffix, completedParameterTypes);
			if (completedMethod != null)
			{
				// load this._log
				mIL.Emit(OpCodes.Ldarg_0);
				mIL.Emit(OpCodes.Ldfld, _logField);

				// load the value from the local variable
				if (executeMethod.ReturnType != typeof(void))
					mIL.Emit(OpCodes.Ldloc, returnValue);

				mIL.Emit(OpCodes.Call, completedMethod);
				if (completedMethod.ReturnType != typeof(void))
					mIL.Emit(OpCodes.Pop);
			}

			// handle exceptions by logging them and rethrowing
			mIL.BeginCatchBlock(typeof(Exception));
			var faultedMethod = DiscoverMethod(_logField.FieldType, executeMethod.Name, TypeImplementer.FaultedSuffix, new Type[] { typeof(Exception) });
			if (faultedMethod != null)
			{
				// save the exception
				var exception = mIL.DeclareLocal(typeof(Exception));
				mIL.Emit(OpCodes.Stloc, exception);

				// load this._log
				mIL.Emit(OpCodes.Ldarg_0);
				mIL.Emit(OpCodes.Ldfld, _logField);

				// load the exception
				mIL.Emit(OpCodes.Ldloc, exception);

				// call the fault handler
				mIL.Emit(OpCodes.Call, faultedMethod);
				if (faultedMethod.ReturnType != typeof(void))
					mIL.Emit(OpCodes.Pop);
			}

			mIL.Emit(OpCodes.Rethrow);

			// clean up the activity scope
			if (_callWithActivityScope)
			{
				mIL.BeginFinallyBlock();
				mIL.Emit(OpCodes.Ldloc, scope);
				mIL.Emit(OpCodes.Callvirt, typeof(EventActivityScope).GetMethod("Dispose"));
			}

			mIL.EndExceptionBlock();

			// return the result
			if (executeMethod.ReturnType != typeof(void))
				mIL.Emit(OpCodes.Ldloc, returnValue);
			mIL.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Emits a call to the base method by pushing all of the arguments.
		/// </summary>
		/// <param name="m">The method to append to.</param>
		/// <param name="invocationContext">The invocation context for this call.</param>
		/// <param name="field">The field containing the interface to call.</param>
		/// <param name="originalMethod">The the original method signature.</param>
		/// <param name="baseMethod">The method to call.</param>
		private void EmitBaseMethodCall(MethodBuilder m, InvocationContext invocationContext, FieldInfo field, MethodInfo originalMethod, MethodInfo baseMethod)
		{
			// if this is a generic method, we have to instantiate our type of method
			if (baseMethod.IsGenericMethodDefinition)
				baseMethod = baseMethod.MakeGenericMethod(baseMethod.GetGenericArguments());

			var sourceParameters = originalMethod.GetParameters();
			var targetParameters = baseMethod.GetParameters();

			// load the pointer from the field, push the parameters and call the method
			// this is an instance method, so arg.0 is the this pointer
			ILGenerator mIL = m.GetILGenerator();
			mIL.Emit(OpCodes.Ldarg_0);
			mIL.Emit(OpCodes.Ldfld, field);

			// go through and serialize the parameters
			for (int i = 0; i < targetParameters.Length; i++)
			{
				ProxyHelper.EmitSerializeValue(
					_typeBuilder,
					m,
					invocationContext,
					_invocationContexts,
					_invocationContextsField,
					i,
					sourceParameters[i].ParameterType,
					targetParameters[i].ParameterType,
					null,
					_serializationProvider,
					_serializerField);
			}

			// call the method
			mIL.Emit(OpCodes.Callvirt, baseMethod);
		}

		/// <summary>
		/// Discover the given method on the type or a base interface.
		/// </summary>
		/// <param name="type">The type to analyze.</param>
		/// <param name="methodName">The name of the method to look up.</param>
		/// <param name="suffix">A suffix on the method name to look up.</param>
		/// <param name="parameterTypes">The types of parameters.</param>
		/// <returns>The method information or null.</returns>
		private MethodInfo DiscoverMethod(Type type, string methodName, string suffix, Type[] parameterTypes)
		{
			// find all of the methods with a matching signature
			var methods = type.GetMethods().Where(m => ProxyHelper.ParametersMatch(m, parameterTypes));

			// match the one without an integer first, then one with an integer
			var method = methods.FirstOrDefault(m => Regex.IsMatch(m.Name, "_?" + Regex.Escape(methodName + suffix))) ??
						methods.FirstOrDefault(m => Regex.IsMatch(m.Name, "_?" + Regex.Escape(methodName) + "_\\d+" + Regex.Escape(suffix)));

			if (method == null && type.IsInterface)
			{
				foreach (Type baseInterface in type.GetInterfaces())
				{
					method = DiscoverMethod(baseInterface, methodName, suffix, parameterTypes);
					if (method != null)
						return method;
				}
			}

			return method;
		}
		#endregion
	}
}

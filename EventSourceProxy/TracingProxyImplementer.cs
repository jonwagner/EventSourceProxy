using System;
using System.Collections.Generic;
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
		private static Type[] _createParameterTypes = new Type[] { typeof(object), typeof(object), typeof(object) };

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
		private ITraceSerializationProvider _serializationProvider;

		/// <summary>
		/// The type builder for the type.
		/// </summary>
		private TypeBuilder _typeBuilder;

		/// <summary>
		/// The field containing the logger.
		/// </summary>
		private FieldBuilder _logField;

		/// <summary>
		/// The field containing the object to execute.
		/// </summary>
		private FieldBuilder _executeField;

		/// <summary>
		/// Thje field containing the serialization provider.
		/// </summary>
		private FieldBuilder _serializerField;
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
			_logType = logType;
			_callWithActivityScope = callWithActivityScope;
			_serializationProvider = ObjectSerializationProvider.GetSerializationProvider(logType);

			CreateMethod = ImplementProxy();
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
		/// <returns>A static method that can be used to construct the proxy.</returns>
		private MethodInfo ImplementProxy()
		{
			// create a new assembly
			AssemblyName an = Assembly.GetExecutingAssembly().GetName();
			an.Name = ProxyHelper.AssemblyName;
			AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
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

			// create a class
			Type t = _typeBuilder.CreateType();

			// return the create method
			return t.GetMethod(createMethod.Name, BindingFlags.Static | BindingFlags.Public, null, _createParameterTypes, null);
		}
		
		/// <summary>
		/// Emits the proxy fields and a constructor.
		/// </summary>
		/// <returns>The constructor.</returns>
		private ConstructorInfo EmitFieldsAndConstructor()
		{
			// define the fields
			_logField = _typeBuilder.DefineField(LogFieldName, _logType, FieldAttributes.InitOnly | FieldAttributes.Private);
			_executeField = _typeBuilder.DefineField(ExecuteFieldName, _executeType, FieldAttributes.InitOnly | FieldAttributes.Private);
			_serializerField = _typeBuilder.DefineField(SerializerFieldName, typeof(ITraceSerializationProvider), FieldAttributes.InitOnly | FieldAttributes.Private);

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
			var constructorParameterTypes = new Type[] { _executeType, _logType, typeof(ITraceSerializationProvider) };
			ConstructorBuilder ctor = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, constructorParameterTypes);
			ILGenerator ctorIL = ctor.GetILGenerator();
			ctorIL.Emit(OpCodes.Ldarg_0);
			ctorIL.Emit(OpCodes.Ldarg_1);
			ctorIL.Emit(OpCodes.Stfld, _executeField);
			ctorIL.Emit(OpCodes.Ldarg_0);
			ctorIL.Emit(OpCodes.Ldarg_2);
			ctorIL.Emit(OpCodes.Stfld, _logField);
			ctorIL.Emit(OpCodes.Ldarg_0);
			ctorIL.Emit(OpCodes.Ldarg_3);
			ctorIL.Emit(OpCodes.Stfld, _serializerField);
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
			 * public static T Create (I log, I execute)
			 * {
			 *		return new T (log, execute);
			 * }
			 */
			MethodBuilder mb = _typeBuilder.DefineMethod("Create", MethodAttributes.Static | MethodAttributes.Public, typeof(object), _createParameterTypes);
			ILGenerator mIL = mb.GetILGenerator();
			for (int i = 0; i < _createParameterTypes.Length; i++)
				mIL.Emit(OpCodes.Ldarg, (int)i);
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
			 *		// make sure that there is an Activity ID wrapped around the calls
			 *		using (var EventActivityScope = new EventActivityScope(true))
			 *		{
			 *			_log.Method(params);
			 *			object value = _execute.Method(params);
			 *			_log.Method_Completed(value);
			 *		}
			 * }
			 */

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
				mIL.Emit(OpCodes.Newobj, typeof(EventSourceProxy.EventActivityScope).GetConstructor(new Type[] { typeof(bool) }));
				mIL.Emit(OpCodes.Stloc, scope);

				// start the try block
				mIL.BeginExceptionBlock();
			}

			// call the method on the log that matches the execute method
			var targetParameterTypes = parameterTypes.Select(p => p.IsGenericParameter ? TypeImplementer.GetTypeSupportedByEventSource(p) : p).ToArray();
			var logMethod = DiscoverMethod(_logField.FieldType, executeMethod.Name, String.Empty, targetParameterTypes);
			if (logMethod != null)
			{
				// call the log method and throw away the result if there is one
				EmitBaseMethodCall(m, _logField, executeMethod, logMethod);
				if (logMethod.ReturnType != typeof(void))
					mIL.Emit(OpCodes.Pop);
			}

			// call execute
			EmitBaseMethodCall(m, _executeField, executeMethod, executeMethod);
			if (executeMethod.ReturnType != typeof(void))
				mIL.Emit(OpCodes.Stloc, returnValue);

			// if there is a completed method, then call that
			var completedParameterTypes = new Type[] { TypeImplementer.GetTypeSupportedByEventSource(executeMethod.ReturnType) };
			var completedMethod = DiscoverMethod(_logField.FieldType, executeMethod.Name, "_Completed", completedParameterTypes) ??
				DiscoverMethod(_logField.FieldType, executeMethod.Name, "_Completed", Type.EmptyTypes);
			if (completedMethod != null)
			{
				// load this._log
				mIL.Emit(OpCodes.Ldarg_0);
				mIL.Emit(OpCodes.Ldfld, _logField);

				// load the value from the local variable
				var completedParameters = completedMethod.GetParameters();
				if (completedParameters.Length == 1)
				{
					ProxyHelper.EmitSerializeValue(
						m,
						-1,
						executeMethod.ReturnType,
						completedParameters[0].ParameterType,
						_serializationProvider,
						_serializerField,
						il => il.Emit(OpCodes.Ldloc, returnValue),
						il => il.Emit(OpCodes.Ldloca_S, returnValue));
				}

				mIL.Emit(OpCodes.Call, completedMethod);
				if (completedMethod.ReturnType != typeof(void))
					mIL.Emit(OpCodes.Pop);
			}

			// clean up the activity scope
			if (_callWithActivityScope)
			{
				mIL.BeginFinallyBlock();
				mIL.Emit(OpCodes.Ldloc, scope);
				mIL.Emit(OpCodes.Callvirt, typeof(EventActivityScope).GetMethod("Dispose"));
				mIL.EndExceptionBlock();
			}

			// return the result
			if (executeMethod.ReturnType != typeof(void))
				mIL.Emit(OpCodes.Ldloc, returnValue);
			mIL.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Emits a call to the base method by pushing all of the arguments.
		/// </summary>
		/// <param name="m">The method to append to.</param>
		/// <param name="field">The field containing the interface to call.</param>
		/// <param name="originalMethod">The the original method signature.</param>
		/// <param name="baseMethod">The method to call.</param>
		private void EmitBaseMethodCall(MethodBuilder m, FieldInfo field, MethodInfo originalMethod, MethodInfo baseMethod)
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
					m,
					i,
					sourceParameters[i].ParameterType,
					targetParameters[i].ParameterType,
					_serializationProvider,
					_serializerField,
					il => il.Emit(OpCodes.Ldarg, (int)i + 1),
					il => il.Emit(OpCodes.Ldarga_S, (int)i + 1));
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
			var methods = type.GetMethods();

			var method = methods.FirstOrDefault(m => Regex.IsMatch(m.Name, "_?" + Regex.Escape(methodName + suffix)) && ProxyHelper.ParametersMatch(m, parameterTypes)) ??
						methods.FirstOrDefault(m => Regex.IsMatch(m.Name, "_?" + Regex.Escape(methodName) + "_\\d+" + Regex.Escape(suffix)) && ProxyHelper.ParametersMatch(m, parameterTypes));

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

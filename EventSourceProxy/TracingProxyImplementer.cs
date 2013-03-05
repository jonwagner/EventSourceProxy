using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
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

		/// <summary>
		/// Initializes a new instance of the TracingProxyImplementer class.
		/// </summary>
		/// <param name="executeType">The type of object to execute.</param>
		/// <param name="logType">The type of object to log.</param>
		public TracingProxyImplementer(Type executeType, Type logType)
		{
			_executeType = executeType;
			_logType = logType;
			_serializationProvider = ObjectSerializationProvider.GetSerializationProvider(logType);

			CreateMethod = ImplementProxy();
		}

		/// <summary>
		/// Gets a method that can be used to create the proxy.
		/// </summary>
		public MethodInfo CreateMethod { get; private set; }

		#region Implementation
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

		/// <summary>
		/// Implements a logging proxy around a given interface type.
		/// </summary>
		/// <returns>A static method that can be used to construct the proxy.</returns>
		private MethodInfo ImplementProxy()
		{
			// create a new assembly
			AssemblyName an = Assembly.GetExecutingAssembly().GetName();
			an.Name = TypeImplementer.AssemblyName;
			AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
			ModuleBuilder mb = ab.DefineDynamicModule(an.Name);

			// create a type that implements the given interface
			if (_executeType.IsInterface)
				_typeBuilder = mb.DefineType(_executeType.FullName + "_LoggingProxy", TypeAttributes.Class | TypeAttributes.Public, typeof(Object), new Type[] { _executeType });
			else
				_typeBuilder = mb.DefineType(_executeType.FullName + "_LoggingProxy", TypeAttributes.Class | TypeAttributes.Public, _executeType);

			var ctor = EmitFieldsAndConstructor();

			// create a method that invokes the constructor so we can return a fast delegate
			var createMethod = EmitCreateImpl(ctor);

			// for each method on the interface, try to implement it with a call to eventsource
			var interfaceMethods = _executeType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly);
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

			// look at the parameters on the interface
			var parameters = executeMethod.GetParameters();
			var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

			MethodBuilder m = _typeBuilder.DefineMethod(executeMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual, executeMethod.ReturnType, parameterTypes);
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
			var logMethod = DiscoverMethod(_logField.FieldType, executeMethod.Name, parameterTypes);
			if (logMethod != null)
			{
				// call the log method and throw away the result if there is one
				EmitBaseMethodCall(mIL, _logField, logMethod);
				if (logMethod.ReturnType != typeof(void))
					mIL.Emit(OpCodes.Pop);
			}

			// call execute
			EmitBaseMethodCall(mIL, _executeField, executeMethod);
			if (executeMethod.ReturnType != typeof(void))
				mIL.Emit(OpCodes.Stloc_1);

			var completedParameterTypes = new Type[] { TypeImplementer.GetTypeSupportedByEventSource(executeMethod.ReturnType) };

			// if there is a completed method, then call that
			var completedMethod = DiscoverMethod(_logField.FieldType, executeMethod.Name + "_Completed", completedParameterTypes) ??
				DiscoverMethod(_logField.FieldType, executeMethod.Name + "_Completed", Type.EmptyTypes);
			if (completedMethod != null)
			{
				// load this._log
				mIL.Emit(OpCodes.Ldarg_0);
				mIL.Emit(OpCodes.Ldfld, _logField);

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
						if (_serializationProvider.ShouldSerialize(completedMethod, 0))
						{
							// get the object serializer from the this pointer
							mIL.Emit(OpCodes.Ldarg_0);
							mIL.Emit(OpCodes.Ldfld, _serializerField);

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
		private MethodInfo DiscoverMethod(Type type, string methodName, Type[] parameterTypes)
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
		#endregion
	}
}

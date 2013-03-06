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
		/// The suffix for the _Completed methods.
		/// </summary>
		private const string CompletedSuffix = "_Completed";

		/// <summary>
		/// The name of the context parameter.
		/// </summary>
		private const string Context = "Context";

		/// <summary>
		/// The parameter types for the constructor.
		/// </summary>
		private static Type[] _eventSourceConstructorParameters = new Type[] { typeof(ITraceContextProvider), typeof(ITraceSerializationProvider) };

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
		/// True if this logger supports context.
		/// </summary>
		private bool _supportsContext;

		/// <summary>
		/// The constructor for the type.
		/// </summary>
		private ConstructorInfo _constructor;

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
		/// The context provider for this type.
		/// </summary>
		private ITraceContextProvider _contextProvider;

		/// <summary>
		/// The serialization provider for this type.
		/// </summary>
		private ITraceSerializationProvider _serializationProvider;
		#endregion

		#region Public Methods
		/// <summary>
		/// Initializes a new instance of the TypeImplementer class.
		/// </summary>
		/// <param name="interfaceType">The type to implement.</param>
		/// <param name="contextProvider">The context provider for the event source.</param>
		/// <param name="serializationProvider">The serialization provider for the event source.</param>
		public TypeImplementer(Type interfaceType, ITraceContextProvider contextProvider, ITraceSerializationProvider serializationProvider)
		{
			_interfaceType = interfaceType;
			_contextProvider = contextProvider;
			_serializationProvider = serializationProvider;

			// only interfaces support context
			_supportsContext = contextProvider != null;
			if (_supportsContext && !_interfaceType.IsInterface)
				throw new InvalidOperationException("Context Providers can only be added to interface-based logs.");

			ImplementType();
		}

		/// <summary>
		/// Creates a new instance of the generated EventSource.
		/// </summary>
		/// <returns>A newly constructed EventSource.</returns>
		public object Create()
		{
			// get the providers
			object[] providers = new object[2] 
			{ 
				_contextProvider, 
				_serializationProvider
			};

			return _constructor.Invoke(providers);
		}
		#endregion

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
			EmitFieldsAndConstructor();

			// find all of the methods that need to be implemented
			var interfaceMethods = DiscoverMethods(_interfaceType);
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
				var beginMethod = EmitMethodImpl(interfaceMethod, ref eventId, (EventKeywords)autoKeyword);
				EmitMethodCompletedImpl(interfaceMethod, beginMethod, ref eventId, (EventKeywords)autoKeyword);

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

			// return the constructor for our type
			_constructor = t.GetConstructor(_eventSourceConstructorParameters);
		}

		/// <summary>
		/// Emit the internal fields and the constructor.
		/// </summary>
		private void EmitFieldsAndConstructor()
		{
			// emit provider fields 
			_contextProviderField = _typeBuilder.DefineField("_contextProvider", typeof(ITraceContextProvider), FieldAttributes.Private | FieldAttributes.InitOnly);
			_serializationProviderField = _typeBuilder.DefineField("_serializationProvider", typeof(ITraceSerializationProvider), FieldAttributes.Private | FieldAttributes.InitOnly);

			/*
			 * public ctor(ITraceContextProvider contextProvider, ITraceSerializationProvider serializationProvider)
			 * {
			 *		_contextProvider = contextProvider;
			 *		_serializationProvider = serializationProvider;
			 * }
			 */
			ConstructorBuilder cb = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, _eventSourceConstructorParameters);
			ILGenerator il = cb.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Call, _typeBuilder.BaseType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null));
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Stfld, _contextProviderField);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Stfld, _serializationProviderField);
			il.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Discovers the methods that need to be implemented for a type.
		/// </summary>
		/// <param name="type">The type to implement.</param>
		/// <returns>The virtual and abstract methods that need to be implemented.</returns>
		private List<MethodInfo> DiscoverMethods(Type type)
		{
			List<MethodInfo> methods = new List<MethodInfo>();

			// for interfaces, we need to look at all of the methods that are in the base interfaces
			if (type.IsInterface)
				foreach (Type baseInterface in type.GetInterfaces())
					methods.AddRange(DiscoverMethods(baseInterface));

			BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly;

			// add in the base types
			for (; type != null && type != typeof(object) && type != typeof(EventSource); type = type.BaseType)
				methods.AddRange(type.GetMethods(bindingFlags));

			return methods;
		}
		#endregion

		#region Method Implementation
		/// <summary>
		/// Emits an implementation of a given method.
		/// </summary>
		/// <param name="interfaceMethod">The method to implement.</param>
		/// <param name="eventId">The next eventID to use.</param>
		/// <param name="autoKeyword">The auto-keyword to use if enabled.</param>
		/// <returns>The method that is implemented.</returns>
		private MethodBuilder EmitMethodImpl(MethodInfo interfaceMethod, ref int eventId, EventKeywords autoKeyword)
		{
			// look at the parameters on the interface
			var parameters = interfaceMethod.GetParameters();
			var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

			// some types aren't supported in event source. we will convert them to strings
			var targetParameters = parameterTypes.Select(t => GetTypeSupportedByEventSource(t)).ToList();
			
			// if we are implementing an interface, then add an string context parameter
			if (_supportsContext)
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
				eventAttribute = interfaceMethod.GetCustomAttribute<EventAttribute>();
				if (eventAttribute == null)
				{
					eventAttribute = new EventAttribute(eventId++);
					eventAttribute.Message = methodName;
				}
			}

			// if auto-keywords are enabled, use them
			if (eventAttribute != null && eventAttribute.Keywords == EventKeywords.None)
				eventAttribute.Keywords = autoKeyword;

			// create the internal method that calls WriteEvent
			// this cannot be virtual or static, or the manifest builder will skip it
			// it also cannot return a value
			MethodBuilder m = _typeBuilder.DefineMethod(methodName, MethodAttributes.Public, typeof(void), targetParameterTypes);
			{
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
				if (_supportsContext)
					m.DefineParameter(parameters.Length + 1, ParameterAttributes.In, Context);

				// we have 3 cases
				if (interfaceMethod.IsAbstract)
				{
					// for interface methods, implement a call to write event
					EmitCallWriteEvent(m, eventAttribute, parameterTypes, targetParameterTypes);

					// then implement the interface method as calling the core method
					// this must be virtual to be an interface override
					MethodBuilder im = _typeBuilder.DefineMethod("_" + methodName, MethodAttributes.Public | MethodAttributes.Virtual);
					ProxyHelper.CopyMethodSignature(interfaceMethod, im);
					EmitDirectProxy(im, m, parameterTypes, targetParameterTypes);

					// map our method to the interface implementation
					_typeBuilder.DefineMethodOverride(im, interfaceMethod);
				}
				else if (interfaceMethod.DeclaringType.IsSubclassOf(typeof(EventSource)))
				{
					// if we are implementing an event source, then
					// for non-abstract methods we just proxy the base implementation
					EmitDirectProxy(m, interfaceMethod, parameterTypes, targetParameterTypes);
				}
				else
				{
					// the base class is not an event source, so we are creating an eventsource-derived class
					// that just logs the event
					// so we need to call write event
					EmitCallWriteEvent(m, eventAttribute, parameterTypes, targetParameterTypes);
				}
			}

			return m;
		}

		/// <summary>
		/// Emits a _Completed version of a given event that logs the result of an operation.
		/// The _Completed event is used by TracingProxy to signal the end of a method call.
		/// </summary>
		/// <param name="interfaceMethod">The method to use as a template.</param>
		/// <param name="beginMethod">The begin method for this interface call.</param>
		/// <param name="eventId">The next available event ID.</param>
		/// <param name="autoKeyword">The auto-keyword to use if enabled.</param>
		private void EmitMethodCompletedImpl(MethodInfo interfaceMethod, MethodInfo beginMethod, ref int eventId, EventKeywords autoKeyword)
		{
			// if there is a NonEvent attribute, then no need to emit this method
			if (interfaceMethod.GetCustomAttribute<NonEventAttribute>() != null)
				return;

			// if the method ends in _Completed, then don't emit another one
			if (interfaceMethod.Name.EndsWith(CompletedSuffix, StringComparison.OrdinalIgnoreCase))
				return;

			var methodName = beginMethod.Name + CompletedSuffix;

			// if the interface already has a _Completed method, don't emit a new one
			var parameterTypes = interfaceMethod.ReturnType == typeof(void) ? Type.EmptyTypes : new Type[] { interfaceMethod.ReturnType };
			if (interfaceMethod.DeclaringType.GetMethod(methodName, parameterTypes) != null ||
				interfaceMethod.DeclaringType.GetMethod(methodName, Type.EmptyTypes) != null)
				return;

			var targetParameters = parameterTypes.Select(t => TypeIsSupportedByEventSource(t) ? t : typeof(string)).ToList();
			if (_supportsContext)
				targetParameters.Add(typeof(string));
			var targetParameterTypes = targetParameters.ToArray();

			// determine if this is a non-event or an event
			// if an event, but there is no event attribute, just add one to the last event id
			EventAttribute startEventAttribute = interfaceMethod.GetCustomAttribute<EventAttribute>() ?? new EventAttribute(eventId);
			EventAttribute eventAttribute = new EventAttribute(eventId++)
			{
				Keywords = startEventAttribute.Keywords,
				Level = startEventAttribute.Level,
				Message = methodName,
				Opcode = startEventAttribute.Opcode,
				Task = startEventAttribute.Task,
				Version = startEventAttribute.Version
			};
			if (eventAttribute.Keywords == EventKeywords.None)
				eventAttribute.Keywords = autoKeyword;

			// create the internal method that calls WriteEvent
			// this cannot be virtual or static, or the manifest builder will skip it
			// it also cannot return a value
			MethodBuilder m = _typeBuilder.DefineMethod(methodName, MethodAttributes.Public, typeof(void), targetParameterTypes);
			m.SetCustomAttribute(EventAttributeHelper.ConvertEventAttributeToAttributeBuilder(eventAttribute));

			// the base class is not an event source, so we are creating an eventsource-derived class
			// that just logs the event
			// so we need to call write event
			EmitCallWriteEvent(m, eventAttribute, targetParameterTypes, targetParameterTypes);
		}

		/// <summary>
		/// Emit a call to WriteEvent(param object[]).
		/// </summary>
		/// <param name="methodBuilder">The MethodBuilder to append to.</param>
		/// <param name="eventAttribute">The EventAttribute to use as values in the method.</param>
		/// <param name="sourceParameterTypes">The types of parameters on the source method.</param>
		/// <param name="targetParameterTypes">The types of the parameters on the target method.</param>
		private void EmitCallWriteEvent(MethodBuilder methodBuilder, EventAttribute eventAttribute, Type[] sourceParameterTypes, Type[] targetParameterTypes)
		{
			ILGenerator mIL = methodBuilder.GetILGenerator();

			// if there is no event attribute, then this is a non-event, so just return silently
			if (eventAttribute == null)
			{
				mIL.Emit(OpCodes.Ret);
				return;
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
			if (_supportsContext)
			{
				// load the array and index onto the stack
				mIL.Emit(OpCodes.Dup);
				mIL.Emit(OpCodes.Ldc_I4, (int)targetParameterTypes.Length - 1);

				// load the context provider and get the context
				mIL.Emit(OpCodes.Ldarg_0);
				mIL.Emit(OpCodes.Ldfld, _contextProviderField);
				mIL.Emit(OpCodes.Callvirt, typeof(ITraceContextProvider).GetMethod("ProvideContext"));

				// put the result into the array
				mIL.Emit(OpCodes.Stelem, typeof(object));
			}

			// call writeevent
			mIL.Emit(OpCodes.Call, _writeEvent);
			mIL.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Emits a proxy to a method that just calls the base method.
		/// </summary>
		/// <param name="methodBuilder">The method to implement.</param>
		/// <param name="baseMethod">The base method.</param>
		/// <param name="sourceParameterTypes">The types of the parameters on the source method.</param>
		/// <param name="targetParameterTypes">The types of the parameters on the target method.</param>
		private void EmitDirectProxy(MethodBuilder methodBuilder, MethodInfo baseMethod, Type[] sourceParameterTypes, Type[] targetParameterTypes)
		{
			ILGenerator mIL = methodBuilder.GetILGenerator();

			// copy the parameters to the stack
			// arg.0 = this
			// so we go to length+1
			mIL.Emit(OpCodes.Ldarg_0);
			for (int i = 1; i < sourceParameterTypes.Length + 1; i++)
			{
				ProxyHelper.EmitSerializeValue(
					methodBuilder,
					i,
					sourceParameterTypes[i - 1],
					targetParameterTypes[i - 1],
					_serializationProvider,
					_serializationProviderField,
					il => il.Emit(OpCodes.Ldarg, i),
					il => il.Emit(OpCodes.Ldarga_S, i));
			}

			// if this method supports context, then add a context parameter
			// note that we pass null in here and then build the context from within EmitCallWriteEvent
			if (_supportsContext)
				mIL.Emit(OpCodes.Ldnull);

			// now that all of the parameters have been loaded, call the base method
			mIL.Emit(OpCodes.Call, baseMethod);

			// if we need to return a value, but the base implementation doesn't return a value
			// (i.e. just calling WriteEvent and returning void)
			// then we need to manufacture the return value as default(T)
			// ldnull works well for this
			// this is important when we create logging proxies
			if (methodBuilder.ReturnType != null && methodBuilder.ReturnType != typeof(void) && baseMethod.ReturnType == typeof(void))
			{
				// for generics and values, init a local object with a blank object
				if (methodBuilder.ReturnType.IsGenericParameter || methodBuilder.ReturnType.IsValueType)
				{
					var returnValue = mIL.DeclareLocal(methodBuilder.ReturnType);
					mIL.Emit(OpCodes.Ldloca_S, returnValue);
					mIL.Emit(OpCodes.Initobj, methodBuilder.ReturnType);
					mIL.Emit(OpCodes.Ldloc, returnValue);
				}
				else
					mIL.Emit(OpCodes.Ldnull);
			}

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
	}
}
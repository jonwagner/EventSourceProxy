using System;
using System.Collections.Generic;
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
	/// Methods to help with building proxies.
	/// </summary>
	static class ProxyHelper
	{
		/// <summary>
		/// The name of the generated dynamic assembly.
		/// </summary>
		internal const string AssemblyName = "EventSourceImplementation";

		/// <summary>
		/// Copy the generic attributes of a method.
		/// </summary>
		/// <param name="sourceMethod">The source method.</param>
		/// <param name="targetMethod">The target method.</param>
		internal static void CopyGenericSignature(MethodInfo sourceMethod, MethodBuilder targetMethod)
		{
			if (sourceMethod.IsGenericMethod)
			{
				// get the interface's generic types and make our own
				var oldTypes = sourceMethod.GetGenericArguments();
				var newTypes = targetMethod.DefineGenericParameters(oldTypes.Select(t => t.Name).ToArray());
				for (int i = 0; i < newTypes.Length; i++)
				{
					var oldType = oldTypes[i];
					var newType = newTypes[i];

					newType.SetGenericParameterAttributes(oldType.GenericParameterAttributes);
					newType.SetInterfaceConstraints(oldType.GetGenericParameterConstraints());
				}
			}
		}

		/// <summary>
		/// Copies the method signature from one method to another.
		/// This includes generic parameters, constraints and parameters.
		/// </summary>
		/// <param name="sourceMethod">The source method.</param>
		/// <param name="targetMethod">The target method.</param>
		internal static void CopyMethodSignature(MethodInfo sourceMethod, MethodBuilder targetMethod)
		{
			CopyGenericSignature(sourceMethod, targetMethod);

			targetMethod.SetReturnType(sourceMethod.ReturnType);

			// copy the parameters and attributes
			// it seems that we can use the source parameters directly because the target method is derived
			// from the source method
			var parameters = sourceMethod.GetParameters();
			targetMethod.SetParameters(parameters.Select(p => p.ParameterType).ToArray());

			for (int i = 0; i < parameters.Length; i++)
			{
				var parameter = parameters[i];
				targetMethod.DefineParameter(i + 1, parameter.Attributes, parameter.Name);
			}
		}

		/// <summary>
		/// Emits the code needed to properly push an object on the stack,
		/// serializing the value if necessary.
		/// </summary>
		/// <param name="methodBuilder">The method currently being built.</param>
		/// <param name="invocationContext">The invocation context for this call.</param>
		/// <param name="invocationContexts">A list of invocation contexts that will be appended to.</param>
		/// <param name="invocationContextsField">The static field containing the array of invocation contexts at runtime.</param>
		/// <param name="parameterMapping">The mapping of source parameters to destination parameters.</param>
		/// <param name="serializationProvider">The serialization provider for the current interface.</param>
		/// <param name="serializationProviderField">
		/// The field on the current object that contains the serialization provider at runtime.
		/// This method assume the current object is stored in arg.0.
		/// </param>
		internal static void EmitSerializeValue(
			MethodBuilder methodBuilder,
			InvocationContext invocationContext,
			List<InvocationContext> invocationContexts,
			FieldBuilder invocationContextsField,
			ParameterMapping parameterMapping,
			TraceSerializationProvider serializationProvider,
			FieldBuilder serializationProviderField)
		{
			if (parameterMapping.MappingType == ParameterMappingType.ReturnValue)
			{
				EmitSerializeValue(
					methodBuilder,
					invocationContext,
					invocationContexts,
					invocationContextsField,
					0,
					parameterMapping.SourceType,
					parameterMapping.CleanTargetType,
					serializationProvider,
					serializationProviderField);
				return;
			}

			var sourceCount = parameterMapping.Sources.Count();
			if (sourceCount == 0)
				return;

			if (sourceCount == 1)
			{
				var parameter = parameterMapping.Sources.First();
				EmitSerializeValue(
					methodBuilder,
					invocationContext,
					invocationContexts,
					invocationContextsField,
					parameter.Position,
					parameter.SourceType,
					parameterMapping.CleanTargetType,
					serializationProvider,
					serializationProviderField);
				return;
			}

			var il = methodBuilder.GetILGenerator();

			// use the serializer to serialize the objects
			var context = new TraceSerializationContext(invocationContext.SpecifyType(InvocationContextTypes.BundleParameters), -1);
			context.EventLevel = serializationProvider.GetEventLevelForContext(context);

			if (context.EventLevel != null)
			{
				// get the object serializer from the this pointer
				il.Emit(OpCodes.Ldsfld, serializationProviderField);

				// create a new dictionary strings and values
				il.Emit(OpCodes.Newobj, typeof(Dictionary<string, string>).GetConstructor(Type.EmptyTypes));

				foreach (var parameter in parameterMapping.Sources)
				{
					il.Emit(OpCodes.Dup);
					il.Emit(OpCodes.Ldstr, parameter.Name);

					EmitSerializeValue(
						methodBuilder,
						invocationContext,
						invocationContexts,
						invocationContextsField,
						parameter.Position,
						parameter.SourceType,
						parameterMapping.CleanTargetType,
						serializationProvider, 
						serializationProviderField);

					var method = typeof(Dictionary<string, string>).GetMethod("Add");
					il.Emit(OpCodes.Call, method);
				}

				// get the invocation context from the array on the provider
				il.Emit(OpCodes.Ldsfld, invocationContextsField);
				il.Emit(OpCodes.Ldc_I4, invocationContexts.Count);
				il.Emit(OpCodes.Ldelem, typeof(TraceSerializationContext));
				invocationContexts.Add(context);

				il.Emit(OpCodes.Callvirt, typeof(TraceSerializationProvider).GetMethod("ProvideSerialization", BindingFlags.Instance | BindingFlags.Public));
			}
			else
				il.Emit(OpCodes.Ldnull);
		}

		/// <summary>
		/// Emits the code needed to properly push an object on the stack,
		/// serializing the value if necessary.
		/// </summary>
		/// <param name="methodBuilder">The method currently being built.</param>
		/// <param name="invocationContext">The invocation context for this call.</param>
		/// <param name="invocationContexts">A list of invocation contexts that will be appended to.</param>
		/// <param name="invocationContextsField">The static field containing the array of invocation contexts at runtime.</param>
		/// <param name="i">The index of the current parameter being pushed.</param>
		/// <param name="sourceType">The type that the parameter is being converted from.</param>
		/// <param name="targetType">The type that the parameter is being converted to.</param>
		/// <param name="serializationProvider">The serialization provider for the current interface.</param>
		/// <param name="serializationProviderField">
		/// The field on the current object that contains the serialization provider at runtime.
		/// This method assume the current object is stored in arg.0.
		/// </param>
		internal static void EmitSerializeValue(
			MethodBuilder methodBuilder,
			InvocationContext invocationContext,
			List<InvocationContext> invocationContexts,
			FieldBuilder invocationContextsField,
			int i,
			Type sourceType,
			Type targetType,
			TraceSerializationProvider serializationProvider,
			FieldBuilder serializationProviderField)
		{
			ILGenerator mIL = methodBuilder.GetILGenerator();

			// if the source type is a reference to the target type, we have to dereference it
			if (sourceType.IsByRef && sourceType.GetElementType() == targetType)
			{
				mIL.Emit(OpCodes.Ldarg, (int)i + 1);
				sourceType = sourceType.GetElementType();
				mIL.Emit(OpCodes.Ldobj, sourceType);
				return;
			}

			// if the types match, just put the argument on the stack
			if (sourceType == targetType)
			{
				mIL.Emit(OpCodes.Ldarg, (int)i + 1);
				return;
			}

			// this is not a match, so convert using the serializer.
			// verify that the target type is a string
			if (targetType != typeof(string))
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Cannot convert type {0} to a type compatible with EventSource", targetType.FullName));

			// for fundamental types, just convert them with ToString and be done with it
			var underlyingType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
			if (!sourceType.IsGenericParameter && (underlyingType.IsEnum || (underlyingType.IsValueType && underlyingType.Assembly == typeof(string).Assembly)))
			{
				// convert the argument to a string with ToString
				mIL.Emit(OpCodes.Ldarga_S, i + 1);
				mIL.Emit(OpCodes.Call, sourceType.GetMethod("ToString", Type.EmptyTypes));
				return;
			}

			// non-fundamental types use the object serializer
			var context = new TraceSerializationContext(invocationContext, i);
			context.EventLevel = serializationProvider.GetEventLevelForContext(context);
			if (context.EventLevel != null)
			{
				// get the object serializer from the this pointer
				mIL.Emit(OpCodes.Ldsfld, serializationProviderField);

				// load the value
				mIL.Emit(OpCodes.Ldarg, (int)i + 1);

				// if the source type is a reference to the target type, we have to dereference it
				if (sourceType.IsByRef)
				{
					sourceType = sourceType.GetElementType();
					mIL.Emit(OpCodes.Ldobj, sourceType);
				}

				// if it's a value type, we have to box it to log it
				if (sourceType.IsGenericParameter || sourceType.IsValueType)
					mIL.Emit(OpCodes.Box, sourceType);

				// get the invocation context from the array on the provider
				mIL.Emit(OpCodes.Ldsfld, invocationContextsField);
				mIL.Emit(OpCodes.Ldc_I4, invocationContexts.Count);
				mIL.Emit(OpCodes.Ldelem, typeof(TraceSerializationContext));
				invocationContexts.Add(context);

				mIL.Emit(OpCodes.Callvirt, typeof(TraceSerializationProvider).GetMethod("ProvideSerialization", BindingFlags.Instance | BindingFlags.Public));
			}
			else
				mIL.Emit(OpCodes.Ldnull);
		}

		/// <summary>
		/// Determine if the parameters of a method match a list of parameter types.
		/// </summary>
		/// <param name="m">The method to test.</param>
		/// <param name="targetTypes">The list of parameter types.</param>
		/// <returns>True if the types of parameters match.</returns>
		internal static bool ParametersMatch(MethodInfo m, Type[] targetTypes)
		{
			var sourceTypes = m.GetParameters().Select(p => p.ParameterType).ToArray();

			// need to have the same number of types
			if (sourceTypes.Length != targetTypes.Length)
				return false;

			for (int i = 0; i < sourceTypes.Length; i++)
			{
				var sourceType = sourceTypes[i];
				var targetType = targetTypes[i];

				// if they match exactly, then go to the next parameter
				if (sourceType == targetType)
					continue;

				// if both are generics of the same type, then they match
				if (sourceType.IsGenericType &&
					targetType.IsGenericType &&
					sourceType.GetGenericTypeDefinition() == targetType.GetGenericTypeDefinition())
					continue;

				// if both are generic parameters and they match by name, then we can use this
				// NOTE: this only works because we copy generic parameters and keep their names
				if (sourceType.IsGenericParameter &&
					targetType.IsGenericParameter &&
					sourceType.Name == targetType.Name)
					continue;

				return false;
			}

			return true;
		}

		/// <summary>
		/// Discovers the methods that need to be implemented for a type.
		/// </summary>
		/// <param name="type">The type to implement.</param>
		/// <returns>The virtual and abstract methods that need to be implemented.</returns>
		internal static List<MethodInfo> DiscoverMethods(Type type)
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

		/// <summary>
		/// Emits the code to load the default value of a type onto the stack.
		/// </summary>
		/// <param name="mIL">The ILGenerator to emit to.</param>
		/// <param name="type">The type of object to emit.</param>
		internal static void EmitDefaultValue(ILGenerator mIL, Type type)
		{
			// if there is no type, then we don't need a value
			if (type == null || type == typeof(void))
				return;

			// for generics and values, init a local object with a blank object
			if (type.IsGenericParameter || type.IsValueType)
			{
				var returnValue = mIL.DeclareLocal(type);
				mIL.Emit(OpCodes.Ldloca_S, returnValue);
				mIL.Emit(OpCodes.Initobj, type);
				mIL.Emit(OpCodes.Ldloc, returnValue);
			}
			else
				mIL.Emit(OpCodes.Ldnull);
		}
	}
}

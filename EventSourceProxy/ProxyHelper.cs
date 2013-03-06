using System;
using System.Collections.Generic;
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
		/// Copies the method signature from one method to another.
		/// This includes generic parameters, constraints and parameters.
		/// </summary>
		/// <param name="sourceMethod">The source method.</param>
		/// <param name="targetMethod">The target method.</param>
		internal static void CopyMethodSignature(MethodInfo sourceMethod, MethodBuilder targetMethod)
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

			targetMethod.SetReturnType(sourceMethod.ReturnType);

			// copy the parameters and attributes
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
		/// <param name="i">The index of the current parameter being pushed.</param>
		/// <param name="sourceType">The type that the parameter is being converted from.</param>
		/// <param name="targetType">The type that the parameter is being converted to.</param>
		/// <param name="serializationProvider">The serialization provider for the current interface.</param>
		/// <param name="serializationProviderField">
		/// The field on the current object that contains the serialization provider at runtime.
		/// This method assume the current object is stored in arg.0.
		/// </param>
		/// <param name="emitLoadValue">An action that emits the code needed to load the value onto the stack.</param>
		/// <param name="emitLoadValueReference">An action that emits the code needed to load a reference to the value onto the stack.</param>
		internal static void EmitSerializeValue(
			MethodBuilder methodBuilder,
			int i,
			Type sourceType,
			Type targetType,
			ITraceSerializationProvider serializationProvider,
			FieldBuilder serializationProviderField,
			Action<ILGenerator> emitLoadValue,
			Action<ILGenerator> emitLoadValueReference)
		{
			ILGenerator mIL = methodBuilder.GetILGenerator();

			// if the source type is a reference to the target type, we have to dereference it
			if (sourceType.IsByRef && sourceType.GetElementType() == targetType)
			{
				emitLoadValue(mIL);
				sourceType = sourceType.GetElementType();
				mIL.Emit(OpCodes.Ldobj, sourceType);
				return;
			}

			// if the types match, just put the argument on the stack
			if (sourceType == targetType)
			{
				emitLoadValue(mIL);
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
				emitLoadValueReference(mIL);
				mIL.Emit(OpCodes.Call, sourceType.GetMethod("ToString", Type.EmptyTypes));
				return;
			}

			// note that the parameter index is 1-based, but the parameters from the method builder will be 0-based
			int parameterIndex = i - 1;

			// non-fundamental types use the object serializer
			if (serializationProvider.ShouldSerialize(methodBuilder, parameterIndex))
			{
				// get the object serializer from the this pointer
				mIL.Emit(OpCodes.Ldarg_0);
				mIL.Emit(OpCodes.Ldfld, serializationProviderField);

				// load the value
				emitLoadValue(mIL);

				// if the source type is a reference to the target type, we have to dereference it
				if (sourceType.IsByRef)
				{
					sourceType = sourceType.GetElementType();
					mIL.Emit(OpCodes.Ldobj, sourceType);
				}

				// if it's a value type, we have to box it to log it
				if (sourceType.IsGenericParameter || sourceType.IsValueType)
					mIL.Emit(OpCodes.Box, sourceType);

				// add the method builder and parameter index
				mIL.Emit(OpCodes.Ldtoken, methodBuilder);
				mIL.Emit(OpCodes.Ldc_I4, parameterIndex);

				mIL.Emit(OpCodes.Callvirt, typeof(ITraceSerializationProvider).GetMethod("SerializeObject", BindingFlags.Instance | BindingFlags.Public));
			}
			else
				mIL.Emit(OpCodes.Ldnull);
		}

		/// <summary>
		/// Determine if the parameters of a method match a list of parameter types.
		/// </summary>
		/// <param name="m">The method to test.</param>
		/// <param name="expectedParameters">The list of parameter types.</param>
		/// <returns>True if the types of parameters match.</returns>
		internal static bool ParametersMatch(MethodInfo m, Type[] expectedParameters)
		{
			var p = m.GetParameters();

			if (p.Length != expectedParameters.Length)
				return false;

			for (int i = 0; i < p.Length; i++)
				if (p[i].ParameterType != expectedParameters[i])
					return false;

			return true;
		}
	}
}

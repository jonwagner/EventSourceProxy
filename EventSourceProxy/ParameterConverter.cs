using System;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace EventSourceProxy
{
	/// <summary>
	/// Handles custom conversion of logging data by invoking LambdaExpressions.
	/// </summary>

	class ParameterConverter
	{
		/// <summary>
		/// Gets the type of the input parameter, or null if there are no parameters.
		/// </summary>
		public Type InputType { get; private set; }

		/// <summary>
		/// Gets the type of the return value.
		/// </summary>
		public Type ReturnType { get; private set; }

		private Func<ILGenerator, Type> _generator;

		/// <summary>
		/// Initializes a new instance of the ParameterConverter class.
		/// </summary>
		/// <param name="inputType">The type of the input parameter.</param>
		/// <param name="returnType">The type returned from the converter.</param>
		/// <param name="generator">A function that emits the conversion code and returns the return type.</param>
		public ParameterConverter(Type inputType, Type returnType, Func<ILGenerator, Type> generator)
		{
			InputType = inputType;
			ReturnType = returnType;
			_generator = generator;
		}

		/// <summary>
		/// Initializes a new instance of the ParameterConverter class.
		/// </summary>
		/// <param name="expression">A LambdaExpression that converts the value.</param>
		public ParameterConverter(LambdaExpression expression)
		{
			var hasParameters = (expression.Parameters.Count > 0);

			InputType = hasParameters ? expression.Parameters[0].Type : null;
			ReturnType = expression.ReturnType;

			_generator = (il =>
			{
				var del = expression.Compile();

				if (hasParameters)
				{
					var local = il.DeclareLocal(InputType);
					il.Emit(OpCodes.Stloc, local.LocalIndex);
					StaticFieldStorage.EmitLoad(il, del);
					il.Emit(OpCodes.Ldloc, local.LocalIndex);
					il.Emit(OpCodes.Call, del.GetType().GetMethod("Invoke"));					
				}
				else
				{
					StaticFieldStorage.EmitLoad(il, del);
					il.Emit(OpCodes.Call, del.GetType().GetMethod("Invoke"));					
				}

				return expression.ReturnType;
			});
		}

		/// <summary>
		/// Emits the IL to perform the conversion.
		/// </summary>
		/// <returns>The return type of the conversion.</returns>
		public Type Emit(ILGenerator il)
		{
			return _generator(il);
		}
	}
}

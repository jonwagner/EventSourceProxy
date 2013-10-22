namespace EventSourceProxy
{
	/// <summary>
	/// Encapsulates a placeholder value that can be passed to a method.
	/// </summary>
	/// <typeparam name="TType">The type of the placeholder.</typeparam>
    public static class Any<TType>
    {
		/// <summary>
		/// Gets the default value for the Any type.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
		public static TType Value 
        {
            get { return default(TType); }
        }
    }
}

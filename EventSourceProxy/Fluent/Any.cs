
namespace EventSourceProxy.Fluent
{
    public static class Any<TType>
    {
        public static TType Ignore 
        {
            get { return default(TType); }
        }
    }
}

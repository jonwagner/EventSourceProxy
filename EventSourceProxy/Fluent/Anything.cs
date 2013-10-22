
namespace EventSourceProxy.Fluent
{
    public static class Anything
    {
        public static ITraceDescriptionForParam<TParam> With<TParam>()
        {
            return new TraceDescriptionForParam<TParam>();
        }
    }
}

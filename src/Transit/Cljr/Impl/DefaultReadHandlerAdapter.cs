using Sellars.Transit.Alpha;

namespace Sellars.Transit.Cljr.Impl
{
    internal abstract class DefaultReadHandlerAdapter : IDefaultReadHandler<object>, IDefaultReadHandler
    {
        public static IDefaultReadHandler Adapt(IDefaultReadHandler<object> defaultReadHandler) =>
            defaultReadHandler as IDefaultReadHandler
            ?? new TypedToUntyped(defaultReadHandler);

        public static IDefaultReadHandler<object> Adapt(IDefaultReadHandler defaultReadHandler) =>
            defaultReadHandler as IDefaultReadHandler<object>
            ?? new UntypedToTyped(defaultReadHandler);

        public abstract object FromRepresentation(string tag, object representation);

        public class UntypedToTyped : DefaultReadHandlerAdapter
        {
            private readonly IDefaultReadHandler customDefaultHandler;

            public UntypedToTyped(IDefaultReadHandler customDefaultHandler)
            {
                this.customDefaultHandler = customDefaultHandler;
            }

            public override object FromRepresentation(string tag, object representation) =>
                customDefaultHandler == null
                    ? new Beerendonk.Transit.Impl.TaggedValue(tag, representation)
                    : customDefaultHandler.FromRepresentation(tag, representation);
        }

        public class TypedToUntyped : DefaultReadHandlerAdapter
        {
            private readonly IDefaultReadHandler<object> customDefaultHandler;

            public TypedToUntyped(IDefaultReadHandler<object> customDefaultHandler)
            {
                this.customDefaultHandler = customDefaultHandler;
            }

            public override object FromRepresentation(string tag, object representation) =>
                customDefaultHandler == null
                    ? new Beerendonk.Transit.Impl.TaggedValue(tag, representation)
                    : customDefaultHandler.FromRepresentation(tag, representation);
        }
    }
}

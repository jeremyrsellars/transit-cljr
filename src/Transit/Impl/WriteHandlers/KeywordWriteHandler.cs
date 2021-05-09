using Sellars.Transit.Alpha;

namespace Beerendonk.Transit.Impl.WriteHandlers
{
    internal class KeywordWriteHandler : AbstractWriteHandler, IKnownTag
    {
        private readonly string t;

        public KeywordWriteHandler(string t)
        {
            this.t = t;
        }

        public string KnownTag => t;

        public override string Tag(object ignored) => KnownTag;

        public override object Representation(object obj)
        {
            return obj.ToString().Substring(1);
        }

        public override string StringRepresentation(object obj)
        {
            return (string)Representation(obj);
        }
    }
}

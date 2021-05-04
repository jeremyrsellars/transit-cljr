namespace Beerendonk.Transit.Impl.WriteHandlers
{
    internal class KeywordWriteHandler : AbstractWriteHandler
    {
        private readonly string t;

        public KeywordWriteHandler(string t)
        {
            this.t = t;
        }

        public override string Tag(object ignored)
        {
            return t;
        }

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

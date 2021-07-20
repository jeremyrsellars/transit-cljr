using clojure.lang;
using Sellars.Transit.Alpha;

namespace Sellars.Transit.Cljr.Impl.ReadHandlers
{
    internal class BigDecimalReadHandler : IReadHandler
    {
        public object FromRepresentation(object representation)
        {
            BigDecimal result;
            if (!BigDecimal.TryParse((string)representation, out result))
            {
                throw new TransitException($"Cannot parse representation as a {nameof(BigDecimal)}: " + representation);
            }

            return result;
        }
    }
}

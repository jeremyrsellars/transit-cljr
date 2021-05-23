using clojure.lang;

namespace Sellars.Transit
{
    class Program
    {
        const string MainNS = "TransitTool.roundtrip";

        static void Main(string[] args)
        {
            DelayedClj.RequireNS(MainNS);
            RT.var(MainNS, "-main").applyTo(RT.arrayToList(args));
        }
    }
}

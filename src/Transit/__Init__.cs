using clojure.lang;

// ClojureCLR: old init class name
// https://github.com/clojure/clojure-clr/blob/886631fc104c31683eff977b9c7b48d9165cdd84/Clojure/Clojure/CljCompiler/Compiler.cs#L1753
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
/// <summary>
/// This is called by ClojureClR's require.
/// </summary>
public class __Init__
{
    public static int onlyInitOnce = 0;
    // https://github.com/clojure/clojure-clr/blob/886631fc104c31683eff977b9c7b48d9165cdd84/Clojure/Clojure/CljCompiler/Compiler.cs#L1766
    public static void Initialize()
    {
        if (System.Threading.Interlocked.Increment(ref onlyInitOnce) != 1)
            return;

        RT.load("sellars.transit.alpha");
    }
}

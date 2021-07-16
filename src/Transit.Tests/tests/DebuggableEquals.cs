using clojure.lang;

public static class Debuggable
{
    public static void WriteLine(object x)
    {
        System.Console.WriteLine(x);
    }

    public static void WriteLine(params object [] args)
    {
        foreach(var arg in args)
        {
            System.Console.Write(arg);
        }
        System.Console.WriteLine();
    }

    public static new bool Equals(object a, object b)
    {
        bool eq = RT.IsTrue(RT.var("clojure.core", "=").invoke(a, b));
        if (eq)
            return true;
        System.Console.WriteLine($"Not Equal:\n{a}\n      and:\n{b}");
        return eq;
    }

    public static bool NormalizedEquals(object a, object b, IFn normalize)
    {
        if (Equals(a, b))
            return true;
        if (normalize == null)
            return false;

        bool eq = RT.IsTrue(RT.var("clojure.core", "=").invoke(normalize.invoke(a), normalize.invoke(b)));
        if (eq)
            return true;
        System.Console.WriteLine($"Not Equal after normalization:\n{a}\n      and:\n{b}");
        return eq;
    }
}

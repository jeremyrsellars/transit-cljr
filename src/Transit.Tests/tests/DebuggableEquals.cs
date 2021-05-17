using clojure.lang;

public static class Debuggable
{
    public static new bool Equals(object a, object b)
    {
        bool eq = RT.IsTrue(RT.var("clojure.core", "=").invoke(a, b));
        if (eq)
            return true;
        System.Console.WriteLine($"Not Equal:\n{a}\n      and:\n{b}");
        return eq;
    }
}

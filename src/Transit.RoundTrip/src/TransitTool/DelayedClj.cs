using System;
using System.IO;
using System.Reflection;
using static clojure.lang.RT;

namespace Sellars.Transit
{
    public static class DelayedClj
    {
        static DelayedClj()
        {
            Init();
        }

        internal static void RequireNS(string ns)
        {
            var fileNs = ns.Replace("-", "_");
#if DEBUG
            LoadClojureStringFromResource(fileNs);
#endif
            load(fileNs);
        }

        internal static void RequireFileNS(string fileNs)
        {
            load(fileNs);
        }

        internal static string LoadClojureStringFromResource(string fileNs) =>
            LoadClojureStringFromResource(typeof(DelayedClj).Assembly, fileNs);

        internal static string LoadClojureStringFromResource(Assembly assembly, string fileNs)
        {
            return new StreamReader(
                assembly.GetManifestResourceStream(fileNs + ".cljc")
                ?? assembly.GetManifestResourceStream(fileNs + ".clj")
                ?? throw new InvalidOperationException(
                    $"Missing resource {fileNs}.cljc\r\nDid you mean one of these?\r\n{string.Join(Environment.NewLine, assembly.GetManifestResourceNames())}"))
                .ReadToEnd();
        }
    }
}
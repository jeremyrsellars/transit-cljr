using System;

namespace Sellars.Transit.Util.Alpha
{
    public static class TimeUtils
    {
        public static readonly DateTime Epoch = new DateTime(1970, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        public static readonly long TicksPerMilliseconds = TimeSpan.FromMilliseconds(1).Ticks;

        public static long ToTransitTime(DateTime d) =>
            (ToUtcAssumeUtcForUnspecified(d) - Epoch).Ticks / TicksPerMilliseconds;

        public static DateTime FromTransitTime(long msSinceEpoch) =>
            Epoch.AddTicks(msSinceEpoch * TicksPerMilliseconds);

        public static DateTime ToUtcAssumeUtcForUnspecified(DateTime inst)
        {
            switch (inst.Kind)
            {
                case DateTimeKind.Unspecified:
                    return new DateTime(inst.Ticks, DateTimeKind.Utc);
                case DateTimeKind.Utc:
                    return inst;
                case DateTimeKind.Local:
                default:
                    return inst.ToUniversalTime();
            }
        }
    }
}

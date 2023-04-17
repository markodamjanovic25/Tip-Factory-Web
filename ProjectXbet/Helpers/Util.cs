using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectXbet.Helpers
{
    public static class Util
    {
        public static bool In<T>(this T source, params T[] list)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (list == null)
                throw new ArgumentNullException("list");

            return source.In(list.AsEnumerable());
        }

        public static bool In<T>(this T source, IEnumerable<T> list)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (list == null)
                throw new ArgumentNullException("list");

            return list.Contains(source);
        }
    }
}

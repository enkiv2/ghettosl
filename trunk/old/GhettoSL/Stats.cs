using System;
using System.Collections.Generic;
using System.Text;

namespace ghetto
{
    /// <summary>
    /// Client statistics for things like uptime, money earned, etc.
    /// </summary>
    public static class Stats
    {
        /// <summary>
        /// Unix timestamp of when the client was launched
        /// </summary>
        public static uint StartTime;
        /// <summary>
        /// L$ paid out to objects/avatars since login
        /// </summary>
        public static int MoneySpent;
        /// <summary>
        /// L$ received since login (before subtracting .MoneySpent)
        /// </summary>
        public static int MoneyReceived;
    }
}

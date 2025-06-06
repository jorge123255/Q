using System;

namespace Quasar.Common.Utilities
{
    /// <summary>
    /// Helper class for DateTime operations
    /// </summary>
    public static class DateTimeHelper
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Converts a DateTime to Unix timestamp (milliseconds since Unix epoch)
        /// </summary>
        /// <param name="dateTime">The DateTime to convert</param>
        /// <returns>Unix timestamp in milliseconds</returns>
        public static long ToUnixTimeMilliseconds(this DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - UnixEpoch).TotalMilliseconds;
        }

        /// <summary>
        /// Converts a DateTimeOffset to Unix timestamp (milliseconds since Unix epoch)
        /// </summary>
        /// <param name="dateTime">The DateTimeOffset to convert</param>
        /// <returns>Unix timestamp in milliseconds</returns>
        public static long ToUnixTimeMilliseconds(this DateTimeOffset dateTime)
        {
            return (long)(dateTime.UtcDateTime - UnixEpoch).TotalMilliseconds;
        }
    }
}

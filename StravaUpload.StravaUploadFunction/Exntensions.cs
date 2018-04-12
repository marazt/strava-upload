using System;
using System.Globalization;

namespace StravaUpload.StravaUploadFunction
{
    public static class Exntensions
    {
        public static string ToIsoString(this DateTime date) => date.ToString("o", CultureInfo.InvariantCulture);
    }
}
using System;

namespace StravaUpload.Lib
{
    public class RangePair
    {
        public RangePair(DateTime from, DateTime to)
        {
            this.From = from.ToUniversalTime();
            this.To = to.ToUniversalTime();
        }

        private DateTime From { get; }

        private DateTime To { get; }

        public override bool Equals(object obj)
        {
            if (obj is RangePair rangeToCompare)
            {
                return this.AreDatesSame(this.From, rangeToCompare.From) && this.AreDatesSame(rangeToCompare.To, rangeToCompare.To);
            }
            return false;
        }

        private string FormateDate (DateTime date) => date.ToString("yyyy-MM-ddTHH:mmZ");

        private bool AreDatesSame(DateTime dt1, DateTime dt2) => dt1.Date == dt2.Date && dt1.Hour == dt2.Hour && dt1.Minute == dt2.Minute;

        public override int GetHashCode()
        {
            return $"{this.FormateDate(this.From)}|{this.FormateDate(this.To)}".GetHashCode();
        }
    }
}

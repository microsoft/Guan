using System;
using System.Collections.Generic;

namespace Guan.Common
{
    public class TimeRangeList : List<TimeRange>
    {
        public void AddRange(TimeRange range)
        {
            int index = FindRangeContainsOrAfter(range.Begin);
            if (index == Count || this[index].Begin > range.End)
            {
                Insert(index, range);
            }
            else
            {
                this[index].Merge(range);
                int i;
                for (i = index + 1; i < Count && this[i].Begin <= this[index].End; i++)
                {
                    this[index].Merge(this[i]);
                }

                if (i > index + 1)
                {
                    RemoveRange(index + 1, i - index - 1);
                }
            }
        }

        public TimeRange GetFirstMissingRange(TimeRange range, TimeSpan minDuration)
        {
            DateTime begin = range.Begin;
            int index = FindRangeContainsOrAfter(range.Begin);

            while (index < Count && this[index].Begin < range.End)
            {
                TimeSpan duration = this[index].Begin - begin;
                if (duration >= minDuration)
                {
                    return new TimeRange(begin, this[index].Begin);
                }

                begin = this[index].End;
                index++;
            }

            if (begin > range.End)
            {
                return null;
            }

            return new TimeRange(begin, range.End);
        }

        private int FindRangeContainsOrAfter(DateTime time)
        {
            int startIndex = 0;
            int endIndex = Count;

            while (startIndex < endIndex)
            {
                int mid = (startIndex + endIndex) / 2;
                if (this[mid].End < time)
                {
                    startIndex = mid + 1;
                }
                else
                {
                    endIndex = mid;
                }
            }

            return startIndex;
        }
    }
}

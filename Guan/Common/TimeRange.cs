// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Guan.Common
{

    public class TimeRange : Range<DateTime>, IComparable<TimeRange>
    {
        public static readonly TimeRange Full = new TimeRange(DateTime.MinValue, DateTime.MaxValue);
        public static readonly TimeRange Empty = new TimeRange(DateTime.MinValue, DateTime.MinValue);

        public TimeRange(DateTime begin, DateTime end, bool skipCheck = false)
            : base(begin, end, false, true, skipCheck)
        {
        }

        public TimeRange(TimeRange other)
            : base(other)
        {
        }

        public TimeSpan Duration
        {
            get
            {
                return Utility.SafeSubtract(End, Begin);
            }
        }

        public TimeRange Extend(TimeSpan duration)
        {
            return new TimeRange(Utility.SafeSubtract(Begin, duration), Utility.SafeAdd(End, duration));
        }

        public DateTime MiddlePoint
        {
            get
            {
                return new DateTime((Begin.Ticks + End.Ticks) / 2);
            }
        }

        public bool Merge(TimeRange other, TimeSpan gap)
        {
            if (Begin > other.Begin)
            {
                if (Begin > Utility.SafeAdd(other.End, gap))
                {
                    return false;
                }

                Begin = other.Begin;
            }

            if (End < other.End)
            {
                if (Utility.SafeAdd(End, gap) < other.Begin)
                {
                    return false;
                }

                End = other.End;
            }

            return true;
        }

        public void ForceMerge(TimeRange other)
        {
            if (Begin > other.Begin)
            {
                Begin = other.Begin;
            }

            if (End < other.End)
            {
                End = other.End;
            }
        }

        public bool Subtract(TimeRange other, List<TimeRange> result)
        {
            if (other.Begin >= End || other.End <= Begin)
            {
                return false;
            }

            if (other.Begin > Begin)
            {
                result.Add(new TimeRange(Begin, other.Begin));
            }

            if (other.End < End)
            {
                result.Add(new TimeRange(other.End, End));
            }

            return true;
        }

        public override string ToString()
        {
            return Utility.FormatString("{0}, {1}", Utility.FormatTimeWithTicks(Begin), Utility.FormatTimeWithTicks(End));
        }

        public int CompareTo(TimeRange other)
        {
            if (Begin < other.Begin)
            {
                return CompareWithEarlierBeginTime(other);
            }
            else if (Begin > other.Begin)
            {
                return -other.CompareWithEarlierBeginTime(this);
            }
            else
            {
                return End.CompareTo(other.End);
            }
        }

        private int CompareWithEarlierBeginTime(TimeRange other)
        {
            if (End <= other.End)
            {
                return -1;
            }

            TimeSpan dist1 = other.Begin - Begin;
            TimeSpan dist2 = End - other.End;
            return dist2.CompareTo(dist1);
        }

        public static TimeRange Intersect(TimeRange range1, TimeRange range2)
        {
            TimeRange result = new TimeRange(range1);
            if (range1 == TimeRange.Empty || range2 == TimeRange.Empty || !result.Intersect(range2))
            {
                return null;
            }

            return result;
        }
    }

    public class TimeRangeWithClock : TimeRange
    {
        private string beginClockId_;
        private string endClockId_;
        private object beginContext_;
        private object endContext_;

        public TimeRangeWithClock(DateTime begin, DateTime end, string beginClockId, string endClockId)
            : base(begin, end, beginClockId != endClockId)
        {
            beginClockId_ = beginClockId;
            endClockId_ = endClockId;
        }

        public TimeRangeWithClock(DateTime begin, DateTime end, string clockId)
            : this(begin, end, clockId, clockId)
        {
        }

        public TimeRangeWithClock(TimeRange range, string clockId)
            : this(range.Begin, range.End, clockId, clockId)
        {
        }

        public TimeRangeWithClock(TimeRangeWithClock other)
            : base(other.Begin, other.End, other.beginClockId_ != other.endClockId_)
        {
            beginClockId_ = other.beginClockId_;
            endClockId_ = other.endClockId_;
        }

        public string BeginClockId
        {
            get
            {
                return beginClockId_;
            }
        }

        public string EndClockId
        {
            get
            {
                return endClockId_;
            }
        }

        public object BeginContext
        {
            get
            {
                return beginContext_;
            }
            set
            {
                beginContext_ = value;
            }
        }

        public object EndContext
        {
            get
            {
                return endContext_;
            }
            set
            {
                endContext_ = value;
            }
        }

        public bool SameClock
        {
            get
            {
                return beginClockId_ == endClockId_;
            }
        }
    }

    internal class TimeRangeFunc : StandaloneFunc
    {
        public static TimeRangeFunc Singleton = new TimeRangeFunc();

        private TimeRangeFunc() : base("TimeRange")
        {
        }

        public override object Invoke(object[] args)
        {
            if (args == null || args.Length < 1)
            {
                throw new ArgumentException("args");
            }

            TimeRange range = args[0] as TimeRange;
            if (range != null)
            {
                DateTime begin = range.Begin;
                DateTime end = range.End;

                if (args.Length > 1)
                {
                    begin = GetTime(range, begin, args[1]);
                }

                if (args.Length > 2)
                {
                    end = GetTime(range, end, args[2]);
                }

                return new TimeRange(begin, end);
            }
            else
            {
                if (args.Length != 2)
                {
                    throw new ArgumentException("Two arguments are needed");
                }

                return new TimeRange((DateTime) args[0], (DateTime) args[1]);
            }
        }

        private DateTime GetTime(TimeRange range, DateTime original, object arg)
        {
            double delta;

            string stringArg = arg as string;
            if (stringArg != null && stringArg.Length > 1 && stringArg[1] == ':')
            {
                if (stringArg[0] == '0')
                {
                    original = range.Begin;
                }
                else if (stringArg[0] == '1')
                {
                    original = range.End;
                }
                else
                {
                    throw new ArgumentException("Invalid arg: " + arg);
                }

                stringArg = stringArg.Substring(2);
                delta = double.Parse(stringArg);
            }
            else
            {
                delta = Utility.Convert<double>(arg);
            }

            return original + TimeSpan.FromSeconds(delta);
        }
    }
}

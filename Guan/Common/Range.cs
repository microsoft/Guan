using System;

namespace Guan.Common
{
    public class Range<T> where T : IComparable
    {
        private T begin_;
        private T end_;
        private int beginComparand_;
        private int endComparand_;

        public Range(T begin, T end, bool beginInclusive = true, bool endInclusive = true, bool skipCheck = false)
        {
            if (!skipCheck && begin.CompareTo(end) > 0)
            {
                throw new ArgumentException(Utility.FormatString("Invalid range: {0}-{1}", begin, end));
            }

            begin_ = begin;
            end_ = end;
            beginComparand_ = (beginInclusive ? 0 : -1);
            endComparand_ = (endInclusive ? 0 : 1);
        }

        public Range(Range<T> other)
            : this(other.Begin, other.End, other.IsBeginInclusive, other.IsEndInclusive)
        {
        }

        public T Begin
        {
            get
            {
                return begin_;
            }
            protected set
            {
                begin_ = value;
            }
        }

        public T End
        {
            get
            {
                return end_;
            }
            protected set
            {
                end_ = value;
            }
        }

        public bool IsBeginInclusive
        {
            get
            {
                return beginComparand_ == 0;
            }
        }

        public bool IsEndInclusive
        {
            get
            {
                return endComparand_ == 0;
            }
        }

        public bool Contains(T value)
        {
            return (begin_.CompareTo(value) <= beginComparand_ && end_.CompareTo(value) >= endComparand_);
        }

        public bool WeakContains(T value)
        {
            return (begin_.CompareTo(value) <= 0 && end_.CompareTo(value) >= 0);
        }

        public bool StrongContains(T value)
        {
            return (begin_.CompareTo(value) < 0 && end_.CompareTo(value) > 0);
        }

        public bool Contains(Range<T> other)
        {
            int result1 = begin_.CompareTo(other.begin_);
            if ((result1 > 0) || (result1 == 0 && !IsBeginInclusive && other.IsBeginInclusive))
            {
                return false;
            }

            int result2 = end_.CompareTo(other.end_);
            return (result2 > 0) || (result2 == 0 && (IsEndInclusive || !other.IsEndInclusive));
        }

        public bool WeakContains(Range<T> other)
        {
            return (begin_.CompareTo(other.begin_) <= 0 && end_.CompareTo(other.end_) >= 0);
        }

        public bool Overlaps(Range<T> other)
        {
            int result = end_.CompareTo(other.begin_);
            if ((result < 0) || (result == 0 && (endComparand_ != 0 || other.beginComparand_ != 0)))
            {
                return false;
            }

            result = other.end_.CompareTo(begin_);
            return (result > 0 || (result == 0 && other.endComparand_ == 0 && beginComparand_ == 0));
        }

        public bool Merge(Range<T> other)
        {
            int result1 = begin_.CompareTo(other.begin_);
            if (result1 > 0)
            {
                int result2 = begin_.CompareTo(other.end_);
                if ((result2 > 0) || (result2 == 0 && !IsBeginInclusive && !other.IsEndInclusive))
                {
                    return false;
                }

                begin_ = other.begin_;
                beginComparand_ = other.beginComparand_;
            }
            else if (result1 == 0 && !IsBeginInclusive)
            {
                beginComparand_ = other.beginComparand_;
            }

            result1 = end_.CompareTo(other.end_);
            if (result1 < 0)
            {
                int result2 = end_.CompareTo(other.begin_);
                if ((result2 < 0) || (result2 == 0 && !IsEndInclusive && !other.IsBeginInclusive))
                {
                    return false;
                }

                end_ = other.end_;
                endComparand_ = other.endComparand_;
            }
            else if (result1 == 0 && !IsEndInclusive)
            {
                endComparand_ = other.endComparand_;
            }

            return true;
        }

        protected bool Intersect(Range<T> other)
        {
            int result1 = begin_.CompareTo(other.begin_);
            if (result1 < 0)
            {
                int result2 = end_.CompareTo(other.begin_);
                if ((result2 < 0) || (result2 == 0 && !IsBeginInclusive && !other.IsEndInclusive))
                {
                    return false;
                }

                begin_ = other.begin_;
                beginComparand_ = other.beginComparand_;
            }
            else if (result1 == 0 && IsBeginInclusive)
            {
                beginComparand_ = other.beginComparand_;
            }

            result1 = end_.CompareTo(other.end_);
            if (result1 > 0)
            {
                int result2 = begin_.CompareTo(other.end_);
                if ((result2 > 0) || (result2 == 0 && !IsEndInclusive && !other.IsBeginInclusive))
                {
                    return false;
                }

                end_ = other.end_;
                endComparand_ = other.endComparand_;
            }
            else if (result1 == 0 && IsEndInclusive)
            {
                endComparand_ = other.endComparand_;
            }

            return true;
        }

        public static bool TryParse(string value, out Range<T> result)
        {
            int index = value.IndexOf('-');
            if (index < 0)
            {
                T start;
                if (Utility.TryParse<T>(value, out start))
                {
                    result = new Range<T>(start, start);
                    return true;
                }
            }
            else
            {
                T start, end;
                if (Utility.TryParse(value.Substring(0, index - 1).Trim(), Utility.MinValue<T>(), out start) &&
                    Utility.TryParse(value.Substring(index + 1), Utility.MaxValue<T>(), out end))
                {
                    result = new Range<T>(start, end);
                    return true;
                }
            }

            result = null;
            return false;
        }

        public override bool Equals(object obj)
        {
            Range<T> other = obj as Range<T>;
            if (other == null)
            {
                return false;
            }

            return (begin_.CompareTo(other.begin_) == 0 && end_.CompareTo(other.end_) == 0 && beginComparand_ == other.beginComparand_ && endComparand_ == other.endComparand_);
        }

        public override int GetHashCode()
        {
            return begin_.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}{1}-{2}{3}", IsBeginInclusive ? '[' : '(', begin_, end_, IsEndInclusive ? ']' : ')');
        }
    }
}

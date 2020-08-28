// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Guan.Common
{
    public class GuanTime : IPartialOrder
    {
        private DateTime time_;
        private string clockId_;
        private object context_;
        private IComparable contextLocation_;

        public static readonly GuanTime MinValue = new GuanTime(DateTime.MinValue);
        public static readonly GuanTime MaxValue = new GuanTime(DateTime.MaxValue);

        public GuanTime(DateTime time)
        {
            time_ = time;
        }

        public GuanTime(DateTime time, string clockId) : this(time, clockId, null, null)
        {
        }

        public GuanTime(DateTime time, string clockId, object context, IComparable contextLocation)
        {
            time_ = time;
            if (time != DateTime.MaxValue && time != DateTime.MinValue)
            {
                clockId_ = clockId;
                context_ = context;
                contextLocation_ = contextLocation;
            }
        }

        public DateTime Time
        {
            get
            {
                return time_;
            }
        }

        public string ClockId
        {
            get
            {
                return clockId_;
            }
        }

        public object Context
        {
            get
            {
                return context_;
            }
        }

        public bool CompareTo(GuanTime other, out int result)
        {
            if (context_ == null || other.context_ == null)
            {
                result = time_.CompareTo(other.time_);
                if (clockId_ != other.clockId_ && !string.IsNullOrEmpty(clockId_) && !string.IsNullOrEmpty(other.clockId_))
                {
                    return false;
                }

                if (result == 0)
                {
                    if (context_ != null)
                    {
                        result = 1;
                    }
                    else if (other.context_ != null)
                    {
                        result = -1;
                    }
                }
            }
            else
            {
                if (!object.Equals(context_, other.context_))
                {
                    result = 0;
                    return false;
                }
                result = contextLocation_.CompareTo(other.contextLocation_);
            }

            return true;
        }

        public int CompareTo(DateTime other)
        {
            int result = time_.CompareTo(other);
            if (result == 0 && context_ != null)
            {
                result = 1;
            }

            return result;
        }

        public bool CompareTo(object other, out int result)
        {
            GuanTime otherGuanTime = other as GuanTime;
            if (otherGuanTime != null)
            {
                return CompareTo(otherGuanTime, out result);
            }

            if (other is DateTime)
            {
                result = CompareTo((DateTime)other);
                return true;
            }

            result = 0;
            return false;
        }

        public override string ToString()
        {
            return Utility.FormatTime(time_);
        }

        public static explicit operator GuanTime(DateTime value)
        {
            return new GuanTime(value);
        }

        public static explicit operator DateTime(GuanTime value)
        {
            return value.time_;
        }

        public static GuanTime Convert(object value)
        {
            if (value == null)
            {
                return null;
            }

            GuanTime result = value as GuanTime;
            if (result != null)
            {
                return result;
            }

            DateTime time = (DateTime)value;
            if (time == DateTime.MinValue)
            {
                return GuanTime.MinValue;
            }
            if (time == DateTime.MaxValue)
            {
                return GuanTime.MaxValue;
            }

            return new GuanTime(time);
        }
    }
}

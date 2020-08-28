///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  
//      
// @File: AddFunc.cs
//
// @Owner: xunlu   
// @Test:  xunlu
//
// Purpose:
//   Function to add integer or concatenate string.
//   
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace Guan.Common
{
    internal class AddFunc : StandaloneFunc
    {
        public static readonly AddFunc Singleton = new AddFunc();

        private MathsFunc mathsAdd_;

        private AddFunc()
            : base("add")
        {
            mathsAdd_ = new MathsFunc("add", '+');
        }

        public override object Invoke(object[] args)
        {
            if (args.Length == 2)
            {
                if (args[0] is DateTime)
                {
                    return AddDateTime((DateTime)args[0], args[1]);
                }
                if (args[0] is GuanTime)
                {
                    return AddGuanTime((GuanTime)args[0], args[1]);
                }

                object result = mathsAdd_.Invoke(args);
                if (result != null)
                {
                    return result;
                }
            }

            string s = "";
            foreach (object arg in args)
            {
                if (arg != null)
                {
                    s += arg.ToString();
                }
            }

            return s;
        }

        private DateTime AddDateTime(DateTime arg1, object arg2)
        {
            if (arg2 is TimeSpan)
            {
                return arg1 + (TimeSpan)arg2;
            }

            if (arg2 is string)
            {
                return arg1 + TimeSpan.Parse((string)arg2);
            }

            throw new ArgumentException("Invalid argument to add to DateTime: " + arg2);
        }

        private GuanTime AddGuanTime(GuanTime arg1, object arg2)
        {
            DateTime result = AddDateTime(arg1.Time, arg2);
            return new GuanTime(result);
        }
    }
}

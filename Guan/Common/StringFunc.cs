///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  
//      
// @File: StrFunc.cs
//
// @Owner: xunlu   
// @Test:  xunlu
//
// Purpose:
//   Function to convert object to literal string with quotation marks.
//   
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Guan.Common
{
    internal class StringFunc : UnaryFunc
    {
        public static readonly StringFunc Singleton = new StringFunc();

        private StringFunc()
            : base("string")
        {
        }

        public override object UnaryInvoke(object arg)
        {
            return (arg != null ? arg.ToString() : string.Empty);
        }
    }

    internal class StrFunc : UnaryFunc
    {
        public static readonly StrFunc Singleton = new StrFunc();

        private StrFunc()
            : base("str")
        {
        }

        public override object UnaryInvoke(object arg)
        {
            string result = (arg != null ? arg.ToString() : string.Empty);

            return "\"" + result + "\"";
        }
    }

    internal class TrimFunc : UnaryFunc
    {
        public static readonly TrimFunc Singleton = new TrimFunc();

        private TrimFunc()
            : base("Trim")
        {
        }

        public override object UnaryInvoke(object arg)
        {
            string result = (arg != null ? arg.ToString() : string.Empty);

            return result.Trim();
        }
    }

    internal class SubStrFunc : StandaloneFunc
    {
        public static readonly SubStrFunc Singleton = new SubStrFunc();

        private SubStrFunc()
            : base("substr")
        {
        }

        public override object Invoke(object[] args)
        {
            if (args.Length == 0)
            {
                return string.Empty;
            }

            string result = args[0] as string;
            if (result == null)
            {
                return string.Empty;
            }

            int start = 0;
            int end = result.Length;
            if (args.Length > 1 && args[1] != null)
            {
                start = result.IndexOf((string)args[1]) + 1;
            }
            if (args.Length > 2)
            {
                end = result.IndexOf((string)args[2]);
                if (end < 0)
                {
                    end = result.Length;
                }
            }

            return result.Substring(start, end - start);
        }
    }
}

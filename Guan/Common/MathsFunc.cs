// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Guan.Common
{
    internal class MathsFunc : StandaloneFunc
    {
        private char op_;

        public static MathsFunc Minus = new MathsFunc("minus", '-');
        public static MathsFunc Multiply = new MathsFunc("multiply", '*');
        public static MathsFunc Divide = new MathsFunc("divide", '/');
        public static MathsFunc Mod = new MathsFunc("mod", '%');

        public MathsFunc(string name, char op) 
            : base(name)
        {
            this.op_ = op;
        }

        public override object Invoke(object[] args)
        {
            if (args.Length == 1 && this == Minus)
            {
                return UnaryMinus(args[0]);
            }

            if (args.Length != 2)
            {
                throw new ArgumentException("Invalid number of args");
            }

            object result = Calculate(args[0], args[1]);
            ReleaseAssert.IsTrue(result != null, "Incompatible type for arithemetic operation: {0}, {1}", args[0], args[1]);
            return result;
        }

        private object UnaryMinus(object arg)
        {
            Type type = arg.GetType();
            if (type == typeof(int))
            {
                int value = (int)arg;
                return -value;
            }
            if (type == typeof(long))
            {
                long value = (long)arg;
                return -value;
            }
            if (type == typeof(ulong))
            {
                ulong value = (ulong)arg;
                return -((long)value);
            }
            if (type == typeof(TimeSpan))
            {
                TimeSpan value = (TimeSpan)arg;
                return -value;
            }

            throw new GuanException("Invalid type for unary minus function: ", type);
        }

        private object Calculate(object arg1, object arg2)
        {
            if (arg1 == null || arg2 == null)
            {
                return null;
            }

            Type type = arg1.GetType();
            if (type != arg2.GetType())
            {
                return null;
            }

            if (type == typeof(long))
            {
                long v1 = (long)arg1;
                long v2 = (long)arg2;
                switch (op_)
                {
                    case '+':
                        return v1 + v2;
                    case '-':
                        return v1 - v2;
                    case '*':
                        return v1 * v2;
                    case '/':
                        return v1 / v2;
                    case '%':
                        return v1 % v2;
                }
            }
            else if (type == typeof(ulong))
            {
                ulong v1 = (ulong)arg1;
                ulong v2 = (ulong)arg2;
                switch (op_)
                {
                    case '+':
                        return v1 + v2;
                    case '-':
                        return v1 - v2;
                    case '*':
                        return v1 * v2;
                    case '/':
                        return v1 / v2;
                    case '%':
                        return v1 % v2;
                }
            }
            else if (type == typeof(int))
            {
                int v1 = (int)arg1;
                int v2 = (int)arg2;
                switch (op_)
                {
                    case '+':
                        return v1 + v2;
                    case '-':
                        return v1 - v2;
                    case '*':
                        return v1 * v2;
                    case '/':
                        return v1 / v2;
                    case '%':
                        return v1 % v2;
                }
            }
            else if (type == typeof(double))
            {
                double v1 = (double)arg1;
                double v2 = (double)arg2;
                switch (op_)
                {
                    case '+':
                        return v1 + v2;
                    case '-':
                        return v1 - v2;
                    case '*':
                        return v1 * v2;
                    case '/':
                        return v1 / v2;
                }
            }

            return null;
        }
    }
}

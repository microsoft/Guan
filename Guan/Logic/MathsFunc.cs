// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;

    internal class MathsFunc : StandaloneFunc
    {
        private static MathsFunc minus = new MathsFunc("minus", '-');
        private static MathsFunc multiply = new MathsFunc("multiply", '*');
        private static MathsFunc divide = new MathsFunc("divide", '/');
        private static MathsFunc mod = new MathsFunc("mod", '%');

        private char op;

        public MathsFunc(string name, char op)
            : base(name)
        {
            this.op = op;
        }

        public static MathsFunc Minus
        {
            get
            {
                return minus;
            }
        }

        public static MathsFunc Multiply
        {
            get
            {
                return multiply;
            }
        }

        public static MathsFunc Divide
        {
            get
            {
                return divide;
            }
        }

        public static MathsFunc Mod
        {
            get
            {
                return mod;
            }
        }

        public override object Invoke(object[] args)
        {
            if (args.Length == 1 && this == Minus)
            {
                return this.UnaryMinus(args[0]);
            }

            if (args.Length != 2)
            {
                throw new ArgumentException("Invalid number of args");
            }

            object result = this.Calculate(args[0], args[1]);
            ReleaseAssert.IsTrue(result != null, "Incompatible type for arithemetic operation: {0}, {1}", args[0], args[1]);
            return result;
        }

        internal object TryInvoke(object[] args)
        {
            if (args.Length == 0)
            {
                return null;
            }

            Type type = args[0].GetType();
            if (type != typeof(int) && type != typeof(long) && type != typeof(ulong) && type != typeof(double))
            {
                return null;
            }

            return this.Invoke(args);
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

            if (type == typeof(double))
            {
                double value = (double)arg;
                return -((double)value);
            }

            if (type == typeof(TimeSpan))
            {
                TimeSpan value = (TimeSpan)arg;
                return -((TimeSpan)value);
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
                switch (this.op)
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
                switch (this.op)
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
                switch (this.op)
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
                switch (this.op)
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
            else if (type == typeof(TimeSpan))
            {
                TimeSpan v1 = (TimeSpan)arg1;
                TimeSpan v2 = (TimeSpan)arg2;
                switch (this.op)
                {
                    case '+':
                        return v1 + v2;
                    case '-':
                        return v1 - v2;
                }
            }
            else if (type == typeof(DateTime))
            {
                DateTime v1 = (DateTime)arg1;
                DateTime v2 = (DateTime)arg2;
                switch (this.op)
                {
                    case '-':
                        return v1 - v2;
                }
            }

            return null;
        }
    }
}

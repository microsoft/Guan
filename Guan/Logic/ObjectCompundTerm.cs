using System;
using System.Reflection;
using System.Globalization;
using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// Adapater to expose object properties as a compund term using reflection.
    /// </summary>
    internal class ObjectCompundTerm : CompoundTerm
    {
        private object value_;
        private Type type_;

        private ObjectCompundTerm(object value)
            : base(Functor.ClassObject)
        {
            value_ = value;
            type_ = value.GetType();
        }

        public Type ObjectType
        {
            get
            {
                return type_;
            }
        }

        public override Term GetArgument(string name)
        {
            if (type_.GetProperty(name) == null)
            {
                return null;
            }

            object result = type_.InvokeMember(name, BindingFlags.GetProperty, null, value_, null, CultureInfo.InvariantCulture);
            GuanObject guanObject = result as GuanObject;
            if (guanObject != null && guanObject.GetValue() != null)
            {
                result = guanObject.GetValue();
            }

            return Term.FromObject(result);
        }

        public static ObjectCompundTerm Create(object value)
        {
            if (value == null || value.GetType().IsPrimitive)
            {
                return null;
            }

            return new ObjectCompundTerm(value);
        }

        public override string ToString()
        {
            return value_.ToString();
        }
    }
}

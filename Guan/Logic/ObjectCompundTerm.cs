// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Text.Json;

    /// <summary>
    /// Adapater to expose object properties as a compund term using reflection.
    /// </summary>
    internal class ObjectCompundTerm : CompoundTerm
    {
        private static readonly Type[] EmptyTypes = new Type[0];

        private object value;
        private Type type;

        public ObjectCompundTerm(object value, bool ignoreType = false)
            : base(new Functor(ignoreType ? typeof(ObjectCompundTerm) : value.GetType()))
        {
            this.value = value;
            this.type = value.GetType();
        }

        public Type ObjectType
        {
            get
            {
                return this.type;
            }
        }

        public object Value
        {
            get
            {
                return this.value;
            }
        }

        public static ObjectCompundTerm Create(object value)
        {
            if (value == null || value.GetType().IsPrimitive)
            {
                return null;
            }

            return new ObjectCompundTerm(value);
        }

        public override Term GetExtendedArgument(string name)
        {
            object result;
            IPropertyContext context = this.value as IPropertyContext;
            if (context != null)
            {
                result = context[name];
                if (result == null)
                {
                    return null;
                }
            }
            else
            {
                if (this.type.GetProperty(name) == null)
                {
                    return null;
                }

                result = this.type.InvokeMember(name, BindingFlags.GetProperty, null, this.value, null, CultureInfo.InvariantCulture);
            }

            GuanObject guanObject = result as GuanObject;
            if (guanObject != null && guanObject.GetValue() != null)
            {
                result = guanObject.GetValue();
            }

            return Term.FromObject(result);
        }

        public override string ToString()
        {
            if (this.value == null)
            {
                return "null";
            }

            return JsonSerializer.Serialize(this.value, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}

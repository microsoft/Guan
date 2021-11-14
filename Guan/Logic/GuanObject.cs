//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    public class GuanObject : IPropertyContext, IWritablePropertyContext
    {
        private static readonly char[] PathDelimit = new char[] { '/' };
        private static readonly List<string> EmptyList = new List<string>();

        private object value;
        private GuanObject parent;
        private SortedDictionary<string, GuanObject> children;
        private Dictionary<string, string> aliases;

        public GuanObject()
        {
            this.children = new SortedDictionary<string, GuanObject>();
        }

        public GuanObject(GuanObject other, GuanObject parent = null)
        {
            this.parent = parent;
            this.aliases = other.aliases;
            this.children = new SortedDictionary<string, GuanObject>();
            this.CopyFrom(other);
        }

        public SortedDictionary<string, GuanObject> Children
        {
            get
            {
                return this.children;
            }
        }

        protected Dictionary<string, string> Aliases
        {
            get
            {
                return this.aliases;
            }

            set
            {
                this.aliases = value;
            }
        }

        public virtual object this[string name]
        {
            get
            {
                string path = name;
                GuanObject obj = this.GetNode(ref path, false);
                if (obj == null)
                {
                    if (this.aliases != null && this.aliases.TryGetValue(name, out path))
                    {
                        obj = this.GetNode(ref path, false);
                    }

                    if (obj == null)
                    {
                        return null;
                    }
                }

                return obj.GetValue(path);
            }

            set
            {
                this.SetValue(name, value, null);
            }
        }

        public IEnumerator GetEnumerator()
        {
            return this.children.GetEnumerator();
        }

        public void CopyFrom(GuanObject other)
        {
            this.value = other.value;
            this.CopyChildren(other);
        }

        public void CopyChildren(GuanObject other, Dictionary<string, string> children = null, bool overwrite = true)
        {
            foreach (KeyValuePair<string, GuanObject> entry in other.children)
            {
                if (children == null)
                {
                    if (overwrite || !this.children.ContainsKey(entry.Key))
                    {
                        this.children[entry.Key] = new GuanObject(entry.Value, this);
                    }
                }
                else
                {
                    string newKey;
                    if (children.TryGetValue(entry.Key, out newKey))
                    {
                        this.children.Add(newKey, new GuanObject(entry.Value, this));
                    }
                }
            }
        }

        public GuanObject GetObject(string name)
        {
            string path = name;
            return this.GetNode(ref path, false);
        }

        public bool ContainsKey(string key)
        {
            GuanObject obj = this.GetNode(ref key, false);
            return (obj != null);
        }

        public T GetValue<T>(string name)
        {
            object value = this[name];
            return Utility.Convert<T>(value);
        }

        public virtual void SetValue(string name, object value, string operation)
        {
            if (operation == "delete")
            {
                _ = this.Delete(name);
                return;
            }

            string path = name;
            GuanObject obj = this.GetNode(ref path, true);
            GuanObject child = value as GuanObject;
            if (operation == "Children")
            {
                if (child != null)
                {
                    obj.CopyChildren(child);
                }
            }
            else if (child == null)
            {
                obj.SetValue(value, operation);
            }
            else
            {
                foreach (KeyValuePair<string, GuanObject> entry in obj.parent.children)
                {
                    if (entry.Value == obj)
                    {
                        obj.parent.children[entry.Key] = child;
                        child.parent = obj.parent;
                        return;
                    }
                }

                ReleaseAssert.Fail("Child not found");
            }
        }

        public void AddKey(string name)
        {
            this.SetValue(name, null, null);
        }

        public bool Delete(string name)
        {
            string path = name;
            GuanObject obj = this.GetNode(ref path, false);
            if (obj == null)
            {
                return false;
            }

            if (path.Length != 0)
            {
                throw new ArgumentException("name");
            }

            return obj.parent.children.Remove(this.GetLeafName(name));
        }

        public object GetValue()
        {
            return this.value;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            if (this.children.Count > 0)
            {
                _ = result.Append("{");
            }

            if (this.value != null)
            {
                IEnumerable valueCollection = this.value as IEnumerable;
                if (valueCollection != null && this.value.GetType() != typeof(string))
                {
                    _ = result.Append("(");
                    int i = 0;
                    foreach (object entry in valueCollection)
                    {
                        if (entry != null)
                        {
                            if (i > 0)
                            {
                                _ = result.Append(",");
                            }

                            _ = result.Append(entry.ToString());
                            i++;
                        }
                    }

                    _ = result.Append(")");
                }
                else
                {
                    _ = result.Append(this.value);
                }
            }

            if (this.children.Count > 0)
            {
                if (this.children.Count > 12)
                {
                    if (result.Length > 1)
                    {
                        _ = result.Append(",");
                    }

                    _ = result.AppendFormat("#Count:{0}", this.children.Count);
                }
                else
                {
                    foreach (KeyValuePair<string, GuanObject> entry in this.children)
                    {
                        if (!entry.Key.StartsWith("_"))
                        {
                            if (result.Length > 1)
                            {
                                _ = result.Append(",");
                            }

                            _ = result.AppendFormat("{0}:{1}", entry.Key, entry.Value);
                        }
                    }
                }

                if (result.Length > 1)
                {
                    _ = result.Append("}");
                }
                else
                {
                    result.Length = 0;
                }
            }

            return result.ToString();
        }

        private string GetLeafName(string name)
        {
            int index = name.LastIndexOf('/');
            if (index < 0)
            {
                return name;
            }

            return name.Substring(index + 1);
        }

        private object GetValue(string name)
        {
            if (name == "#Count")
            {
                return this.children.Count;
            }

            if (this.value != null)
            {
                return this.value;
            }

            return this;
        }

        private void SetValue(object value, string operation)
        {
            if (operation == null)
            {
                this.value = value;
            }
            else if (value != null)
            {
                if (operation == "+")
                {
                    this.Add(value);
                }
                else if (operation == "count")
                {
                    if (value != null)
                    {
                        this.Count(value);
                    }
                }
                else
                {
                    ReleaseAssert.IsTrue(operation == "Create", "Unknown operation: " + operation);
                }
            }
        }

        private void Count(object value)
        {
            string key = value.ToString();
            GuanObject obj = this.GetNode(ref key, true);
            obj.Add(1);
        }

        private GuanObject GetNode(ref string name, bool create)
        {
            if (string.IsNullOrEmpty(name))
            {
                return this;
            }

            GuanObject result = this;
            string[] path = name.Split(PathDelimit, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] == "#append")
                {
                    path[i] = result.children.Count.ToString();
                }

                if (path[i].StartsWith("#", StringComparison.InvariantCulture))
                {
                    if (i < path.Length - 1)
                    {
                        throw new ArgumentException("Invalid name: " + name);
                    }

                    name = path[i];
                    return result;
                }

                GuanObject child;
                if (!result.children.TryGetValue(path[i], out child))
                {
                    if (!create)
                    {
                        return null;
                    }

                    child = new GuanObject();
                    child.parent = result;

                    result.children.Add(path[i], child);
                }

                result = child;
            }

            name = string.Empty;
            return result;
        }

        private void Add(object value)
        {
            this.value = (Utility.Convert<long>(this.value) + Utility.Convert<long>(value)).ToString();
        }

        private class EncodePathFunc : UnaryFunc
        {
            public static readonly EncodePathFunc Singleton = new EncodePathFunc();

            private EncodePathFunc()
                : base("EncodePath")
            {
            }

            public override object UnaryInvoke(object arg)
            {
                string input = (string)arg;
                return input.Replace("/", "%2F");
            }
        }

        private class DecodePathFunc : UnaryFunc
        {
            public static readonly DecodePathFunc Singleton = new DecodePathFunc();

            private DecodePathFunc()
                : base("DecodePath")
            {
            }

            public override object UnaryInvoke(object arg)
            {
                string input = (string)arg;
                return input.Replace("%2F", "/");
            }
        }

        private class ListObjFunc : GuanFunc
        {
            public static readonly ListObjFunc Singleton = new ListObjFunc();

            private ListObjFunc()
                : base("ListObj")
            {
            }

            public override object Invoke(IPropertyContext context, object[] args)
            {
                if (args.Length != 1 && args.Length != 2)
                {
                    throw new ArgumentException("Need 1 or 2 arguments");
                }

                GuanObject result = new GuanObject();
                IEnumerable arg1 = this.ConvertArg(args[0]);
                if (arg1 == null)
                {
                    throw new ArgumentException("Invalid argument in ListObj");
                }

                if (args.Length == 1)
                {
                    foreach (object obj in arg1)
                    {
                        result["#append"] = obj;
                    }
                }
                else
                {
                    IEnumerable arg2 = this.ConvertArg(args[1]);
                    if (arg1 == null)
                    {
                        throw new ArgumentException("Invalid argument ListObj");
                    }

                    IEnumerator enumerator1 = arg1.GetEnumerator();
                    IEnumerator enumerator2 = arg2.GetEnumerator();

                    while (enumerator1.MoveNext() && enumerator2.MoveNext())
                    {
                        if (enumerator1.Current != null)
                        {
                            result[enumerator1.Current.ToString()] = enumerator2.Current;
                        }
                    }
                }

                return result;
            }

            private IEnumerable ConvertArg(object arg)
            {
                if (arg is string)
                {
                    return null;
                }

                return arg as IEnumerable;
            }
        }
    }
}

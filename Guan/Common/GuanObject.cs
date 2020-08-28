using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Guan.Common
{
    public interface IGuanObject
    {
        GuanObject ToGuanObject();
    }

    public interface IGuanObjectTransformer
    {
        List<GuanObject> Transform(GuanObject original);

        void Apply(GuanObject original, GuanObject target);
    }

    class ListObjFunc : GuanFunc
    {
        public static ListObjFunc Singleton = new ListObjFunc();

        private ListObjFunc() : base("ListObj")
        {
        }

        public override object Invoke(IPropertyContext context, object[] args)
        {
            if (args.Length != 1 && args.Length != 2)
            {
                throw new ArgumentException("Need 1 or 2 arguments");
            }

            GuanObject result = new GuanObject();
            IEnumerable arg1 = ConvertArg(args[0]);
            if (arg1 == null)
            {
                throw new ArgumentException("Invalid argument");
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
                IEnumerable arg2 = ConvertArg(args[1]);
                if (arg1 == null)
                {
                    throw new ArgumentException("Invalid argument");
                }

                IEnumerator enumerator1 = arg1.GetEnumerator();
                IEnumerator enumerator2 = arg2.GetEnumerator();
                
                while (enumerator1.MoveNext() && enumerator2.MoveNext())
                {
                    result[enumerator1.Current.ToString()] = enumerator2.Current;
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

    public class GuanObject : IPropertyContext, IWritablePropertyContext, IEnumerable
    {
        private object value_;
        private GuanObject parent_;
        private SortedDictionary<string, GuanObject> children_;
        private Dictionary<string, string> aliases_;

        private static readonly char[] PathDelimit = new char[] { '/' };
        private static readonly List<string> EmptyList = new List<string>();

        class EncodePathFunc : UnaryFunc
        {
            public static EncodePathFunc Singleton = new EncodePathFunc();

            private EncodePathFunc() : base("EncodePath")
            {
            }

            public override object UnaryInvoke(object arg)
            {
                string input = (string)arg;
                return input.Replace("/", "%2F");
            }
        }

        class DecodePathFunc : UnaryFunc
        {
            public static DecodePathFunc Singleton = new DecodePathFunc();

            private DecodePathFunc() : base("DecodePath")
            {
            }

            public override object UnaryInvoke(object arg)
            {
                string input = (string)arg;
                return input.Replace("%2F", "/");
            }
        }

        public GuanObject()
        {
            children_ = new SortedDictionary<string, GuanObject>();
        }

        public GuanObject(GuanObject other, GuanObject parent = null)
        {
            parent_ = parent;
            aliases_ = other.aliases_;
            children_ = new SortedDictionary<string, GuanObject>();
            CopyFrom(other);
        }

        protected Dictionary<string, string> Aliases
        {
            get
            {
                return aliases_;
            }
            set
            {
                aliases_ = value;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return children_.GetEnumerator();
        }

        public SortedDictionary<string, GuanObject> Children
        {
            get
            {
                return children_;
            }
        }

        public virtual void CopyFrom(GuanObject other)
        {
            value_ = other.value_;
            CopyChildren(other);
        }

        public void CopyChildren(GuanObject other, Dictionary<string, string> children = null, bool overwrite = true)
        {
            foreach (KeyValuePair<string, GuanObject> entry in other.children_)
            {
                if (children == null)
                {
                    if (overwrite || !children_.ContainsKey(entry.Key))
                    {
                        children_[entry.Key] = new GuanObject(entry.Value, this);
                    }
                }
                else
                {
                    string newKey;
                    if (children.TryGetValue(entry.Key, out newKey))
                    {
                        children_.Add(newKey, new GuanObject(entry.Value, this));
                    }
                }
            }
        }

        public virtual object this[string name]
        {
            get
            {
                string path = name;
                GuanObject obj = GetNode(ref path, false);
                if (obj == null)
                {
                    if (aliases_ != null && aliases_.TryGetValue(name, out path))
                    {
                        obj = GetNode(ref path, false);
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
                SetValue(name, value, null);
            }
        }

        public GuanObject GetObject(string name)
        {
            string path = name;
            return GetNode(ref path, false);
        }

        public bool ContainsKey(string key)
        {
            GuanObject obj = GetNode(ref key, false);
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
                Delete(name);
                return;
            }

            string path = name;
            GuanObject obj = GetNode(ref path, true);
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
                foreach (var entry in obj.parent_.children_)
                {
                    if (entry.Value == obj)
                    {
                        obj.parent_.children_[entry.Key] = child;
                        child.parent_ = obj.parent_;
                        return;
                    }
                }

                ReleaseAssert.Fail("Child not found");
            }
        }

        public void AddKey(string name)
        {
            SetValue(name, null, null);
        }

        public bool Delete(string name)
        {
            string path = name;
            GuanObject obj = GetNode(ref path, false);
            if (obj == null)
            {
                return false;
            }

            if (path.Length != 0)
            {
                throw new ArgumentException("name");
            }

            return obj.parent_.children_.Remove(GetLeafName(name));
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
                return children_.Count;
            }

            if (value_ != null)
            {
                return value_;
            }

            return this;
        }

        public object GetValue()
        {
            return value_;
        }

        private void SetValue(object value, string operation)
        {
            if (operation == null)
            {
                value_ = value;
            }
            else if (value != null)
            {
                if (operation == "+")
                {
                    Add(value);
                }
                else if (operation == "count")
                {
                    if (value != null)
                    {
                        Count(value);
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
            GuanObject obj = GetNode(ref key, true);
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
                    path[i] = result.children_.Count.ToString();
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
                if (!result.children_.TryGetValue(path[i], out child))
                {
                    if (!create)
                    {
                        return null;
                    }

                    child = new GuanObject();
                    child.parent_ = result;

                    result.children_.Add(path[i], child); 
                }

                result = child;
            }

            name = string.Empty;
            return result;
        }

        private void Add(object value)
        {
            value_ = (Utility.Convert<long>(value_) + Utility.Convert<long>(value)).ToString();
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            if (children_.Count > 0)
            {
                result.Append("{");
            }

            if (value_ != null)
            {
                IEnumerable valueCollection = value_ as IEnumerable;
                if (valueCollection != null && value_.GetType() != typeof(string))
                {
                    result.Append("(");
                    int i = 0;
                    foreach (var entry in valueCollection)
                    {
                        if (i > 0)
                        {
                            result.Append(",");
                        }
                        result.Append(entry.ToString());
                        i++;
                    }
                    result.Append(")");
                }
                else
                {
                    result.Append(value_);
                }
            }

            if (children_.Count > 0)
            {
                if (children_.Count > 12)
                {
                    if (result.Length > 1)
                    {
                        result.Append(",");
                    }
                    result.AppendFormat("#Count:{0}", children_.Count);
                }
                else
                {
                    foreach (var entry in children_)
                    {
                        if (!entry.Key.StartsWith("_"))
                        {
                            if (result.Length > 1)
                            {
                                result.Append(",");
                            }
                            result.AppendFormat("{0}:{1}", entry.Key, entry.Value);
                        }
                    }
                }

                if (result.Length > 1)
                {
                    result.Append("}");
                }
                else
                {
                    result.Length = 0;
                }
            }

            return result.ToString();
        }
    }
}

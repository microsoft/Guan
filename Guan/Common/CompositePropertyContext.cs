using System.Collections.Generic;

namespace Guan.Common
{
    public class CompositePropertyContext : IPropertyContext
    {
        private List<IPropertyContext> contexts_;

        public CompositePropertyContext(IPropertyContext context, params IPropertyContext[] extraContexts)
        {
            contexts_ = new List<IPropertyContext>(extraContexts.Length + 1)
            {
                context
            };
            contexts_.AddRange(extraContexts);
        }

        public void SetContext(IPropertyContext context, int index = 0)
        {
            while (contexts_.Count < index)
            {
                contexts_.Add(null);
            }

            if (index == contexts_.Count)
            {
                contexts_.Add(context);
            }
            else
            {
                contexts_[index] = context;
            }
        }

        public virtual object this[string name]
        {
            get
            {
                int start, end;
                if (name.Length > 3 && name[0] == '#' && name[2] == ':' && name[1] >= '0' && name[1] <= '9')
                {
                    start = name[1] - '0';
                    end = start + 1;
                    name = name.Substring(3);
                }
                else
                {
                    start = 0;
                    end = contexts_.Count;
                }

                for (int i = start; i < end; i++)
                {
                    object result = contexts_[i][name];
                    if (result != null)
                    {
                        return result;
                    }
                }

                return null;
            }
        }
    }
}

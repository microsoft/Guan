//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System.Collections.Generic;

    public class CompositePropertyContext : IPropertyContext
    {
        private List<IPropertyContext> contexts;

        public CompositePropertyContext(IPropertyContext context, params IPropertyContext[] extraContexts)
        {
            this.contexts = new List<IPropertyContext>(extraContexts.Length + 1);
            this.contexts.Add(context);
            this.contexts.AddRange(extraContexts);
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
                    end = this.contexts.Count;
                }

                for (int i = start; i < end; i++)
                {
                    object result = this.contexts[i][name];
                    if (result != null)
                    {
                        return result;
                    }
                }

                return null;
            }
        }

        public void SetContext(IPropertyContext context, int index = 0)
        {
            while (this.contexts.Count < index)
            {
                this.contexts.Add(null);
            }

            if (index == this.contexts.Count)
            {
                this.contexts.Add(context);
            }
            else
            {
                this.contexts[index] = context;
            }
        }
    }
}

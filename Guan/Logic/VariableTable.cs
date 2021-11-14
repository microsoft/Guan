//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Collection of variables in a rule.
    /// </summary>
    internal class VariableTable : List<string>
    {
        public static readonly VariableTable Empty = new VariableTable();

        public VariableTable()
        {
        }

        public int GetIndex(string name, bool create)
        {
            if (name != "_")
            {
                for (int i = 0; i < this.Count; i++)
                {
                    if (this[i] == name)
                    {
                        return i;
                    }
                }
            }

            if (!create)
            {
                return -1;
            }

            this.Add(name);
            return this.Count - 1;
        }
    }
}

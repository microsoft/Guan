using System.Collections.Generic;

namespace Guan.Logic
{
    /// <summary>
    /// Collection of variables in a rule.
    /// </summary>
    public class VariableTable : List<string>
    {
        public static readonly VariableTable Empty = new VariableTable();

        public VariableTable()
        {
        }

        public int GetIndex(string name, bool create)
        {
            if (name != "_")
            {
                for (int i = 0; i < Count; i++)
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

            Add(name);
            return Count - 1;
        }
    }
}

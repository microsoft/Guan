namespace Guan.Logic
{
    /// <summary>
    /// Variable term used in rules. At runtime they will be converted to Variable objects.
    /// </summary>
    public class IndexedVariable : Term
    {
        private int index_;
        private string name_;

        public IndexedVariable(int index, string name)
        {
            index_ = index;
            name_ = name;
        }

        public int Index
        {
            get
            {
                return index_;
            }
        }

        public string Name
        {
            get
            {
                return name_;
            }
        }

        public override bool IsGround()
        {
            return false;
        }

        public override string ToString()
        {
            return "?" + name_ + "_" + index_.ToString();
        }
    }
}

namespace Guan.Logic
{
    /// <summary>
    /// Argument of a compound term.
    /// </summary>
    public class TermArgument
    {
        private string name_;
        private ArgumentDescription desc_;
        private Term value_;

        public TermArgument(string name, Term value, ArgumentDescription desc = null)
        {
            name_ = name;
            value_ = value;
            desc_ = desc;
        }

        public string Name
        {
            get
            {
                return name_;
            }
        }

        public ArgumentDescription Description
        {
            get
            {
                return desc_;
            }
        }

        public Term Value
        {
            get
            {
                return value_;
            }
            set
            {
                value_ = value;
            }
        }

        public override string ToString()
        {
            return Name + ":" + value_.ToString();
        }
    }
}

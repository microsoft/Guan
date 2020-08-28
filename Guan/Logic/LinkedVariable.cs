namespace Guan.Logic
{
    /// <summary>
    /// A variable bound to another variable.
    /// </summary>
    public class LinkedVariable : Variable
    {
        private Variable original_;

        public LinkedVariable(VariableBinding binding, Variable original, string name)
            : base(name, binding)
        {
            original_ = original;
        }

        public LinkedVariable(LinkedVariable other, VariableBinding binding)
            : base(other, binding)
        {
            original_ = other.original_;
        }

        public Variable Original
        {
            get
            {
                return original_;
            }
            set
            {
                original_ = value;
            }
        }
    }

    /// <summary>
    /// Variable output from a goal to the rule.
    /// </summary>
    public class OutputVariable : LinkedVariable
    {
        public OutputVariable(VariableBinding binding, Variable original, string name)
            : base(binding, original, name)
        {
        }

        public OutputVariable(OutputVariable other, VariableBinding binding)
            : base(other, binding)
        {
        }
    }
}

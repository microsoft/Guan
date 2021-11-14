///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.
//
// @File: OutputVariable.cs
//
// @Owner: xunlu
// @Test:  xunlu
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
namespace Guan.Logic
{
    /// <summary>
    /// Variable output from a goal to the rule.
    /// </summary>
    internal class OutputVariable : LinkedVariable
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

// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
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

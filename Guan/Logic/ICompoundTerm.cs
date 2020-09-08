// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Guan.Logic
{
    /// <summary>
    /// Interface for exporting object to compound term.
    /// </summary>
    public interface ICompoundTerm
    {
        CompoundTerm ToCompoundTerm();
    }
}

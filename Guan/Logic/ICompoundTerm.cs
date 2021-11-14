//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
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

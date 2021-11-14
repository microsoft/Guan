// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    /// <summary>
    /// Variable term used in rules. At runtime they will be converted to Variable objects.
    /// </summary>
    internal class IndexedVariable : Term
    {
        private int index;
        private string name;

        public IndexedVariable(int index, string name)
        {
            this.index = index;
            this.name = name;
        }

        public int Index
        {
            get
            {
                return this.index;
            }
        }

        public string Name
        {
            get
            {
                return this.name;
            }
        }

        public override bool IsGround()
        {
            return false;
        }

        public override string ToString()
        {
            return "?" + this.name + "_" + this.index.ToString();
        }
    }
}

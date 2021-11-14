// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    /// <summary>
    /// Argument of a compound term.
    /// </summary>
    public class TermArgument
    {
        private string name;
        private ArgumentDescription desc;
        private Term value;

        public TermArgument(string name, Term value, ArgumentDescription desc = null)
        {
            this.name = name;
            this.value = value;
            this.desc = desc;
        }

        public string Name
        {
            get
            {
                return this.name;
            }
        }

        public ArgumentDescription Description
        {
            get
            {
                return this.desc;
            }
        }

        public Term Value
        {
            get
            {
                return this.value;
            }

            set
            {
                this.value = value;
            }
        }

        public override string ToString()
        {
            return this.Name + ":" + this.value.ToString();
        }
    }
}

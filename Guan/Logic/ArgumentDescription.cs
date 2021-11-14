// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    /// <summary>
    /// Meta data about term argument.
    /// </summary>
    public class ArgumentDescription
    {
        public ArgumentDescription(string text, bool required)
        {
            this.Text = text;
            this.Required = required;
        }

        public string Text { get; set; }

        public bool Required { get; set; }
    }
}

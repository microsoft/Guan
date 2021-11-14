//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------

namespace Guan.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.Json;

    internal sealed class JsonPropertyContext : IPropertyContext
    {
        private JsonElement json;

        public JsonPropertyContext(JsonElement json)
        {
            this.json = json;
        }

        public object this[string name]
        {
            get
            {
                string nextName = null;

                int index = name.IndexOf('.');
                if (index > 0)
                {
                    nextName = name.Substring(index + 1);
                    name = name.Substring(0, index);
                }

                index = -1;
                if (name.EndsWith(']'))
                {
                    index = name.LastIndexOf('[');
                    if (index > 0)
                    {
                        string indexString = name.Substring(index + 1, name.Length - index - 2);
                        name = name.Substring(0, index);
                        if (!int.TryParse(indexString, out index))
                        {
                            index = -1;
                        }
                    }
                }

                JsonElement result;
                if (!this.json.TryGetProperty(name, out result))
                {
                    return null;
                }

                if (index >= 0)
                {
                    if (result.ValueKind != JsonValueKind.Array)
                    {
                        throw new ArgumentException("Property not an array: " + name);
                    }

                    result = result[index];
                }

                if (nextName != null)
                {
                    return new JsonPropertyContext(result)[nextName];
                }

                switch (result.ValueKind)
                {
                    case JsonValueKind.Null:
                        return null;
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Number:
                        return result.GetInt64();
                    case JsonValueKind.String:
                        return result.GetString();
                    default:
                        return new JsonPropertyContext(result);
                }
            }
        }

        public override string ToString()
        {
            return this.json.ToString();
        }
    }
}

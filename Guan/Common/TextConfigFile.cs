///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  
//      
// @File: TextConfigFile.cs
//
// @Owner: xunlu
// @Test:  xunlu
//
// Purpose:
//   Base class to construct objects from configuration information in
//   text file.
//   
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;

namespace Guan.Common
{
    public abstract class TextConfigFile<T>
        where T : IConfigurable
    {
        private string m_path;
        private Dictionary<string, T> m_entries;

        protected TextConfigFile()
        {
            m_entries = new Dictionary<string, T>();
        }

        protected abstract T CreateEntry(string name, string value);

        public Dictionary<string, T> Entries
        {
            get
            {
                return m_entries;
            }
        }

        protected string ConfigFilePath
        {
            get
            {
                return m_path;
            }
        }

        public bool Load(string fileName)
        {
            m_path = Utility.GetFilePath(fileName);
            if (!File.Exists(m_path))
            {
                return false;
            }

            using (StreamReader reader = new StreamReader(m_path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length > 0)
                    {
                        CreateEntry(line, reader);
                    }
                }
            }

            return true;
        }

        private void CreateEntry(string line, StreamReader reader)
        {
            string name;
            string value;

            int index = line.IndexOf(':');
            if (index < 0)
            {
                name = line;
                value = string.Empty;
            }
            else
            {
                name = line.Substring(0, index).TrimEnd();
                value = line.Substring(index + 1).TrimStart();
            }

            T entry = CreateEntry(name, value);
            if (entry != null)
            {
                m_entries[name] = entry;
            }

            string category = null;

            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0)
                {
                    return;
                }

                if (entry != null && !line.StartsWith("#", StringComparison.Ordinal))
                {
                    if (line.StartsWith("[", StringComparison.Ordinal) && !line.StartsWith("[[", StringComparison.Ordinal))
                    {
                        index = line.IndexOf(']');
                        if (index > 0)
                        {
                            category = line.Substring(1, index - 1);
                            line = line.Substring(index + 1).TrimStart();
                        }
                    }

                    entry.SetConfig(category, line);
                }
            }
        }
    }
}

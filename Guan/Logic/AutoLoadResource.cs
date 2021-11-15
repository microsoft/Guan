// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;

    public static class AutoLoadResource
    {
        public static void LoadResources(Type resourceType)
        {
            MethodInfo addMethod = resourceType.GetMethod("Add", BindingFlags.Static | BindingFlags.Public);

            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            string thisAssemblyName = thisAssembly.GetName().Name;
            LoadAssembly(thisAssembly, resourceType, addMethod);

            HashSet<string> assemblies = new HashSet<string>();
            string externalAssemblies = Utility.GetConfig("ExternalAssemblies", string.Empty);
            if (!string.IsNullOrEmpty(externalAssemblies))
            {
                foreach (string name in externalAssemblies.Split(','))
                {
                    _ = assemblies.Add(name);
                }
            }

            string assemblyPath = thisAssembly.Location;
            string directoryPath = Path.GetDirectoryName(assemblyPath);
            foreach (string path in Directory.GetFiles(directoryPath, "*.dll"))
            {
                string assemblyName = Path.GetFileNameWithoutExtension(path);
                if ((assemblyName != thisAssemblyName) && assemblies.Contains(assemblyName))
                {
                    try
                    {
                        Assembly assembly = Assembly.LoadFrom(path);
                        LoadAssembly(assembly, resourceType, addMethod);
                    }
                    catch (BadImageFormatException)
                    {
                    }
                    catch (FileLoadException)
                    {
                    }
                    catch (ReflectionTypeLoadException)
                    {
                    }
                }
            }
        }

        private static void LoadAssembly(Assembly assembly, Type resourceType, MethodInfo addMethod)
        {
            foreach (Type type in assembly.GetTypes())
            {
                BindingFlags flags = BindingFlags.Static | BindingFlags.Public;
                FieldInfo[] fields = type.GetFields(flags);
                foreach (FieldInfo field in fields)
                {
                    if (field.FieldType.IsSubclassOf(resourceType))
                    {
                        LoadMember(type, field.Name, field.FieldType, BindingFlags.GetField, addMethod);
                    }
                }

                PropertyInfo[] properties = type.GetProperties(flags);
                foreach (PropertyInfo property in properties)
                {
                    if (property.PropertyType.IsSubclassOf(resourceType))
                    {
                        LoadMember(type, property.Name, property.PropertyType, BindingFlags.GetProperty, addMethod);
                    }
                }
            }
        }

        private static void LoadMember(Type type, string name, Type memberType, BindingFlags flags, MethodInfo addMethod)
        {
            object instance = type.InvokeMember(name, flags, null, null, null, CultureInfo.InvariantCulture);
            _ = addMethod.Invoke(null, new object[] { instance });
        }
    }
}

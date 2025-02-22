﻿using System;
using System.IO;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NetOfficeFw.Build
{
    public class CleanAddin : AppDomainIsolatedTask
    {
        [Required]
        public ITaskItem AssemblyPath { get; set; }

        public ITaskItem[] OfficeApps { get; set; }

        public override bool Execute()
        {
            try
            {
                var assemblyPath = this.AssemblyPath.ItemSpec;
                var assemblyDir = Path.GetDirectoryName(assemblyPath);

                var assembly = Assembly.UnsafeLoadFrom(assemblyPath);
                var publicTypes = assembly.GetExportedTypes();

                foreach (var publicType in publicTypes)
                {
                    var isComVisible = publicType.IsComVisibleType();
                    var isAddinType = publicType.IsComAddinType();

                    if (isComVisible)
                    {
                        var guid = publicType.GUID;
                        var progId = publicType.GetProgId();

                        var guidComClass = guid.ToRegistryString();

                        Log.LogMessage(MessageImportance.High, $@"Cleaning class {progId} with GUID {guidComClass}");

                        var comClass = new ComClassRegistry(this.Log);
                        comClass.DeleteProgId(progId);
                        comClass.DeleteComClassNative(guid);

                        if (Environment.Is64BitOperatingSystem)
                        {
                            comClass.DeleteComClassWOW6432(guid);
                        }

                        if (isAddinType && this.OfficeApps != null)
                        {
                            foreach (var officeAppItem in this.OfficeApps)
                            {
                                var officeApp = officeAppItem.ItemSpec;
                                Log.LogMessage(MessageImportance.High, $@"Cleaning add-in {progId} from Microsoft Office {officeApp}");
                                comClass.DeleteOfficeAddin(officeApp, progId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }

            return true;
        }
    }
}

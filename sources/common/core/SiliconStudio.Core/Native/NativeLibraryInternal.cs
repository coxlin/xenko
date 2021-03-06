// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System;

namespace SiliconStudio.Core
{
    /// <summary>
    /// Internally this file is used by the ExecServer project in order to copy native dlls to shadow copy folders.
    /// </summary>
    internal static class NativeLibraryInternal
    {
        private const string AppDomainCustomDllPathKey = "native_";

#if SILICONSTUDIO_PLATFORM_WINDOWS_DESKTOP
#if !SILICONSTUDIO_RUNTIME_CORECLR
        public static void SetShadowPathForNativeDll(AppDomain appDomain, string dllFileName, string dllPath)
        {
            if (dllFileName == null) throw new ArgumentNullException("dllFileName");
            if (dllPath == null) throw new ArgumentNullException("dllPath");
            var key = AppDomainCustomDllPathKey + dllFileName.ToLowerInvariant();
            appDomain.SetData(key, dllPath);
        }
#endif
#endif

        public static string GetShadowPathForNativeDll(string dllFileName)
        {
#if SILICONSTUDIO_PLATFORM_WINDOWS_DESKTOP
#if !SILICONSTUDIO_RUNTIME_CORECLR
            if (dllFileName == null) throw new ArgumentNullException("dllFileName");
            var key = AppDomainCustomDllPathKey + dllFileName.ToLowerInvariant();
            return (string)AppDomain.CurrentDomain.GetData(key);
#else
            return null;
#endif
#else
            return null;
#endif
        }
    }
}

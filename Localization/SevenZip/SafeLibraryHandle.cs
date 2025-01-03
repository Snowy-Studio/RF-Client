﻿using System;
using System.Runtime.ConstrainedExecution;
using Microsoft.Win32.SafeHandles;

namespace Localization.SevenZip
{
    internal sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeLibraryHandle() : base(true)
        {
        }

        /// <summary>Release library handle</summary>
        /// <returns>true if the handle was released</returns>
        protected override bool ReleaseHandle() => Kernel32Dll.FreeLibrary(this.handle);
    }
}
// https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/main/src/client/Microsoft.Identity.Client/Platforms/Features/WebView2WebUi/Win32Window.cs
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Windows.Forms;

namespace Microsoft.Identity.Client.Platforms.Features.WebView2WebUi
{
    internal class Win32Window : IWin32Window
    {
        public Win32Window(IntPtr handle)
        {
            Handle = handle;
        }
        public IntPtr Handle { get; }

    }
}
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Security.Authentication.Web.Core;
using WinRT;

namespace XAU.Services;

// WAM no desktop nao mostra UI interativa sem janela pai, e o WinRT
// RequestTokenAsync lanca excecao. O caminho suportado e a interface COM
// IWebAuthenticationCoreManagerInterop (recebe um HWND). O .NET moderno nao tem mais
// marshaling de IInspectable/[ComImport], entao chamamos a vtable direto via CsWinRT.
internal static class WamInterop
{
    private static readonly Guid InteropIid = new("F4B8E804-811E-4436-B69C-44CB67B72084");
    private const string ClassName = "Windows.Security.Authentication.Web.Core.WebAuthenticationCoreManager";

    // Layout da vtable: 0-2 IUnknown, 3-5 IInspectable, 6 RequestTokenForWindowAsync.
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int RequestTokenForWindow(IntPtr thisPtr, IntPtr appWindow, IntPtr request, ref Guid riid, out IntPtr asyncOp);

    public static async Task<WebTokenRequestResult> RequestTokenForWindowAsync(IntPtr hwnd, WebTokenRequest request)
    {
        using var interop = ActivationFactory.Get(ClassName, InteropIid);
        var thisPtr = interop.ThisPtr;
        var vtbl = Marshal.ReadIntPtr(thisPtr);
        var methodPtr = Marshal.ReadIntPtr(vtbl, 6 * IntPtr.Size);
        var fn = Marshal.GetDelegateForFunctionPointer<RequestTokenForWindow>(methodPtr);

        var requestPtr = MarshalInspectable<WebTokenRequest>.FromManaged(request);
        try
        {
            var asyncIid = GuidGenerator.CreateIID(typeof(IAsyncOperation<WebTokenRequestResult>));
            var hr = fn(thisPtr, hwnd, requestPtr, ref asyncIid, out var asyncOpPtr);
            Marshal.ThrowExceptionForHR(hr);
            var op = MarshalInterface<IAsyncOperation<WebTokenRequestResult>>.FromAbi(asyncOpPtr);
            return await op;
        }
        finally
        {
            MarshalInspectable<WebTokenRequest>.DisposeAbi(requestPtr);
        }
    }
}

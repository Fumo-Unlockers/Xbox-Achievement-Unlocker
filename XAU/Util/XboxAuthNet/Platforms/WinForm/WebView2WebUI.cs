using Microsoft.Web.WebView2.Core;
using XboxAuthNet.OAuth.CodeFlow;

namespace XboxAuthNet.Platforms.WinForm;

#if NET5_WIN
[System.Runtime.Versioning.SupportedOSPlatform("windows7")]
#endif
internal class WebView2WebUI : IWebUI
{
    private readonly object? _parent;
    private readonly SynchronizationContext? _synchronizationContext;

    public WebView2WebUI(WebUIOptions options)
    {
        _parent = options.ParentObject;
        _synchronizationContext = options.SynchronizationContext;
    }

    public async Task<CodeFlowAuthorizationResult> DisplayDialogAndInterceptUri(
        Uri uri,
        ICodeFlowUrlChecker uriChecker,
        CancellationToken cancellationToken)
    {
        WpfWindowWrapper window = new WpfWindowWrapper();
        CodeFlowAuthorizationResult result = new CodeFlowAuthorizationResult();
        await UIThreadHelper.InvokeUIActionOnSafeThread(() =>
        {
            using (var form = new WinFormsPanelWithWebView2(window.Handle))
            {
                result = form.DisplayDialogAndInterceptUri(uri, uriChecker, cancellationToken);
            }
        }, _synchronizationContext, cancellationToken);
        return result;
    }

    public async Task DisplayDialogAndNavigateUri(Uri uri, CancellationToken cancellationToken)
    {
        WpfWindowWrapper window = new WpfWindowWrapper();
        await UIThreadHelper.InvokeUIActionOnSafeThread(() =>
        {
            using (var form = new WinFormsPanelWithWebView2(window.Handle))
            {
                form.DisplayDialogAndNavigateUri(uri, cancellationToken);
            }
        }, _synchronizationContext, cancellationToken);
    }

    public static bool IsWebView2Available()
    {
        try
        {
            string wv2Version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            return !string.IsNullOrEmpty(wv2Version);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            return false;
        }
        catch (Exception ex) when (ex is BadImageFormatException || ex is DllNotFoundException)
        {
            return false;
            //throw new MsalClientException(MsalError.WebView2LoaderNotFound, MsalErrorMessage.WebView2LoaderNotFound, ex);
        }
    }
    public class WpfWindowWrapper : System.Windows.Forms.IWin32Window
{
    public WpfWindowWrapper()
    {
        Handle = new System.Windows.Interop.WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle;
    }
    public IntPtr Handle { get; private set; }
}
}

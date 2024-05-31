// This code is from MSAL.NET
// https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/main/src/client/Microsoft.Identity.Client/Platforms/Features/WebView2WebUi/WinFormsPanelWithWebView2.cs
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
using Microsoft.Identity.Client.Platforms.Features.WebView2WebUi;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using XboxAuthNet.OAuth.CodeFlow;
using Application = System.Windows.Forms.Application;

namespace XboxAuthNet.Platforms.WinForm;

internal class WinFormsPanelWithWebView2 : Form
{
    private const int UIWidth = 566;
    private WebView2 _webView2;
    private const string WebView2UserDataFolder = "%UserProfile%/.msal/webview2/data";

    private ICodeFlowUrlChecker? _uriChecker;
    private CodeFlowAuthorizationResult _authCode;
    private IWin32Window? _ownerWindow;

    public WinFormsPanelWithWebView2(
        object? ownerWindow)
    {
        if (ownerWindow == null)
        {
            _ownerWindow = null;
        }
        else if (ownerWindow is IWin32Window)
        {
            _ownerWindow = (IWin32Window)ownerWindow;
        }
        else if (ownerWindow is IntPtr ptr && ptr != IntPtr.Zero)
        {
            _ownerWindow = new Win32Window(ptr);
        }
        else
        {
            throw new ArgumentException("Invalid owner window type. Expected types are IWin32Window or IntPtr (for window handle).");
        }

        InitializeComponent();

        CoreWebView2Environment.GetAvailableBrowserVersionString();
        _webView2!.CreationProperties = new CoreWebView2CreationProperties()
        {
            UserDataFolder = Environment.ExpandEnvironmentVariables(WebView2UserDataFolder)
        };
    }

    public CodeFlowAuthorizationResult DisplayDialogAndInterceptUri(
        Uri uri, ICodeFlowUrlChecker uriChecker, CancellationToken cancellationToken)
    {
        this._uriChecker = uriChecker;

        _webView2.CoreWebView2InitializationCompleted += WebView2Control_CoreWebView2InitializationCompleted;
        _webView2.NavigationStarting += WebView2Control_NavigationStarting;

        // Starts the navigation
        _webView2.Source = uri;
        DisplayDialog(cancellationToken);

        if (_authCode.IsEmpty)
            throw new InvalidOperationException("_authCode was empty");
        return _authCode;
    }

    public void DisplayDialogAndNavigateUri(Uri uri, CancellationToken cancellationToken)
    {
        _webView2.CoreWebView2InitializationCompleted += WebView2Control_CoreWebView2InitializationCompleted;

        // Starts the navigation
        _webView2.Source = uri;

        using (cancellationToken.Register(CloseIfOpen))
        {
            InvokeHandlingOwnerWindow(() => ShowDialog(_ownerWindow));
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private void DisplayDialog(CancellationToken cancellationToken)
    {
        DialogResult uiResult = DialogResult.None;

        using (cancellationToken.Register(CloseIfOpen))
        {
            InvokeHandlingOwnerWindow(() => uiResult = ShowDialog(_ownerWindow));
            cancellationToken.ThrowIfCancellationRequested();
        }

        switch (uiResult)
        {
            case DialogResult.OK:
                break;
            case DialogResult.Cancel:
                throw new AuthCodeException(null, "User canceled authentication. ");
            default:
                throw new InvalidOperationException(
                    "WebView2 returned an unexpected result: " + uiResult);
        }
    }

    private void CloseIfOpen()
    {
        if (Application.OpenForms.OfType<WinFormsPanelWithWebView2>().Any())
        {
            InvokeOnly(Close);
        }
    }

    private void PlaceOnTop(object? sender, EventArgs e)
    {
        // If we don't have an owner we need to make sure that the pop up browser
        // window is on top of other windows.  Activating the window will accomplish this.
        if (null == Owner)
        {
            Activate();
        }
    }

    /// <summary>
    /// Some calls need to be made on the UI thread and this is the central place to check if we have an owner
    /// window and if so, ensure we invoke on that proper thread.
    /// </summary>
    /// <param name="action"></param>
    private void InvokeHandlingOwnerWindow(Action action)
    {
        // We only support WindowsForms (since our dialog is Win Forms based)
        if (_ownerWindow != null && _ownerWindow is Control winFormsControl)
        {
            winFormsControl.Invoke(action);
        }
        else
        {
            action();
        }
    }

    /// <summary>
    /// Some calls need to be made on the UI thread and this is the central place to do so and if so, ensure we invoke on that proper thread.
    /// </summary>
    /// <param name="action"></param>
    private void InvokeOnly(Action action)
    {
        if (InvokeRequired)
        {
            this.Invoke(action);
        }
        else
        {
            action();
        }
    }

    private void InitializeComponent()
    {
        InvokeHandlingOwnerWindow(() =>
        {
            Screen screen = (_ownerWindow != null)
                ? Screen.FromHandle(_ownerWindow.Handle)
                : Screen.PrimaryScreen;

            // Window height is set to 70% of the screen height.
            int uiHeight = (int)(Math.Max(screen.WorkingArea.Height, 160) * 70.0 / WindowsDpiHelper.ZoomPercent);
            var webBrowserPanel = new Panel();
            webBrowserPanel.SuspendLayout();
            SuspendLayout();

            // webBrowser
            _webView2 = new WebView2();
            _webView2.Dock = DockStyle.Fill;
            _webView2.Location = new System.Drawing.Point(0, 25);
            _webView2.MinimumSize = new System.Drawing.Size(20, 20);
            _webView2.Name = "WebView2";
            _webView2.Size = new System.Drawing.Size(UIWidth, 565);
            _webView2.TabIndex = 1;

            // webBrowserPanel
            webBrowserPanel.Controls.Add(_webView2);
            webBrowserPanel.Dock = DockStyle.Fill;
            webBrowserPanel.BorderStyle = BorderStyle.None;
            webBrowserPanel.Location = new System.Drawing.Point(0, 0);
            webBrowserPanel.Name = "webBrowserPanel";
            webBrowserPanel.Size = new System.Drawing.Size(UIWidth, uiHeight);
            webBrowserPanel.TabIndex = 2;

            // BrowserAuthenticationWindow
            AutoScaleDimensions = new SizeF(6, 13);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(UIWidth, uiHeight);
            Controls.Add(webBrowserPanel);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "BrowserAuthenticationWindow";

            // Move the window to the center of the parent window only if owner window is set.
            StartPosition = (_ownerWindow != null)
                ? FormStartPosition.CenterParent
                : FormStartPosition.CenterScreen;
            Text = string.Empty;
            ShowIcon = false;
            MaximizeBox = false;
            MinimizeBox = false;

            // If we don't have an owner we need to make sure that the pop up browser
            // window is in the task bar so that it can be selected with the mouse.
            ShowInTaskbar = null == _ownerWindow;

            webBrowserPanel.ResumeLayout(false);
            ResumeLayout(false);
        });

        this.Shown += PlaceOnTop;
    }

    private void WebView2Control_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (CheckForEndUrl(new Uri(e.Uri)))
        {
            // _logger.Verbose("[WebView2Control] Redirect URI reached. Stopping the interactive view");
            e.Cancel = true;
        }
        else
        {
            // _logger.Verbose("[WebView2Control] Navigating to " + e.Uri);
        }
    }

    private bool CheckForEndUrl(Uri url)
    {
        if (_uriChecker == null)
            throw new InvalidOperationException("_uriChecker was null");

        var result = _uriChecker.GetAuthCodeResult(url);

        if (!result.IsEmpty)
        {
            // This should close the dialog
            DialogResult = DialogResult.OK;
            _authCode = result;
            _uriChecker = null;
        }

        return !result.IsEmpty;
    }

    private void WebView2Control_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        //_logger.Verbose("[WebView2Control] CoreWebView2InitializationCompleted ");
        _webView2.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        _webView2.CoreWebView2.Settings.AreDevToolsEnabled = false;
        _webView2.CoreWebView2.Settings.AreHostObjectsAllowed = false;
        _webView2.CoreWebView2.Settings.IsScriptEnabled = true;
        _webView2.CoreWebView2.Settings.IsZoomControlEnabled = false;
        _webView2.CoreWebView2.Settings.IsStatusBarEnabled = true;
        _webView2.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;

        _webView2.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
    }

    private void CoreWebView2_DocumentTitleChanged(object? sender, object e)
    {
        Text = _webView2.CoreWebView2.DocumentTitle ?? "";
    }
}

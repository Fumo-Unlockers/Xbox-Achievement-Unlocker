namespace XboxAuthNet.OAuth.CodeFlow;

public class CodeFlowBuilder
{
    private readonly ICodeFlowApiClient _apiClient;

    public CodeFlowBuilder(ICodeFlowApiClient apiClient)
    {
        this._apiClient = apiClient;
    }

    private WebUIOptions? uiOptions;
    private IWebUI? webUI;
    private ICodeFlowUrlChecker? uriChecker;

    public CodeFlowBuilder WithUIParent(object parent)
    {
        uiOptions ??= createDefaultWebUIOptions();
        uiOptions.ParentObject = parent;
        return this;
    }

    public CodeFlowBuilder WithUITitle(string title)
    {
        uiOptions ??= createDefaultWebUIOptions();
        uiOptions.Title = title;
        return this;
    }

    public CodeFlowBuilder WithUIOptions(WebUIOptions options)
    {
        this.uiOptions = options;
        return this;
    }

    public CodeFlowBuilder WithWebUI(IWebUI ui)
    {
        this.webUI = ui;
        return this;
    }

    public CodeFlowBuilder WithWebUI(Func<WebUIOptions, IWebUI> factory)
    {
        this.uiOptions ??= createDefaultWebUIOptions();
        WithWebUI(factory.Invoke(this.uiOptions));
        return this;
    }

    public CodeFlowBuilder WithUriChecker(ICodeFlowUrlChecker checker)
    {
        this.uriChecker = checker;
        return this;
    }

    public CodeFlowAuthenticator Build()
    {
        uriChecker ??= createDefaultUriChecker();
        webUI ??= createDefaultWebUIForPlatform();
        return new CodeFlowAuthenticator(_apiClient, webUI, uriChecker);
    }

    private ICodeFlowUrlChecker createDefaultUriChecker()
    {
        return new CodeFlowUrlChecker();
    }

    private IWebUI createDefaultWebUIForPlatform()
    {
        this.uiOptions ??= createDefaultWebUIOptions();
        return PlatformManager.CurrentPlatform.CreateWebUI(uiOptions);
    }

    private WebUIOptions createDefaultWebUIOptions() => new WebUIOptions
    {
        ParentObject = null,
        SynchronizationContext = SynchronizationContext.Current
    };
}

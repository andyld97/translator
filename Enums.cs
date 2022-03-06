namespace Translator
{
    public enum JsonGeneration
    {
        All,
        OnlyLang,
        OnlyBlogs
    }

    public enum LogLevel
    {
        Debug,
        Information,
        Warning,
        Error,
    }

    public enum LogLevelSetting
    {
        Production,
        All,
        OnlyWarningsAndErros,
        None
    }

    public enum UrlValidationResult
    {
        Success,
        NoValidStart,
        CannotDetectStringAsUrl,
        OnlyHTTP,
        WrongScheme
    }

    public enum TranslationAPI
    {
        DeepLFallback_GCloudTranslation,
        DeepLFallback_GFreeTranslation,
        OnlyGoogleCloudTranslation,
        OnlyGoogleFreeTranslation,
        OnlyDeepLTranslation
    }

    public enum ItemType
    {
        GeneralGeneral,
        GeneralPage,
        Scopes
    }

    public enum ScopeType
    {
        General,
        MultipleParents,
        SinglePage
    }
}
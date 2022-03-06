using DeepL;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.Translate.V3;
using HtmlAgilityPack;
using Translator.Model;
using Translator.Model.Log;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using SupportedLanguage = DeepL.SupportedLanguage;
using System.Windows;

namespace Translator.Helper
{
    public static class TranslationHelper
    {
        private static IEnumerable<SupportedLanguage> supportedLanguagesDeeplCache = null;
        private static readonly TranslationServiceClient client = null;

        static TranslationHelper()
        {
            try
            {
                client = TranslationServiceClient.Create();
            }
            catch
            {

            }
        }

        // ToDo: *** If the current project has no DeepL key, just use a fallback
        //           If the current project has no GoogleProjectCloudID just use either DeepL (if api key is avaialable) or GoogleFreeTranslation as the last fall back - it's free.

        public static async Task<string> TranslateHTML(string content, string sourceLanguage, string targetLanguage, TranslationAPI translationAPI)
        {
            bool isLanguageSupportedByDeepL = await IsLanguageSupportedByDeepL(targetLanguage);
            if (translationAPI == TranslationAPI.OnlyDeepLTranslation && !isLanguageSupportedByDeepL)
            {
                Logger.LogWarning("Übersetzung nicht durchgeführt, da DeepL diese Sprache nicht unterstützt, bitte ändern Sie Ihre Einstellungen oder warten Sie, bis DeepL die Sprache unterstützt!", "TranslationHelper");
                return content;
            }

            // https://html-agility-pack.net/knowledge-base/18396699/how-to-fix-html-tags-which-is-missing-the--open-----close--tags--with-htmlagilitypack
            HtmlNode.ElementsFlags["p"] = HtmlElementFlag.Closed;
            HtmlDocument document = new HtmlDocument
            {
                OptionFixNestedTags = true,
                OptionWriteEmptyNodes = true,
                OptionAutoCloseOnEnd = true
            };
            document.LoadHtml(content);

            List<string> items = new List<string>();
            HtmlNode scriptNode = null;
            foreach (var node in document.DocumentNode.DescendantsAndSelf())
            {
                if (node.Name == "script")
                {
                    scriptNode = node;
                    continue;
                }

                if (scriptNode != null && scriptNode.ChildNodes.Contains(node))
                    continue;

                if (node.ChildNodes.Count == 0 && node.InnerHtml.Trim().Length > 0)
                {
                    // Don't translate empty entries
                    if (string.IsNullOrWhiteSpace(node.InnerHtml.Trim()))
                        continue;

                    if ((translationAPI == TranslationAPI.DeepLFallback_GCloudTranslation || translationAPI == TranslationAPI.DeepLFallback_GFreeTranslation) && !isLanguageSupportedByDeepL)
                    {
                        if (translationAPI == TranslationAPI.DeepLFallback_GCloudTranslation)
                        {
                            var result = await TranslateStringGoogleCloudPlatform(node.InnerHtml, sourceLanguage, targetLanguage);
                            if (result.Success)
                                node.InnerHtml = result.Text;
                            else
                                Logger.LogWarning("Fehler beim Übersetzen!", "TranslationHelper");
                        }
                        else if (translationAPI == TranslationAPI.DeepLFallback_GFreeTranslation)
                        {
                            var result = await TranslateStringWithGoogleTranslateFree(node.InnerHtml, sourceLanguage, targetLanguage);
                            if (result.Success)
                                node.InnerHtml = MergeTranslation(result.Text);
                            else
                                Logger.LogWarning("Fehler beim Übersetzen!", "TranslationHelper");
                        }
                    }
                    else if (translationAPI == TranslationAPI.OnlyGoogleCloudTranslation)
                    {
                        var result = await TranslateStringGoogleCloudPlatform(node.InnerHtml, sourceLanguage, targetLanguage);
                        if (result.Success)
                            node.InnerHtml = result.Text;
                        else
                            Logger.LogWarning("Fehler beim Übersetzen!", "TranslationHelper");
                    }
                    else if (translationAPI == TranslationAPI.OnlyGoogleFreeTranslation)
                    {
                        var result = await TranslateStringWithGoogleTranslateFree(node.InnerHtml, sourceLanguage, targetLanguage);
                        if (result.Success)
                            node.InnerHtml = MergeTranslation(result.Text);
                        else
                            Logger.LogWarning("Fehler beim Übersetzen!", "TranslationHelper");
                    }
                    else
                        items.Add(node.InnerHtml);
                }
            }

            var translationResult = await TranslationHelper.TranslateStringArrayDeepL(items.ToArray(), sourceLanguage, targetLanguage);
            if (!translationResult.Success)
                throw translationResult.Exception;

            int counter = 0;
            scriptNode = null;
            foreach (var node in document.DocumentNode.DescendantsAndSelf())
            {
                if (node.Name == "script")
                {
                    scriptNode = node;
                    continue;
                }

                if (scriptNode != null && scriptNode.ChildNodes.Contains(node))
                    continue;

                if (node.ChildNodes.Count == 0 && node.InnerHtml.Trim().Length > 0)
                {
                    // Don't translate empty entries
                    if (string.IsNullOrWhiteSpace(node.InnerHtml.Trim()))
                        continue;

                    string item = (items.Count == 0 ? node.InnerHtml : translationResult.Text[counter]);

                    // Five Head Move to ensure that spacing is correctly set
                    if (!node.ParentNode.Name.StartsWith("a"))
                        item += " ";
                    else
                    {
                        var url = node.ParentNode.Attributes["href"].Value;
                        node.ParentNode.Attributes["href"].Value = TranslateProjectUrl(url, targetLanguage);
                    }

                    node.InnerHtml = item;
                    counter++;
                }
            }

            return document.DocumentNode.OuterHtml;
        }

        public static async Task<TranslationResult<string>> TranslateText(string text, string sourceLanguage, string targetLanguage, TranslationAPI translationAPI)
        {
            if (sourceLanguage == targetLanguage)
                return TranslationResult<string>.OK(text);

            if (string.IsNullOrEmpty(text))
                return TranslationResult<string>.OK(string.Empty);

            bool isLanguageSupportedByDeepL = await IsLanguageSupportedByDeepL(targetLanguage);
            if (translationAPI == TranslationAPI.OnlyDeepLTranslation && !isLanguageSupportedByDeepL)
            {
                Logger.LogWarning("Übersetzung nicht durchgeführt, da DeepL diese Sprache nicht unterstützt, bitte ändern Sie Ihre Einstellungen oder warten Sie, bis DeepL die Sprache unterstützt!", "TranslationHelper");
                return TranslationResult<string>.Fail(new NotSupportedException());
            }
            else if (translationAPI == TranslationAPI.OnlyGoogleCloudTranslation)
                return await TranslateStringGoogleCloudPlatform(text, sourceLanguage, targetLanguage);
            else if (translationAPI == TranslationAPI.OnlyGoogleFreeTranslation)
                return await TranslateStringWithGoogleTranslateFreeWithMergedText(text, sourceLanguage, targetLanguage);
            else if (translationAPI == TranslationAPI.DeepLFallback_GCloudTranslation)
            {
                if (isLanguageSupportedByDeepL)
                    return await TranslateTextDeepL(text, sourceLanguage, targetLanguage);
                else
                    return await TranslateStringGoogleCloudPlatform(text, sourceLanguage, targetLanguage);
            }
            else if (translationAPI == TranslationAPI.DeepLFallback_GFreeTranslation)
            {
                if (isLanguageSupportedByDeepL)
                    return await TranslateTextDeepL(text, sourceLanguage, targetLanguage);
                else
                    return await TranslateStringWithGoogleTranslateFreeWithMergedText(text, sourceLanguage, targetLanguage);
            }

            return TranslationResult<string>.Fail(new ArgumentException());
        }

        public static async Task<TranslationResult<string[]>> TranslateStringArray(string[] text, string sourceLanguage, string targetLanguage, TranslationAPI translationAPI)
        {
            if (text == null || text.Length == 0)
                return TranslationResult<string[]>.OK(new string[] { });

            bool isLanguageSupportedByDeepL = await IsLanguageSupportedByDeepL(targetLanguage);
            if (translationAPI == TranslationAPI.OnlyDeepLTranslation && !isLanguageSupportedByDeepL)
            {
                Logger.LogWarning("Übersetzung nicht durchgeführt, da DeepL diese Sprache nicht unterstützt, bitte ändern Sie Ihre Einstellungen oder warten Sie, bis DeepL die Sprache unterstützt!", "TranslationHelper");
                return TranslationResult<string[]>.Fail(new NotSupportedException());
            }
            else if (translationAPI == TranslationAPI.OnlyGoogleCloudTranslation)
                return await TranslateStringArrayGoogleCloudPlatform(text, sourceLanguage, targetLanguage);
            else if (translationAPI == TranslationAPI.OnlyGoogleFreeTranslation)
                return await TranslateStringArrayWithGoogleTranslateFree(text, sourceLanguage, targetLanguage);
            else if (translationAPI == TranslationAPI.DeepLFallback_GCloudTranslation)
            {
                if (isLanguageSupportedByDeepL)
                    return await TranslateStringArrayDeepL(text, sourceLanguage, targetLanguage);
                else
                    return await TranslateStringArrayGoogleCloudPlatform(text, sourceLanguage, targetLanguage);
            }
            else if (translationAPI == TranslationAPI.DeepLFallback_GFreeTranslation)
            {
                if (isLanguageSupportedByDeepL)
                    return await TranslateStringArrayDeepL(text, sourceLanguage, targetLanguage);
                else
                    return await TranslateStringArrayWithGoogleTranslateFree(text, sourceLanguage, targetLanguage);
            }

            return TranslationResult<string[]>.Fail(new ArgumentException());
        }

        #region DeepL
        private static async Task<TranslationResult<string>> TranslateTextDeepL(string text, string sourceLanguage, string targetLanguage)
        {
            using (DeepLClient client = new DeepLClient(Project.CurrentProject.DeepLApiKey))
            {             

                try
                {
                    DeepL.Translation translation = await client.TranslateAsync(text, sourceLanguageCode: sourceLanguage, targetLanguageCode: targetLanguage);
                    return TranslationResult<string>.OK(translation.Text.CleanUp());
                }
                catch (DeepLException exception)
                {
                    return TranslationResult<string>.Fail(exception);
                }
            }
        }


        private static async Task<TranslationResult<string[]>> TranslateStringArrayDeepL(string[] text, string sourceLanguage, string targetLanguage)
        {
            if (text == null || text.Length == 0)
                return TranslationResult<string[]>.OK(new string[] { });

            using (DeepLClient client = new DeepLClient(Project.CurrentProject.DeepLApiKey))
            {
                try
                {
                    IEnumerable<DeepL.Translation> translation = await client.TranslateAsync(text, sourceLanguageCode: sourceLanguage, targetLanguageCode: targetLanguage);
                    return TranslationResult<string[]>.OK(translation.Select(p => p.Text.CleanUp()).ToArray());
                }
                catch (DeepLException exception)
                {
                    return TranslationResult<string[]>.Fail(exception);
                }
            }
        }

        private static async Task BuildSupportedLanguageCache(DeepLClient client)
        {
            try
            {
                if (supportedLanguagesDeeplCache == null)
                    supportedLanguagesDeeplCache = await client.GetSupportedLanguagesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Fehler beim Anfragen der von DeepL unterstützten Sprachen: {ex.Message}", "TranslationHelper");
            }
        }

        private static async Task<bool> IsLanguageSupportedByDeepL(string langCode, DeepLClient client = null)
        {
            try
            {
                if (supportedLanguagesDeeplCache == null)
                {
                    if (client == null)
                        using (client = new DeepLClient(Project.CurrentProject.DeepLApiKey))
                            await BuildSupportedLanguageCache(client);
                    else
                        await BuildSupportedLanguageCache(client);
                }

                if (supportedLanguagesDeeplCache != null)
                    return supportedLanguagesDeeplCache.Any(l => l.LanguageCode.ToLower() == langCode);

                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Google Cloud Translator
        private static async Task<TranslationResult<string>> TranslateStringGoogleCloudPlatform(string text, string sourceLangauge, string targetLanguage)
        {
            if (string.IsNullOrEmpty(text))
                return new TranslationResult<string>() { Text = string.Empty, Success = true };

            TranslateTextRequest request = new TranslateTextRequest
            {
                Contents = { text },
                TargetLanguageCode = targetLanguage,
                SourceLanguageCode = sourceLangauge,
                Parent = new ProjectName(Project.CurrentProject.GoogleCloudProjectID).ToString(),
            };

            try
            {
                TranslateTextResponse response = await client.TranslateTextAsync(request);
                // response.Translations will have one entry, because request.Contents has one entry.
                Google.Cloud.Translate.V3.Translation translation = response.Translations.FirstOrDefault();
                return TranslationResult<string>.OK(translation.TranslatedText.CleanUp());
            }
            catch (Exception ex)
            {
                return TranslationResult<string>.Fail(ex);
            }
        }

        private static async Task<TranslationResult<string[]>> TranslateStringArrayGoogleCloudPlatform(string[] text, string sourceLanguage, string targetLanguage)
        {
            List<string> results = new List<string>();
            foreach (var item in text)
            {
                var translatedItem = await TranslateStringGoogleCloudPlatform(item, sourceLanguage, targetLanguage);
                if (translatedItem.Success)
                    results.Add(translatedItem.Text.CleanUp());
                else
                    return TranslationResult<string[]>.Fail(translatedItem.Exception);
            }

            if (text.Length == results.Count)
                return TranslationResult<string[]>.OK(results.ToArray());

            else
                return TranslationResult<string[]>.Fail(new ArgumentException());
        }

        #endregion

        #region Google Free Translator
        private static async Task<TranslationResult<string>> TranslateStringWithGoogleTranslateFreeWithMergedText(string text, string sourceLanguage, string targetLanguage)
        {
            var freeResult = await TranslateStringWithGoogleTranslateFree(text, sourceLanguage, targetLanguage);

            if (freeResult.Success)
                return TranslationResult<string>.OK(MergeTranslation(freeResult.Text));
            else
                return TranslationResult<string>.Fail(freeResult.Exception);
        }

        private static async Task<TranslationResult<string[]>> TranslateStringArrayWithGoogleTranslateFree(string[] text, string sourceLanguage, string targetLanguage)
        {
            List<string> results = new List<string>();
            foreach (var item in text)
            {
                var translatedItem = await TranslateStringWithGoogleTranslateFreeWithMergedText(item, sourceLanguage, targetLanguage);
                if (translatedItem.Success)
                    results.Add(translatedItem.Text);
                else
                    return TranslationResult<string[]>.Fail(translatedItem.Exception);
            }

            if (text.Length == results.Count)
                return TranslationResult<string[]>.OK(results.ToArray());

            else
                return TranslationResult<string[]>.Fail(new ArgumentException());
        }

        private static async Task<TranslationResult<string[]>> TranslateStringWithGoogleTranslateFree(string text, string sourceLanguage, string targetLanguage)
        {
            // Replace unicode chars via their base64 represenation (translator has problems with the unicode representation)
            var matches = Regex.Matches(text, @"[^\u0000-\u007F]");
            Dictionary<string, string> assoc = new Dictionary<string, string>();
            string[] invalidMatches = new string[] { "ü", "ä", "ö", "ß" };

            foreach (Match match in matches)
            {
                // ToDo: *** Ignore stuff like ü, ö, ä, µ, only handle special unicode chars (or remove urlencode)
                // != "✓"
                if (invalidMatches.Any(p => p == match.Value.ToLower()))
                    continue;

                string value = HttpUtility.UrlEncode(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(match.Value)));

                text = text.Replace(match.Value, value);

                if (!assoc.ContainsKey(match.Value))
                    assoc.Add(match.Value, value);                
            }
            

            //string translateUrl = "https://translate.googleapis.com/translate_a/t?client=gtx&dt=t&sl={sourceLanguage}&tl={targetLanguage}&q={text}";
            string translateUrl = $"https://clients5.google.com/translate_a/t?client=dict-chrome-ex&sl={sourceLanguage}&tl={targetLanguage}&q={HttpUtility.UrlEncode(text)}";

            // @bibisbeautypalaces doesn't get translated
            if (!text.Contains(" ") && text.Contains("@"))
                return TranslationResult<string[]>.OK(new string[] { text.CleanUp() });                      

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var result = await client.GetStringAsync(translateUrl);
                    if (!string.IsNullOrEmpty(result))
                    {
                        var content = JObject.Parse(result);

                        List<string> sentences = new List<string>();
                        foreach (var sentence in content.Value<JArray>("sentences"))
                        {
                            var trans = sentence.Value<string>("trans");

                            // Replace base64 encoded unicode chars back with their original values 
                            foreach (var asso in assoc)
                                trans = trans.Replace(asso.Value, asso.Key).Replace(HttpUtility.UrlDecode(asso.Value), asso.Key).CleanUp();


                            if (trans != null)
                                sentences.Add(trans);
                        }

                        var translation = sentences.ToArray();
                        return TranslationResult<string[]>.OK(translation);
                    }
                }
            }
            catch (Exception exception)
            {
                return TranslationResult<string[]>.Fail(exception);
            }

            return TranslationResult<string[]>.Fail(null);
        }      

        private static string MergeTranslation(string[] sentences)
        {
            string mergedText = string.Empty;

            if (sentences == null || sentences.Length == 0)
                return string.Empty;
            else if (sentences.Length == 1)
                return sentences.FirstOrDefault();

            foreach (var sentence in sentences)
            {
                string temp = sentence;

                if (!temp.EndsWith(Environment.NewLine) && !temp.EndsWith("\n"))
                    temp = temp.TrimEnd();
                else
                {
                    mergedText += temp;
                    continue;
                }

                mergedText += temp;

                if (mergedText.EndsWith(".") || mergedText.EndsWith("?") || mergedText.EndsWith("!"))
                { }
                else
                    mergedText += ".";

                mergedText += " ";
            }

            return mergedText;
        }
        #endregion

        public static UrlValidationResult ValidateUrl(this string url)
        {
            // Guid are accepted as an url, because they will be replaced later!
            if (Guid.TryParse(url, out Guid guid))
                return UrlValidationResult.Success;

            if (!Regex.IsMatch(url, @"^https?:\/\/", RegexOptions.IgnoreCase))
                return UrlValidationResult.NoValidStart;

            if (Uri.TryCreate(url, UriKind.Absolute, out var resultURI))
            {
                if (resultURI.Scheme == Uri.UriSchemeHttp)
                    return UrlValidationResult.OnlyHTTP;

                if (resultURI.Scheme == Uri.UriSchemeHttps)
                    return UrlValidationResult.Success;
                else
                    return UrlValidationResult.WrongScheme;
            }

            return UrlValidationResult.CannotDetectStringAsUrl;
        }

        public static string TranslateProjectUrl(string url, string targetLangCode)
        {
            // Ignore external urls and also blog-Urls (not blogs, blogs is valid)
            if (!url.StartsWith(Project.CurrentProject.ProjectUrl) || url.StartsWith($"{Project.CurrentProject.ProjectUrl}/blog/"))
                return url;

            // Don't translate an invalid url
            if (ValidateUrl(url) != UrlValidationResult.Success)
                return url;

            // Remove last slash
            string preparedUrl = (url.EndsWith("/") ? url.Substring(0, url.Length - 1) : url);

            // We should have something like
            // https://izoomyou.com
            // https://izoomyou.com/reels
            // https://izoomyou.com/feed

            if (preparedUrl == Project.CurrentProject.ProjectUrl)
                return $"{preparedUrl}/{targetLangCode}/";
            else
                return preparedUrl.Replace(Project.CurrentProject.ProjectUrl, $"{Project.CurrentProject.ProjectUrl}/{targetLangCode}");
        }
    }

    public class TranslationResult<T>
    {
        public bool Success { get; set; }

        public T Text { get; set; }

        public Exception Exception { get; set; }

        public static TranslationResult<T> Fail(Exception ex)
        {
            return new TranslationResult<T>() { Exception = ex, Success = false };
        }

        public static TranslationResult<T> OK(T text)
        {
            return new TranslationResult<T>() { Success = true, Text = text };
        }

        public override string ToString()
        {
            return Text.ToString();
        }
    }
}
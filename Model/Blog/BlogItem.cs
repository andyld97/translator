using HtmlAgilityPack;
using Translator.Helper;
using Translator.Model.Blog;
using Translator.Model.Log;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using static Translator.Helper.TranslationHelper;

namespace Translator.Model
{
    public class BlogItem : ICloneable, INotifyPropertyChanged
    {
        #region Member
        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly XmlDocument _dummyDoc = new XmlDocument();


        #region Private Member
        private DateTime publishedDate = DateTime.MinValue;
        private DateTime modifiedDate = DateTime.MinValue;
        private string title = string.Empty;
        private string text = string.Empty;
        private string previewText = string.Empty;
        private string imageFileName = string.Empty;
        private string altText = string.Empty;
        private BlogMeta meta = new BlogMeta();
        private string urlName = string.Empty;
        private string publisher = string.Empty;
        private bool isTranslatedBlogItem;
        private bool hasCustomUrl = false;
        private bool isSelected = false;
        private List<string> tags = new List<string>();
        #endregion
        #endregion

        #region Properties
        [JsonIgnore()]
        public Guid ID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Is set in case this is a translated blog
        /// </summary>
        [JsonIgnore]
        public Guid ParentID { get; set; }

        [JsonIgnore]
        public bool HasCustomUrl
        {
            get => hasCustomUrl;
            set
            {
                if (value != hasCustomUrl)
                {
                    hasCustomUrl = value;
                    FirePropertyChanged();
                }
            }
        }

        [JsonIgnore]
        [XmlIgnore]
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    FirePropertyChanged();
                }
            }
        }

        [JsonProperty("title")]
        public string Title
        {
            get => title.CleanUp();
            set
            {
                if (value != title)
                {
                    title = value.CleanUp();
                    FirePropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public bool IsTranslated
        {
            get => isTranslatedBlogItem;
            set
            {
                if (value != isTranslatedBlogItem)
                {
                    isTranslatedBlogItem = value;
                    FirePropertyChanged();
                }
            }
        }

        [XmlElement("content")]
        public XmlCDataSection ContentCDATA
        {
            get =>_dummyDoc.CreateCDataSection(Text.CleanUp());
            set => Text = value?.Data.CleanUp();
        }

        [XmlElement("preview")]
        public XmlCDataSection PreviewCDATA
        {
            get => _dummyDoc.CreateCDataSection(PreviewText.CleanUp());
            set => PreviewText = value?.Data.CleanUp();
        }

        [JsonProperty("content")]
        [XmlIgnore]
        public string Text
        {
            get => text;
            set
            {
                if (value != text)
                {
                    text = value;
                    FirePropertyChanged();
                }
            }
        }

        [JsonIgnore]
        [XmlIgnore]
        public string PreviewText
        {
            get => previewText;
            set
            {
                if (value != previewText)
                {
                    previewText = value;
                    FirePropertyChanged();
                }
            }
        }

        [JsonProperty("image")]
        public string ImageFileName
        {
            get => imageFileName;
            set
            {
                if (value != imageFileName)
                {
                    imageFileName = value;
                    FirePropertyChanged();
                }
            }
        }

        [JsonProperty("alt")]
        public string AltText
        {
            get => altText;
            set
            {
                if (value != altText)
                {
                    altText = value;
                    FirePropertyChanged();
                }
            }
        }

        [JsonProperty("meta")]
        public BlogMeta MetaInfo
        {
            get => meta;
            set
            {
                if (value != meta)
                {
                    meta = value;
                    FirePropertyChanged();
                }
            }
        }

        [JsonProperty("published_date")]
        public DateTime PublishedDate
        {
            get => publishedDate;
            set
            {
                if (value != publishedDate)
                {
                    publishedDate = value;
                    FirePropertyChanged();
                }
            }
        }

        [JsonProperty("modified_date")]
        public DateTime ModifiedDate
        {
            get => modifiedDate;
            set
            {
                if (value != modifiedDate)
                {
                    modifiedDate = value;
                    FirePropertyChanged();
                }
            }
        }

        [JsonProperty("url_name")]
        public string UrlName
        {
            get => urlName;
            set
            {
                if (value != urlName)
                {
                    urlName = value;
                    FirePropertyChanged();
                }
            }
        }

        [JsonProperty("publisher")]
        public string Publisher
        {
            get => publisher;
            set
            {
                if (value != publisher)
                {
                    publisher = value;
                    FirePropertyChanged();
                }
            }
        }

        public List<string> Tags
        {
            get => tags;
            set
            {
                if (value != tags)
                {
                    tags = value;
                    FirePropertyChanged();
                }
            }
        }

        [JsonIgnore]
        [XmlIgnore]
        public string Language { get; set; }

        #endregion

        public void FirePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task<BlogItem> Translate(string sourceLanguage, string targetLanguage)
        {
            BlogItem blogItem = new BlogItem
            {
                ParentID = this.ID,
                IsTranslated = true,
                ImageFileName = this.ImageFileName,
                PublishedDate = this.PublishedDate,
                Publisher = publisher,
                HasCustomUrl = this.HasCustomUrl,
                Tags = this.Tags,
            };

            // Translate
            var api = Settings.Instance.TranslationAPI;
            var altTag = await TranslateText(AltText, sourceLanguage, targetLanguage, api);
            var previewDescription = await TranslateText(MetaInfo.Description, sourceLanguage, targetLanguage, api);
            var keywords = await TranslateStringArray(MetaInfo.Keywords, sourceLanguage, targetLanguage, api);
            var title = await TranslateText(Title, sourceLanguage, targetLanguage, api);
            var urlName = await TranslateText(UrlName, sourceLanguage, targetLanguage, api);
            var previewText = await TranslateText(PreviewText, sourceLanguage, targetLanguage, api);
            blogItem.Text = await TranslateHTML(text, sourceLanguage, targetLanguage, api);

            if (altTag.Success)
                blogItem.AltText = altTag.Text;
            else
                throw altTag.Exception;

            if (previewDescription.Success)
                blogItem.MetaInfo.Description = previewDescription.Text;
            else
                throw previewDescription.Exception;

            if (keywords.Success)
                blogItem.MetaInfo.Keywords = keywords.Text;
            else
                throw keywords.Exception;

            if (title.Success)
                blogItem.Title = title.Text;
            else
                throw title.Exception;

            if (urlName.Success)
            {
                blogItem.UrlName = urlName.Text;

                if (blogItem.UrlName.HasNonASCIIChars())
                {
                    // Now we should use the english one
                    var englishUrlName = await TranslateText(UrlName, sourceLanguage, "en", api);

                    if (!englishUrlName.Success)
                        throw englishUrlName.Exception;
                    else
                        blogItem.UrlName = englishUrlName.Text;
                }

                // Ensure that the urlName is valid after translation
                blogItem.UrlName = blogItem.UrlName.CreateUrlTitle();
            }
            else
                throw urlName.Exception;

            if (previewText.Success)
                blogItem.PreviewText = previewText.Text;
            else
                throw previewText.Exception;

            if (blogItem.UrlName != this.UrlName)
                blogItem.CopyAndFormatImage(originalBlogImage: imageFileName);

            blogItem.ModifiedDate = DateTime.Now;
            return blogItem;
        }

        public JObject ToJObject(Language lang)
        {
            string date = PublishedDate.Date.AddHours(8).ToString(Consts.BlogDateTimeFormat);
            string content = Text.RemoveBom();

            // GUIDs are linked to german blogs
            var assoc = Project.CurrentProject.BlogItems.Where(p => p.LangCode == lang.LangCode).FirstOrDefault();
            if (assoc != null)
            {
                string langRepl = $"{lang.LangCode}/";

                foreach (var blog in assoc.Items)
                {
                    if (!blog.IsTranslated)
                        content = content.Replace(blog.ID.ToString(), $"{Project.CurrentProject.ProjectUrl}/{langRepl}blog/{blog.UrlName.CreateUrlTitle()}");
                    else
                        content = content.Replace(blog.ParentID.ToString(), $"{Project.CurrentProject.ProjectUrl}/{langRepl}blog/{blog.UrlName.CreateUrlTitle()}");
                }
            }

            // Links in Blog Artikel currenlty are https://izoomyou.com/... and instead they should be like /...
            content = content.Replace($"{Project.CurrentProject.ProjectUrl}/", "/");
            content = content.Replace($"{Project.CurrentProject.ProjectUrl}", "/");

            // Generate alternate links
            List<string> alternateLinks = new List<string>();
            string defaultItem = "";
            foreach (var l in Project.CurrentProject.Languages)
            {
                var currentAssoc = Project.CurrentProject.BlogItems.FirstOrDefault(b => b.LangCode == l.LangCode);
                if (currentAssoc == null)
                    continue;

                var item = currentAssoc.Items.FirstOrDefault(i => (i.ParentID == ParentID && ParentID != Guid.Empty) || i.ParentID == ID || i.ID == ID || i.ID == ParentID);
                if (item == null)
                    continue;

                string lnk = item.GenerateLink(l.LangCode, true);
                alternateLinks.Add(lnk);

                if (l.LangCode == "en")
                    defaultItem = item.GenerateLink(l.LangCode, false);
            }

            // x-default language
            alternateLinks.Add($"<link rel='alternate' hreflang='x-default' href='{defaultItem}' />");

            // Generate tags
            List<string> tags = new List<string>();
            foreach (var tagID in Tags)
            {
                var tag = Project.CurrentProject.Tags.Where(t => t.ID == tagID).FirstOrDefault();
                if (tag != null)
                    tags.Add(tag.NameTranslations.Translate(lang.LangCode));
            }

            JObject result = new JObject
            {
                ["h1"] = Title,
                ["meta"] = new JObject()
                {
                    ["title"] = Title,
                    ["description"] = MetaInfo.Description.StripInvalidMetaChars(),
                    ["keywords"] = string.Join(",", MetaInfo.Keywords.StripInvalidMetaChars())
                },
                ["publisher"] = Publisher,
                ["published_date"] = date,
                ["modified_date"] = (ModifiedDate == DateTime.MinValue ? date : ModifiedDate.ToString(Consts.BlogDateTimeFormat)),
                ["url_name"] = UrlName.CreateUrlTitle(),
                ["images"] = new JArray
                {
                    new JObject
                    {
                        ["format"] = System.IO.Path.GetExtension(ImageFileName).Replace(".", string.Empty),
                        ["name"] = ImageFileName
                    },
                    new JObject
                    {
                        ["format"] = "webp",
                        ["name"] = $"{System.IO.Path.GetFileNameWithoutExtension(ImageFileName)}{Consts.WEBP}"
                    }
                },
                ["content"] = content,
                ["alt"] = AltText,
                ["alternate"] = string.Join(Environment.NewLine, alternateLinks),
                ["tags"] = new JArray(tags)
            };

            return result;
        }

        public void CopyAndFormatImage(string newImageFilePath = "", string originalBlogImage = "")
        {
            try
            {
                if (!System.IO.Directory.Exists(Project.CurrentProject.BlogImagesFolder))
                    System.IO.Directory.CreateDirectory(Project.CurrentProject.BlogImagesFolder);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Fehler beim Erstellen des BlogImage-Ordners ({Project.CurrentProject.BlogImagesFolder}): {ex.Message}", "CopyAndFormatImage");
            }

            if (string.IsNullOrEmpty(newImageFilePath) && string.IsNullOrEmpty(originalBlogImage))
            {
                if ((System.IO.Path.GetFileNameWithoutExtension(ImageFileName) == Title.CreateUrlTitle() && HasCustomUrl) || (System.IO.Path.GetFileNameWithoutExtension(ImageFileName) != Title.CreateUrlTitle() && !HasCustomUrl))
                {
                    // Check for normal image
                    string replacementName = UrlName;
                    string oldImagePath = System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, ImageFileName);
                    string newImagePath = System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, replacementName + System.IO.Path.GetExtension(ImageFileName));

                    if (oldImagePath == newImagePath)
                    {
                        Logger.LogDebug($"Bilder sind schon richtig formatiert (\"{oldImagePath}\",\"{newImagePath}\")", "CopyAndFormatImage");
                        return;
                    }

                    try
                    {
                        System.IO.File.Copy(oldImagePath, newImagePath);
                        System.IO.File.Delete(oldImagePath);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Fehler beim Kopieren des Bildes von \"{oldImagePath}\" nach \"{newImagePath}\". Fehler: {ex.Message}", "CopyAndFormatImage");
                    }

                    // Also check for old webP Image
                    oldImagePath = System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, System.IO.Path.GetFileNameWithoutExtension(ImageFileName) + Consts.WEBP);
                    newImagePath = System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, replacementName + Consts.WEBP);

                    try
                    {
                        System.IO.File.Copy(oldImagePath, newImagePath);
                        System.IO.File.Delete(oldImagePath);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Fehler beim Kopieren des Bildes von \"{oldImagePath}\" nach \"{newImagePath}\". Fehler: {ex.Message}", "CopyAndFormatImage");
                    }

                    ImageFileName = replacementName + System.IO.Path.GetExtension(ImageFileName);
                    Project.CurrentProject.Save();
                }
                else
                {
                    // Check if webp-image of this blog exists, otherwise generate it
                    string webpPath = System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, System.IO.Path.GetFileNameWithoutExtension(ImageFileName) + Consts.WEBP);
                    if (!System.IO.File.Exists(webpPath))
                        ImageHelper.CreateWebPImage(System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, ImageFileName), webpPath);
                }
            }
            else if (!string.IsNullOrEmpty(originalBlogImage))
            {
                // Step 1) Set ImageFileName to UrlName
                ImageFileName = UrlName + System.IO.Path.GetExtension(originalBlogImage);
                Project.CurrentProject.Save();

                // Step 2) Prepare to copy files
                Dictionary<string, string> copyFiles = new Dictionary<string, string>
                {
                    {
                        System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, originalBlogImage),
                        System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, ImageFileName)
                    },
                    {
                        System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, System.IO.Path.GetFileNameWithoutExtension(originalBlogImage) + Consts.WEBP),
                        System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, System.IO.Path.GetFileNameWithoutExtension(ImageFileName) + Consts.WEBP)
                    }
                };


                // Step 3) Copy png/jpeg and webp but with blogItem.Urlname
                foreach (var path in copyFiles.Keys)
                {
                    try
                    {
                        System.IO.File.Copy(path, copyFiles[path], true);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Fehler beim Kopieren des Bildes \"{path}\" nach \"{copyFiles[path]}\". Fehler: {ex.Message}", "CopyAndFormatImage");
                    }
                }

            }
            else if (!string.IsNullOrEmpty(newImageFilePath))
            {
                // 1) Delete old blog image (webp and other one)
                string[] filesToDelete = new string[]
                {
                        System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, ImageFileName),
                        System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, $"{System.IO.Path.GetFileNameWithoutExtension(ImageFileName)}{Consts.WEBP}"),
                };
                
                try
                {
                    foreach (var file in filesToDelete)
                        if (System.IO.File.Exists(file))
                            System.IO.File.Delete(file);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Fehler beim Löschen der alten Blog-Bilder: {ex.Message}", "CopyAndFormatImage");
                }

                string fileName = $"{UrlName}{System.IO.Path.GetExtension(newImageFilePath)}";
                string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
                string targetPath = System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, fileName);

                try
                {
                    // 2) Copy blog image
                    System.IO.File.Copy(newImageFilePath, targetPath);

                    // 3) Create webP Image
                    ImageHelper.CreateWebPImage(targetPath, System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, $"{fileNameWithoutExt}{Consts.WEBP}"));

                    // 4) Set new ImageFileName
                    ImageFileName = fileName;
                    Project.CurrentProject.Save();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Fehler beim Kopieren des Bildes \"{newImageFilePath}\" nach \"{targetPath}\": {ex.Message}", "CopyAndFormatImage");
                }
            }
        }

        public bool DeleteBlogImages()
        {
            try
            {
                if (!string.IsNullOrEmpty(ImageFileName))
                {
                    // If a blog image is used in multiple blogs (because it's translation is equal, e.g. like insta-zoom),
                    // we cannot delete this picture/file!!!
                    if (Project.CurrentProject.BlogItems.SelectMany(p => p.Items).Count(b => b.ImageFileName == ImageFileName) > 1)
                    {
                        Logger.LogInformation($"Das Bild \"{ImageFileName}\" wurde nicht gelöscht, da es in mehreren Blog-Artikeln verwendet wird!", "DeleteImage");
                        return false;
                    }

                    string path = System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, ImageFileName);
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);

                    string webPPath = System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, System.IO.Path.GetFileNameWithoutExtension(ImageFileName) + Consts.WEBP);
                    if (System.IO.File.Exists(webPPath))
                        System.IO.File.Delete(webPPath);

                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Fehler beim Löschen von \"{ImageFileName}\": {ex.Message}", "DeleteImage");
            }

            return false;
        }

        public bool ContainsInvalidUrls(out string report)
        {
            return ContainsInvalidUrls(Text, out report);
        }

        public static bool ContainsInvalidUrls(string text, out string report)
        {
            report = string.Empty;

            if (string.IsNullOrEmpty(text))
                return true;

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(text);

            List<UrlValidationResult> result = new List<UrlValidationResult>();
            List<string> urls = new List<string>();

            foreach (var node in htmlDoc.DocumentNode.DescendantsAndSelf())
            {
                if (node.Name == "a")
                {
                    string url = node.Attributes["href"].Value;
                    var validationResult = url.ValidateUrl();
                    if (validationResult == UrlValidationResult.Success)
                        continue;
                    else
                    {
                        result.Add(validationResult);
                        urls.Add(url);
                    }                 
                }
            }


            if (result.Count == 0)
                return true;
            else
            {
                // Generate report
                for (int i = 0; i < result.Count; i++)
                {
                    string currentUrl = urls[i];
                    var currentResult = result[i];
                    int lineNumber = text.DetermineLineNumber(currentUrl);

                    report += $"Zeile: {lineNumber}\nUrl: \"{currentUrl}\"\nFehler: {currentResult}\n\n";
                }

                return false;
            }
        }

        public string GenerateLink(string lang, bool isAlternate = false)
        {
            string url = $"{Project.CurrentProject.ProjectUrl}/{lang}/blog/{UrlName.CreateUrlTitle()}/";

            if (isAlternate)
                return $"<link rel='alternate' hreflang='{lang}' href='{url}' />";

            return url;
        }

        public object Clone()
        {
            return new BlogItem()
            {
                AltText = this.AltText,
                ImageFileName = this.ImageFileName,
                MetaInfo = (BlogMeta)this.MetaInfo.Clone(),
                PreviewText = this.PreviewText,
                PublishedDate = this.PublishedDate,
                Publisher = this.Publisher,
                Text = this.Text,
                Title = this.Text,
                UrlName = this.UrlName,
                HasCustomUrl = this.hasCustomUrl,
                IsTranslated = this.IsTranslated,
                ModifiedDate = this.ModifiedDate
            };
        }

        public override string ToString()
        {
            return UrlName;
        }
    }
}
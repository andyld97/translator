using Translator.Helper;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Translator.Model.Blog
{
    public class BlogItemPreview
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("preview_text")]
        public string PreviewText { get; set; }

        [JsonProperty("preview_images")]
        public List<BlogImage> PreviewImages { get; set; } = new List<BlogImage>();

        [JsonProperty("alt")]
        public string AltText { get; set; }

        [JsonProperty("url_name")]
        public string UrlName { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        public static BlogItemPreview FromBlogItem(BlogItem item, Language lang, bool ignoreTags = false)
        {
            var bip = new BlogItemPreview()
            {
                PreviewText = item.PreviewText.ReplaceLineBreakToBr(),
                Title = item.Title,
                AltText = item.AltText,
                UrlName = item.UrlName
            };

            if (!ignoreTags)
            {
                List<string> tags = new List<string>();
                foreach (var tagID in item.Tags)
                {
                    var tag = Project.CurrentProject.Tags.Where(t => t.ID == tagID).FirstOrDefault();
                    if (tag != null)
                        tags.Add(tag.NameTranslations.Translate(lang.LangCode));
                }

                bip.Tags = tags;
            }
            else
                bip.Tags = null;

            bip.PreviewImages.Add(new BlogImage("jpeg", item.ImageFileName));

            string webpFileName = $"{System.IO.Path.GetFileNameWithoutExtension(item.ImageFileName)}{Consts.WEBP}";
            if (System.IO.File.Exists(System.IO.Path.Combine(Project.CurrentProject.BlogImagesFolder, webpFileName)))
                bip.PreviewImages.Add(new BlogImage("webp", webpFileName));

            return bip;
        }

        public JObject ToJObject(bool ignoreTags = false)
        {
            var result = new JObject
            {
                ["title"] = Title,
                ["preview_text"] = PreviewText,
                ["alt_text"] = AltText,
                ["url_name"] = UrlName,
            };

            if (!ignoreTags)
                result["tags"] = new JArray(Tags);

            var imgResult = new JArray();
            foreach (var img in PreviewImages)
            {
                imgResult.Add(new JObject 
                {
                    ["format"] = img.Format,
                    ["name"] = img.ImageFileName,
                });
            }

            result["preview_images"] = imgResult;
            return result;
        }
    }
}

using Translator.Model.Blog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Translator.Model.Json
{
    public class JsonPage
    {
        [JsonProperty(PropertyName = "page")]
        public JObject Page { get; set; } = new JObject();

        [JsonProperty(PropertyName = "general")]
        public JObject General { get; set; } = new JObject();

        /*[JsonProperty(PropertyName = "blog", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public JObject Blog { get; set; } = null;

        [JsonProperty(PropertyName = "blog_preview", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<BlogItemPreview> BlogPreviews { get; set; } = null;*/
    }
}

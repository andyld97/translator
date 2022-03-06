using Newtonsoft.Json;

namespace Translator.Model.Blog
{
    public class BlogImage
    {
        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("image")]
        public string ImageFileName { get; set; }

        public BlogImage()
        {

        }

        public BlogImage(string format, string imageFileName)
        {
            this.Format = format;
            this.ImageFileName = imageFileName;
        }
    }
}
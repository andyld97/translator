using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Translator.Helper
{
    public static class StringHelper
    {
        #region Definitions

        private static readonly Dictionary<string, string> variants = new Dictionary<string, string>()
        {
            { "<ol>\r\n<li>", "<ol><li>" },
            { "<ol>\r<li>", "<ol><li>" },
            { "<ol>\n<li>", "<ol><li>" },

            { "<ul>\r\n<li>", "<ul><li>" },
            { "<ul>\r<li>", "<ul><li>" },
            { "<ul>\n<li>", "<ul><li>" },

            { "</li>\r\n<li>", "</li><li>" },
            { "</li>\n<li>", "</li><li>" },
            { "</li>\r<li>", "</li><li>" },

            { "</li>\r\n</ol>", "</li></ol>" },
            { "</li>\n</ol>", "</li></ol>" },
            { "</li>\r</ol>", "</li></ol>" },

            { "</li>\r\n</ul>", "</li></ul>" },
            { "</li>\n</ul>", "</li></ul>" },
            { "</li>\r</ul>", "</li></ul>" },

            { "\r\n", "<br/>" },
            { "\n", "<br/>" },
            { "\r", "<br/>" },
       };

        private static readonly string[] invalidChars = new string[]
        {
            ".",
            "。",
            "?",
            "？",
            "!",
            "！", 
            ";",
            ":",
            "：",
            ",",
            "+",                
            "&",
            "%",
            "'",
            "´",
            "`",
            "#",
            "\""
        };

        private static readonly Dictionary<string, string> replacer = new Dictionary<string, string>()
        {
            { "ä", "ae" },
            { "ü", "ue" },
            { "ö", "oe" },
            { "Ä", "Ae" },
            { "Ü", "Ue" },
            { "Ö", "Oe" },
            { "ß", "ss" },
            { ".", "-" }
        };

        #endregion

        public static string ReplaceLineBreakToBr(this string input)
        {
            if (input == "-")
                return string.Empty;

            string result = input;
            foreach (var key in variants.Keys)
                result = result.Replace(key, variants[key]);

             return Regex.Replace(result, @"\t|\n|\r", "<br/>");
        }

        public static bool HasNonASCIIChars(this string str)
        {
            return System.Text.Encoding.UTF8.GetByteCount(str) != str.Length;
        }

        public static string[] GetKeywords(this string input, char delimitter = ',')
        {
            if (string.IsNullOrEmpty(input))
                return new string[] { };

            if (!input.Contains(delimitter.ToString()))
                return new string[] { input.Trim() };

            string[] result =  input.Split(delimitter);
            for (int i = 0; i < result.Length; i++)
                result[i] = result[i].Trim();

            return result;
        }

        public static string CreateUrlTitle(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Thats must be happen before regex strips of ä,ü or ö ...
            foreach (var key in replacer.Keys)
                input = input.Replace(key, replacer[key]);

            input = Regex.Replace(input, @"[^\P{Cc}\t\r\n]", string.Empty); //   @"[^\u0000-\u007F]+", string.Empty);
            input = input.Trim();

            input = input.ToLower().Replace(" ", "-");

            foreach (var c in invalidChars)
                input = input.Replace(c, string.Empty);

            if (input.EndsWith("-"))
                input = input.Substring(0, input.Length - 1);

            return input.Replace("---", "-").Replace("--", "-").CleanUp();
        }

        public static string StripInvalidMetaChars(this string input)
        {
            foreach (var c in Consts.InvalidMetaChars)
                input = input.Replace(c.ToString(), string.Empty);

            return input;            
        }

        /// <summary>
        /// You can get this or test it originally with: Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble())[0];
        /// But no need, this way we have a constant. As these three bytes `[239, 187, 191]` (a BOM) evaluate to a single C# char.
        /// </summary>
        public const char BOMChar = (char)65279;

        public static bool FixBOMIfNeeded(ref string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;

            bool hasBom = str[0] == BOMChar;
            if (hasBom)
                str = str[1..];

            return hasBom;
        }

        public static string RemoveBom(this string p)
        {
            if (string.IsNullOrEmpty(p))
                return p;

            bool hasBom = p[0] == BOMChar;
            if (hasBom)
                p = p[1..];

            return p;
        }

        public static string[] StripInvalidMetaChars(this string[] input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                foreach (var c in Consts.InvalidMetaChars)
                    input[i] = input[i].StripInvalidMetaChars();
            }

            return input;
        }

        public static string CleanUp(this string input)
        {
            string value = input;
            value = value.Replace("&#39;", "'");
            value = value.Replace("#39;", "'");

            return value;
        }

        public static string[] CleanUp(this string[] input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = input[i].Replace("&#39;", "'");
                input[i] = input[i].Replace("#39;", "'");
            }

            return input;
        }

        public static int DetermineLineNumber(this string source, string search, string newLine = "\n")
        {
            // THIS METHOD COULD DELIEVER WRONGS RESULTS,
            // if you search a string which source contains multiple times!!!!!!

            // Remove empty entries is not allowed, because there can be empty lines,
            // and if they are removed, the line-number is wrong
            string[] lines = source.Split(newLine.ToCharArray(), System.StringSplitOptions.None);

            for (int line = 0; line < lines.Length; line++)
            {
                if (lines[line].Contains(search))
                    return line + 1;
            }

            return -1;
        }
    }
}
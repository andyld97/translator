using System.Collections.Generic;
using System.Linq;

namespace Translator.Model.Templates
{
    public static class Templates
    {
        public static readonly Template DefaultTempalte = new Template()
        {
            Name = "Default Template",
            GoogleCloudProjectName = "translator-285319",
            DeepLKey = "0348c21c-86b9-4d9f-f99d-a68f304dd661",
            TelegramUrl = "https://service.izoomyou.com/bot/telegram/translator.php",
            GoogleCloudProjectFileContent = "{\n\"type\": \"service_account\",\n\"project_id\": \"translator-285319\",\n\"private_key_id\": \"069cad851a2a272f2960c1981104e4134f45da40\",\n\"private_key\": \"-----BEGIN PRIVATE KEY-----~MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQDXQKoV5flGPOpl~NZDZP/ogR9NbMT0TtAN4TSligFWzYWh+TdAnWB/JxaMYBacPvpINVjf3Tp2G4eun~LIdrYZsbT8T9Xo1RBS+aAQp13KT0UEokcGvJ5wqZnXDY8AaSUOv+mM0L7ZJNfRdx~eQ7i1MKEvtPl3xYBVOunZIQAcaDUAXpHCrAO/uJKLAxr1C0RgbN5hOyKEqfVtnU5~txY1EikyIYL5TYmtsglIQik/aEnUUo8E2gtQ7fDXOI1yIQPnxU/0fxfviC1siuSy~OdrzWOhzLlLrXjMO7GKy2vQhwK1Fn9W7MSGHqs+BRZyf7MTuhdaMv8l5KFaj/5J/~X3xr8ANjAgMBAAECggEAVIN/pGxwlIrLTySadCoh53W7/vSUsf0+VYgaRSH1RCJt~OBYOMbxwcrTmBcMGZnChAJqHC9Myl2hxsWgS3W7ryuvlgMOUgaijGXjqJf+VRpWV~nrwDHjlYGJtP2OVY9y7Nkd9ltpa3GDSStUteNGJr41nXccxG5Av3IHifOVtj1Yjs~iv3xKNhUFpFOJ9ppfPNSN01vNn7d6MOpslQo57e/ahAOVyFpvIMRasrU7hSQQKCE~oNxXOlT9FmQy7xoa3VCrSRTeFiA6K3p85n2+uHir7FUy4YCM3e16MVzbDXoR/oyA~d0SChTceGeF3HYgnz7oLHZq3UHJpN+E8bvbV7gmT2QKBgQDzANX0nItCzGmUsv/r~vnOgqd1hwqZlfXWTziIhym7COAZlfyfg3xGjeWq1j17jKNJbDZ/sr6CEZSBJeNO4~wx2JjFVLhhJYhy1PBicaEWiDNed47xYILeAp6sGkNFcjg3LbsIZQqL9GdknfWbPL~7ZJ7RJib73kQACsQmy6UeHXvSwKBgQDiw97htnADIWZu5U9ZdjnpYLPwMuAJEAsr~/L9jsjgVC3O9qZX9HWTW3l1IQGuNtR5iBn0rWl6gRUrEkfy7kQX8LuLOdN5YyAyw~/zwQarewFHNEJzJ4YtzLUgy+HPJlOZeiOuPJDwRBU4j2xiNFTY79G+6PPUUaytNE~AJUfUWT1SQKBgAOW15ItXcE207hmjHEm9v5AhAyVm2+UUtBEEyz8mHY17aJCJoyj~vtbzTCgyXextBe5iXSJZ1b0e4UM0jawE9cK6V+gtqsez929bX+h6qViGy0x1+5VT~WCRGW2XZgA/+OQwVp2Y5l9mqlZy+7nDsqWU4tihXeSpLVleAc0euH8/RAoGBAJTH~/JlNMMrtBB8odtp0lmSH3SdwyctIanwO1afcy60LGYJMHSu4OGw98ygvlCGivu+D~4GYsYb94FylHu3F5IlsmjAr3ZNcNUj5jCA6hZimyETqbGSMhgkooaFHn/iXqFpIL~X16QarNN0qROtd+HlpR82hXDFm8QunJ4i17D8aB5AoGBAJnNdGlRKprD5FPQaLYa~tf9mun/RPkYT4VPtlBI0wRg5BDds53BLql9uLvR8sX40VL1eQ4lqNESFk5Giwn6n~N6UgSAZKk/47udHKnXM3DtJgZkVG+hL+u3FAaYXvqKyU6Z3NSOyRYSRG+nWLbizr~T5KAMR/3NZomwqyUQRqgfsxj~-----END PRIVATE KEY-----~\",\n\"client_email\": \"test-701@translator-285319.iam.gserviceaccount.com\",\n\"client_id\": \"103844805851646502487\",\n\"auth_uri\": \"https://accounts.google.com/o/oauth2/auth\",\n\"token_uri\": \"https://oauth2.googleapis.com/token\",\n\"auth_provider_x509_cert_url\": \"https://www.googleapis.com/oauth2/v1/certs\",\n\"client_x509_cert_url\": \"https://www.googleapis.com/robot/v1/metadata/x509/test-701%40translator-285319.iam.gserviceaccount.com\"\n}",
            Pages = new List<string>() { "index", "legal_notice", "privacy_policy", "faq", "404", "blog" },
            Languages = new List<Language>() { new Language("Deutsch", "de"), new Language("English", "en") },
            TemplateScopes = new List<TemplateScope>()
            {
                new TemplateScope()
                {
                    Name = "meta",
                    ParentPages = null,
                    IsUniqueOnEveryPage = true,
                    ScopeType = ScopeType.SinglePage,
                    UniqueItems = new List<TemplateItem>()
                    {
                        new TemplateItem()
                        {
                            Name = "title",
                            Value = "${page_name}",
                        },
                        new TemplateItem() { Name = "description" },
                        new TemplateItem() { Name = "keywords" },
                    }
                },
                new TemplateScope()
                {
                    Name = "google",
                    ParentPages = null,
                    IsUniqueOnEveryPage = true,
                    ScopeType = ScopeType.SinglePage,
                    UniqueItems = new List<TemplateItem>()
                    {
                        new TemplateItem() { Name = "name", Value = "${page_name}" },
                        new TemplateItem() { Name = "description" },
                        new TemplateItem() { Name = "image" },
                    }
                },
                new TemplateScope()
                {
                    Name = "facebook",
                    ParentPages = null,
                    ScopeType = ScopeType.SinglePage,
                    IsUniqueOnEveryPage = true,
                    UniqueItems = new List<TemplateItem>()
                    {
                        new TemplateItem() { Name = "title", Value = "${page_name}" },
                        new TemplateItem() { Name = "description" },
                        new TemplateItem() { Name = "type" },
                        new TemplateItem() { Name = "url", Value = "${project_url}" },
                        new TemplateItem() { Name = "image" },
                    }
                },
                new TemplateScope()
                {
                    Name = "menu",
                    ScopeType = ScopeType.General,
                },
            },
            TemplateItems = new List<TemplateItem>()
            {
                new TemplateItem() { Name = "home", ItemType = ItemType.Scopes, ParentScopes = new List<string>() { "menu" }, Value = "Startseite" },
                new TemplateItem() { Name = "h1", ItemType = ItemType.GeneralPage, Value = "${page_name}" },
                new TemplateItem() { Name = "sitename", ItemType = ItemType.GeneralGeneral, Value = "${project_name}" },
                new TemplateItem() { Name = "siteurl", ItemType = ItemType.GeneralGeneral, Value = "${project_url}" },
                new TemplateItem() { Name = "google_analytics", ItemType = ItemType.GeneralGeneral, IsHtml = true }
            }
        };

        public static readonly Template[] All = new Template[] { DefaultTempalte };

        public static Template FindTemplate(string name)
        {
            if (string.IsNullOrEmpty(name))
                return new Template();

            var template = All.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
            if (template == null)
                return new Template();

            return template;
        }
    }
}

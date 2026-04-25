using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace JustGoAPI.Shared.Helper
{
    public class Utilities
    {
        private static readonly Regex IllegalCharacterRegex = new Regex(@"[<>:""/\\|?*']", RegexOptions.Compiled);
        public static string GetDeviceType(IHttpContextAccessor httpContextAccessor)
        {
            var userAgent = httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();

            if (userAgent.Contains("iPhone"))
                return "Mobile (iPhone, iOS)";
            if (userAgent.Contains("Android") && userAgent.Contains("Mobile"))
                return "Mobile (Android)";
            if (userAgent.Contains("Android"))
                return "Tablet (Android)";
            if (userAgent.Contains("iPad"))
                return "Tablet (iPad, iOS)";
            if (userAgent.Contains("Windows NT") || userAgent.Contains("Mac OS X") || userAgent.Contains("Linux"))
                return "Desktop";

            return "Other";
        }
        public static (string Browser, string Version) GetBrowserInfo(IHttpContextAccessor httpContextAccessor)
        {
            var userAgent = httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();

            if (!string.IsNullOrEmpty(userAgent))
            {
                var browserInfo = ParseUserAgent(userAgent);
                return browserInfo;
            }

            return ("Unknown", "Unknown");
        }

        private static (string Browser, string Version) ParseUserAgent(string userAgent)
        {
            if (userAgent.Contains("Firefox"))
            {
                var match = Regex.Match(userAgent, @"Firefox/(\d+(\.\d+)*)");
                if (match.Success)
                {
                    return ("Firefox", match.Groups[1].Value);
                }
            }
            else if (userAgent.Contains("Chrome"))
            {
                var match = Regex.Match(userAgent, @"Chrome/(\d+(\.\d+)*)");
                if (match.Success)
                {
                    return ("Chrome", match.Groups[1].Value);
                }
            }
            else if (userAgent.Contains("Safari") && userAgent.Contains("Version"))
            {
                var match = Regex.Match(userAgent, @"Version/(\d+(\.\d+)*) Safari");
                if (match.Success)
                {
                    return ("Safari", match.Groups[1].Value);
                }
            }
            else if (userAgent.Contains("Edg"))
            {
                var match = Regex.Match(userAgent, @"Edg/(\d+(\.\d+)*)");
                if (match.Success)
                {
                    return ("Edge", match.Groups[1].Value);
                }
            }

            return ("Unknown", "Unknown");
        }
        public static string GetPropertyValueByName(object obj, string propName)
        {
            string propVal = obj.GetType().GetProperty(propName).GetValue(obj, null)?.ToString();
            return propVal;
        }
        public static string GetJObjectPropertyValueByName(JObject obj, string propName)
        {
            if (obj.TryGetValue(propName, out JToken value))
            {
                return value?.ToString();
            }
            return null;
        }

        public static string GetEnumText<TEnum>(TEnum value) where TEnum : Enum
        {
            FieldInfo field = value.GetType().GetField(value.ToString());
            DescriptionAttribute attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }

        public static List<KeyValuePair<int, string>> GetEnumDropdown<TEnum>() where TEnum : Enum
        {
            var list = new List<KeyValuePair<int, string>>();
            foreach (TEnum value in Enum.GetValues(typeof(TEnum)))
            {
                int intValue = Convert.ToInt32(value);
                string text = GetEnumText(value);
                list.Add(new KeyValuePair<int, string>(intValue, text));
            }
            return list;
        }

        public static bool IsValidSqlIdentifier(string name)
        {
            // Regex for basic SQL identifier rules
            Regex ValidSqlName = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Check against SQL keyword list if needed (optional)
            // For now, we just validate character rules
            return ValidSqlName.IsMatch(name);
        }

        public static string SanitizeFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException("Filename cannot be null or empty.");

            string sanitizedFilename = IllegalCharacterRegex.Replace(filename, "_");
            if (string.IsNullOrWhiteSpace(sanitizedFilename))
                throw new ArgumentException("Filename cannot be empty after sanitization.");

            return sanitizedFilename;
        }

        public static string GetUniqName(string fileName = "", bool addPrefix = true, string format = ".png")
        {
            if (!format.Contains("."))
                format = "." + format;

            return string.IsNullOrEmpty(fileName)
                ? $"{Guid.NewGuid()}{format}"
                : (addPrefix ? $"Prefix_{Guid.NewGuid()}_$_{SanitizeFilename(fileName)}" : SanitizeFilename(fileName));
        }

        public static string ResolveDirectUploadPath(string t, string fileName, string customStorePath = "")
        {
            if (string.IsNullOrWhiteSpace(t) || string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Type and fileName must be provided.");

            t = t.ToLowerInvariant();

            // Dictionary for simple mappings
            var pathMap = new Dictionary<string, string>
            {
                ["eventcategory"] = $"media/images/events/{fileName}",
                ["license"] = $"media/images/licenses/{fileName}",
                ["login"] = $"media/images/login/{fileName}",
                ["organizationlogo"] = $"media/images/organization/logo/{fileName}",
                ["organizationloginbg"] = $"media/images/organization/LoginBg/{fileName}",
                ["organizationeventheroimage"] = $"media/images/organization/EventHeroImage/{fileName}",
                ["organizationeventdefaultimage"] = $"media/images/organization/EventDefaultImage/{fileName}",
                ["organizationshopdefaultimage"] = $"media/images/organization/ShopDefaultImage/{fileName}",
                ["organizationshopheroimage"] = $"media/images/organization/ShopHeroImage/{fileName}",
                ["organizationheroimage"] = $"media/images/organization/HeroImage/{fileName}",
                ["resourcewebsite"] = $"media/images/resource/website/{fileName}",
                ["eula"] = $"media/eula/{fileName}",
                ["email"] = $"FroalaAttachments/{fileName}",
                ["fm_content"] = $"FroalaAttachments/{fileName}",
                ["fieldmanagementattach"] = $"fieldmanagementattachment/attachments/{fileName}",
                ["justgobookingtattach"] = $"justgobookingtattachment/attachments/{fileName}",
                ["competitionattachment"] = $"competitionattachment/attachments/{fileName}",
            };

            if (pathMap.TryGetValue(t, out var mappedPath))
                return mappedPath;

            if (t == "mailattachment")
            {
                var parts = fileName.Split(new[] { "_$_" }, StringSplitOptions.None);
                if (parts.Length < 2)
                    throw new ArgumentException("Invalid fileName format for mailattachment.");
                return $"media/images/mailattachment/{parts[0]}/{parts[1]}";
            }

            if (t == "custom")
            {
                if (string.IsNullOrEmpty(customStorePath))
                    throw new ArgumentException("customStorePath must be provided for custom type.");
                return $"{customStorePath}{fileName}";
            }

            return string.Empty;
        }

        public static string ResolveTempUploadPath(string t, string fileName)
        {
            if (string.IsNullOrWhiteSpace(t) || string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Type and fileName must be provided.");

            t = t.ToLowerInvariant();

            var pathMap = new Dictionary<string, string>
            {
                ["user"] = $"Temp/User/{fileName}",
                ["repo"] = $"Temp/Repository/{fileName}",
                ["repoattach"] = $"Temp/Repository/attachments/{fileName}",
                ["fieldmanagementattach"] = $"Temp/fieldmanagementattachment/attachments/{fileName}",
                ["emailandcommunicationtemplateattachments"] = $"Temp/{fileName}",
                ["import"] = $"import/{fileName}",
                ["default"] = $"Temp/default/{fileName}",
                ["processingscheme"] = $"Temp/processingscheme/{fileName}",
                ["custom"] = $"Temp/custom/{fileName}",
                ["wallettemplatelogo"] = $"Temp/WalletTemplate/Logo/{fileName}",
                ["wallettemplatehero"] = $"Temp/WalletTemplate/Hero/{fileName}",
                ["justgobookingattachment"] = $"Temp/justgobookingattachment/attachments/{fileName}",
                ["competitionattachment"] = $"Temp/competitionattachment/attachments/{fileName}",
            };

            if (pathMap.TryGetValue(t, out var mappedPath))
                return mappedPath;

            if (t == "emailandcommunicationattachments")
            {
                var split = fileName.Split('$');
                if (split.Length == 0)
                    throw new ArgumentException("Invalid fileName format for emailandcommunicationattachments.");
                return $"Temp/{split[0]}$_/{fileName.Substring(split[0].Length + 2)}";
            }

            return string.Empty;
        }

        static string[] allowedExtensions = {
                                                             ".jpg", ".jpeg", ".tiff", ".tif", ".gif", ".png", ".bmp", ".ico",
                                                             ".doc", ".docx",".pdf",".rtf",".txt",".xml",
                                                             ".aif", ".mp3",".mpa",".ogg",".wav",".wma",
                                                             ".pps", ".ppt",".pptx",
                                                             ".xls", ".xlsx" ,".csv",
                                                             ".7z", ".rar" ,".tar.gz",".z",".zip",
                                                             ".flv", ".mp4" , ".mpeg", ".mpg", ".seq"
                                                          };
        public static bool IsFileAllowed(string path)
        {
            var extension = Path.GetExtension(path);
            if (extension == null) return false;
            return allowedExtensions.Contains(extension.ToLower());
        }
    }
}

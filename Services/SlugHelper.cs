using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CuaHangQuanAo.Services
{
    public static class SlugHelper
    {
        public static string ToSlug(this string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return string.Empty;
            }
            title = title.ToLowerInvariant();
            title = title.Replace("đ", "d");
            title = Regex.Replace(title, @"[ôốồổỗộơớởờỡợ]", "o");
            title = title.Normalize(NormalizationForm.FormD)  // Decompose characters (like é -> e)
                                 .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)  // Remove diacritic marks
                                 .Aggregate(string.Empty, (current, c) => current + c);
            title = Regex.Replace(title, @"[^a-z0-9\s-]", "").Trim().Replace(' ', '-');
            title = Regex.Replace(title, @"-+", "-");
            return title;
        }
    }
}

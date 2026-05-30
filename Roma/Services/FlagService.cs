using System;
using System.Collections.Generic;

namespace Roma.Services
{
    public static class FlagService
    {
        // Mapping of RAGE:MP language codes to ISO 3166-1 alpha-2 country codes
        private static readonly Dictionary<string, string> _languageToCountryCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "EN", "gb" },      // English -> Great Britain
            { "GB", "gb" },
            { "US", "en" },
            { "RU", "ru" },      // Russian -> Russia
            { "DE", "de" },      // German -> Germany
            { "ES", "es" },      // Spanish -> Spain
            { "PT", "pt" },      // Portuguese -> Portugal
            { "FR", "fr" },      // French -> France
            { "IT", "it" },      // Italian -> Italy
            { "PL", "pl" },      // Polish -> Poland
            { "TR", "tr" },      // Turkish -> Turkey
            { "BR", "br" },      // Brazilian -> Brazil
            { "UA", "ua" },      // Ukrainian -> Ukraine
            { "CZ", "cz" },      // Czech -> Czechia
            { "RO", "ro" },      // Romanian -> Romania
            { "AR", "sa" },      // Arabic -> Saudi Arabia (keeping first AR entry)
            { "CN", "cn" },      // Chinese -> China
            { "JP", "jp" },      // Japanese -> Japan
            { "KR", "kr" },      // Korean -> South Korea
            { "NL", "nl" },      // Dutch -> Netherlands
            { "SE", "se" },      // Swedish -> Sweden
            { "NO", "no" },      // Norwegian -> Norway
            { "DK", "dk" },      // Danish -> Denmark
            { "FI", "fi" },      // Finnish -> Finland
            { "GR", "gr" },      // Greek -> Greece
            { "HU", "hu" },      // Hungarian -> Hungary
            { "SK", "sk" },      // Slovak -> Slovakia
            { "BG", "bg" },      // Bulgarian -> Bulgaria
            { "HR", "hr" },      // Croatian -> Croatia
            { "SR", "rs" },      // Serbian -> Serbia
            { "SI", "si" },      // Slovenian -> Slovenia
            { "LT", "lt" },      // Lithuanian -> Lithuania
            { "LV", "lv" },      // Latvian -> Latvia
            { "EE", "ee" },      // Estonian -> Estonia
            { "IN", "in" },      // Hindi -> India
            { "TH", "th" },      // Thai -> Thailand
            { "VN", "vn" },      // Vietnamese -> Vietnam
            { "ID", "id" },      // Indonesian -> Indonesia
            { "MY", "my" },      // Malay -> Malaysia
            { "PH", "ph" },      // Filipino -> Philippines
            { "SA", "sa" },      // Arabic -> Saudi Arabia
            { "AE", "ae" },      // Arabic -> UAE
            { "IL", "il" },      // Hebrew -> Israel
            { "ZA", "za" },      // Afrikaans -> South Africa
            { "MX", "mx" },      // Spanish (Mexico) -> Mexico
            { "CL", "cl" },      // Spanish (Chile) -> Chile
            { "CO", "co" },      // Spanish (Colombia) -> Colombia
            { "PE", "pe" },      // Spanish (Peru) -> Peru
            { "VE", "ve" },      // Spanish (Venezuela) -> Venezuela
        };

        /// <summary>
        /// Gets the flag image URL from flagcdn.com for a given language code
        /// </summary>
        /// <param name="languageCode">Language code (e.g., "EN", "RU", "DE")</param>
        /// <param name="width">Flag width in pixels (default: 16)</param>
        /// <param name="height">Flag height in pixels (default: 12)</param>
        /// <returns>URL to the flag image, or a default world flag if not found</returns>
        public static string GetFlagUrl(string languageCode, int width = 20, int height = 15)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                return GetDefaultFlagUrl(width, height);
            }

            var code = languageCode.ToUpperInvariant();

            if (_languageToCountryCode.TryGetValue(code, out var countryCode))
            {
                return $"https://flagcdn.com/{width}x{height}/{countryCode}.png";
            }

            return GetDefaultFlagUrl(width, height);
        }

        /// <summary>
        /// Returns a default world/globe flag URL for unknown languages
        /// </summary>
        private static string GetDefaultFlagUrl(int width, int height)
        {
            // Using the UN flag as a default "world" flag
            return $"https://flagcdn.com/{width}x{height}/un.png";
        }
    }
}

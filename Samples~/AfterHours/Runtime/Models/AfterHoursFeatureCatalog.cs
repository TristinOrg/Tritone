using System;
using System.Collections.Generic;
using UnityEngine;

namespace AfterHours.Models
{
    /// <summary>Localized, data-backed index for every capability demonstrated by After Hours.</summary>
    public sealed class AfterHoursFeatureCatalog
    {
        [Serializable]
        private sealed class Payload { public Feature[] Features; }

        [Serializable]
        /// <summary>Describes one localized scene or editor workflow.</summary>
        public sealed class Feature
        {
            public string Id;
            public string GroupId;
            public string GroupTitleEn;
            public string GroupTitleZh;
            public string GroupTitleJa;
            public string TitleEn;
            public string TitleZh;
            public string TitleJa;
            public string SummaryEn;
            public string SummaryZh;
            public string SummaryJa;
            public string PrerequisiteEn;
            public string PrerequisiteZh;
            public string PrerequisiteJa;
            public string ScenePath;
            public string EditorMenuPath;
            public string ActionEn;
            public string ActionZh;
            public string ActionJa;

            public string GroupTitle(string locale) => Localize(locale, GroupTitleEn, GroupTitleZh, GroupTitleJa);
            public string Title(string locale) => Localize(locale, TitleEn, TitleZh, TitleJa);
            public string Summary(string locale) => Localize(locale, SummaryEn, SummaryZh, SummaryJa);
            public string Prerequisite(string locale) => Localize(locale, PrerequisiteEn, PrerequisiteZh, PrerequisiteJa);
            public string ActionLabel(string locale) => Localize(locale, ActionEn, ActionZh, ActionJa);
        }

        private readonly Feature[] mFeatures;
        private AfterHoursFeatureCatalog(Feature[] features) { mFeatures = features; }
        public IReadOnlyList<Feature> Features => mFeatures;

        public Feature Find(string id) => Array.Find(mFeatures, feature => feature.Id == id);

        public static AfterHoursFeatureCatalog Load()
        {
            var asset = Resources.Load<TextAsset>("FeatureIndex/feature-index");
            if (asset == null)
            {
                Debug.LogError("After Hours Feature Index is missing. Re-import the After Hours sample.");
                return new AfterHoursFeatureCatalog(Array.Empty<Feature>());
            }

            var payload = JsonUtility.FromJson<Payload>(asset.text);
            return new AfterHoursFeatureCatalog(payload?.Features ?? Array.Empty<Feature>());
        }

        public static string CurrentLocale()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return "zh-CN";
                case SystemLanguage.Japanese:
                    return "ja-JP";
                default:
                    return "en-US";
            }
        }

        private static string Localize(string locale, string english, string chinese, string japanese)
        {
            if (locale == "zh-CN" && !string.IsNullOrEmpty(chinese)) return chinese;
            if (locale == "ja-JP" && !string.IsNullOrEmpty(japanese)) return japanese;
            return english ?? string.Empty;
        }
    }
}

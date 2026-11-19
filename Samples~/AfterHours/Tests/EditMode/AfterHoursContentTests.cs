using System;
using System.Collections.Generic;
using System.Reflection;
using AfterHours.Generated.Network;
using AfterHours.UI;
using NUnit.Framework;
using Tritone.Messaging;
using Tritone.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace AfterHours.Tests
{
    public sealed class AfterHoursContentTests
    {
        private static readonly string[] RequiredLocalizationKeys =
        {
            "office.title", "office.headline", "office.open", "case.title", "case.enter",
            "case.playguide", "combat.title", "combat.withdraw", "debrief.title", "debrief.return", "nav.manual", "hub.return"
        };

        [TestCase("en-US")]
        [TestCase("zh-CN")]
        [TestCase("ja-JP")]
        public void EveryLocaleContainsStableStoryAndNavigationKeys(string language)
        {
            var asset = Resources.Load<TextAsset>("Localization/" + language);
            Assert.That(asset, Is.Not.Null, "Missing localization TextAsset for " + language);
            var data = JsonUtility.FromJson<LocalePayload>(asset.text);
            Assert.That(data?.Rows, Is.Not.Null.And.Not.Empty);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in data.Rows)
            {
                Assert.That(row, Is.Not.Null);
                Assert.That(row.Text, Is.Not.Null.And.Not.Empty, "Blank translation at " + row.Id);
                Assert.That(keys.Add(row.Id), Is.True, "Duplicate key in " + language + ": " + row.Id);
            }
            foreach (string key in RequiredLocalizationKeys)
                Assert.That(keys.Contains(key), Is.True, language + " is missing " + key);
        }

        [Test]
        public void AuthoredTablesContainBothEvidenceSitesAndFourBattleSkills()
        {
            var clues = Resources.Load<TextAsset>("Tables/clues");
            var skills = Resources.Load<TextAsset>("Tables/skills");
            Assert.That(clues, Is.Not.Null);
            Assert.That(skills, Is.Not.Null);
            Assert.That(JsonUtility.FromJson<CluePayload>(clues.text).Rows, Has.Length.EqualTo(2));
            Assert.That(JsonUtility.FromJson<SkillPayload>(skills.text).Rows, Has.Length.EqualTo(4));
        }

        [Test]
        public void CityGridGraphicHasCanvasRendererWhenAddedToAnImagePanel()
        {
            var panel = new GameObject("City contour", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            try
            {
                var addCityGrid = typeof(AfterHoursScreenController).GetMethod(
                    "AddCityGrid", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(addCityGrid, Is.Not.Null);
                addCityGrid.Invoke(null, new object[] { panel });

                var graphic = panel.transform.Find("City grid");
                Assert.That(graphic, Is.Not.Null);
                Assert.That(graphic.GetComponent<CanvasRenderer>(), Is.Not.Null,
                            "Graphic components need a CanvasRenderer to draw their mesh.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(panel);
            }
        }

        [Test]
        public void GeneratedRelayProtocolAndMessagesRoundTrip()
        {
            var protocol = NetworkMessages.Protocol;
            Assert.That(protocol.IsValid, Is.True);

            byte[] hello = NetworkProtocolHandshakeFrame.CreateHello(in protocol);
            Assert.That(NetworkProtocolHandshakeFrame.TryReadHello(hello, out var remote), Is.True);
            Assert.That(protocol.EvaluateCompatibility(in remote), Is.EqualTo(ENetworkProtocolCompatibility.Compatible));
            byte[] responseFrame = NetworkProtocolHandshakeFrame.CreateResponse(in protocol, in remote);
            Assert.That(NetworkProtocolHandshakeFrame.TryReadResponse(responseFrame, out var response), Is.True);
            Assert.That(response.Compatibility, Is.EqualTo(ENetworkProtocolCompatibility.Compatible));

            var serializer = new MessageSerializer();
            NetworkMessages.Register(serializer);
            var encoded = serializer.Serialize(new CaseStatus { CaseId = "last-stop", State = "FIELD_CASE_OPEN" });
            var decoded = serializer.Deserialize(encoded) as CaseStatus;
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded.CaseId, Is.EqualTo("last-stop"));
            Assert.That(decoded.State, Is.EqualTo("FIELD_CASE_OPEN"));
        }

        [Serializable]
        private sealed class LocalePayload { public LocaleItem[] Rows; }
        [Serializable]
        private sealed class LocaleItem { public string Id; public string Text; }
        [Serializable]
        private sealed class CluePayload { public ClueItem[] Rows; }
        [Serializable]
        private sealed class ClueItem { public string Id; }
        [Serializable]
        private sealed class SkillPayload { public SkillItem[] Rows; }
        [Serializable]
        private sealed class SkillItem { public string Id; }
    }
}

using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace MezhduParami.Editor
{
    // Targeted, repeatable migration. Existing screens and persistent button
    // handlers are preserved; this does not rebuild the gameplay scene.
    public static class InterfaceRefresh
    {
        private const string Fonts = "Assets/MezhduParami/Fonts/Interface/";
        private static TMP_FontAsset body, heading;
        private static Sprite rounded;
        private const string Paper = "F5EFDF", Ink = "2D3D38", Muted = "586B60", Mint = "42654C";

        [MenuItem("Между парами/Обновить интерфейс и мини-карту")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (active.isDirty) throw new InvalidOperationException("Save the active scene first.");
            var scene = active.path == PrototypeSceneBuilder.ScenePath ? active : EditorSceneManager.OpenScene(PrototypeSceneBuilder.ScenePath);
            var ui = UnityEngine.Object.FindFirstObjectByType<PrototypeUI>();
            if (ui == null || ui.Header == null || ui.Status == null || ui.QuestStatus == null
                || ui.DialogPanel == null || ui.SelectionPanel == null || ui.Player == null)
                throw new InvalidOperationException("Existing prototype UI is incomplete.");
            if (ui.GetComponent<Canvas>() == null || ui.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null
                || UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length != 1
                || ui.PrimaryButton.onClick.GetPersistentEventCount() == 0 || ui.CancelButton.onClick.GetPersistentEventCount() == 0)
                throw new InvalidOperationException("Existing UI input wiring is incomplete.");
            Debug.Log("Interface migration: verified existing UI, " + ui.GetComponentsInChildren<RectTransform>(true).Length + " rects and persistent dialog handlers.");
            body = Font("GolosText-Regular", "GolosTextSDF", true);
            heading = Font("GolosText-SemiBold", "GolosTextSemiBoldSDF", true);
            rounded = RoundedSprite();
            var root = (RectTransform)ui.transform;
            ui.GetComponent<UnityEngine.UI.CanvasScaler>().matchWidthOrHeight = 0;
            var hud = Rect(root, "Hud"); Stretch(hud);
            hud.SetAsFirstSibling();
            ui.HudGroup = Component<CanvasGroup>(hud);
            ui.HudGroup.blocksRaycasts = true;

            var top = ui.Header.transform.parent.GetComponent<RectTransform>();
            top.SetParent(hud, false);
            Place(top, 16, 16, 182, 64); Card(top, Paper);
            Hide(top, "Caption");
            Style(ui.Header, heading, 22, Ink); Place(ui.Header.rectTransform, 12, 6, 158, 29);
            Style(ui.Status, body, 13, Muted); Place(ui.Status.rectTransform, 12, 39, 166, 20);

            var resources = Rect(hud, "Resources"); Place(resources, 206, 16, 514, 64); Card(resources, Paper);
            ui.MoneyValue = Resource(resources, "Money", "ДЕНЬГИ", "120", 0, Ink);
            ui.EnergyValue = Resource(resources, "Energy", "ЭНЕРГИЯ", "80 / 100", 1, Mint);
            ui.HealthValue = Resource(resources, "Health", "ЗДОРОВЬЕ", "80 / 100", 2, Ink);
            ui.StudyValue = Resource(resources, "Study", "ГОТОВНОСТЬ", "0 / 6", 3, Ink);
            ui.EnergyFill = Meter(resources, "EnergyBar", 140, Mint);
            ui.HealthFill = Meter(resources, "HealthBar", 268, "B57964");
            Hide(resources, "MoneyHint"); Hide(resources, "StudyHint");

            var quest = Rect(hud, "PromiseCard"); Place(quest, 16, 88, 428, 36); Card(quest, Paper);
            Hide(quest, "Caption");
            ui.QuestStatus.transform.SetParent(quest, false);
            Style(ui.QuestStatus, body, 15, Ink); Place(ui.QuestStatus.rectTransform, 12, 7, 348, 23);
            var journal = Rect(quest, "Journal"); Place(journal, 378, 5, 40, 26);
            var journalButton = Component<UnityEngine.UI.Button>(journal);
            ButtonStyle(journalButton, "J", false);
            journalButton.GetComponentInChildren<TMP_Text>().fontSize = 13;
            if (journalButton.onClick.GetPersistentEventCount() == 0)
                UnityEditor.Events.UnityEventTools.AddPersistentListener(journalButton.onClick, ui.OpenJournal);

            var footer = ui.Prompt.transform.parent.GetComponent<RectTransform>();
            // On repeated runs Prompt is inside PromptCard, not the original footer.
            footer = root.Find("FooterPanel") as RectTransform ?? footer;
            Stretch(footer);
            var oldBackground = footer.GetComponent<UnityEngine.UI.Image>();
            if (oldBackground != null) oldBackground.enabled = false;
            ui.FooterGroup = Component<CanvasGroup>(footer);
            var prompt = Rect(footer, "PromptCard"); Bottom(prompt, 16, 42, 520, 36); Card(prompt, Paper);
            ui.PromptCard = prompt.gameObject;
            ui.Prompt.transform.SetParent(prompt, false); Style(ui.Prompt, body, 17, Ink);
            Place(ui.Prompt.rectTransform, 12, 7, 496, 25);
            var feedback = Rect(footer, "FeedbackCard"); Bottom(feedback, 16, 88, 560, 48); Card(feedback, Paper);
            ui.FeedbackGroup = Component<CanvasGroup>(feedback);
            ui.Feedback.transform.SetParent(feedback, false); Style(ui.Feedback, body, 14, Ink);
            Place(ui.Feedback.rectTransform, 12, 6, 536, 38);
            var controls = Rect(footer, "ControlsCard"); Bottom(controls, 16, 12, 580, 21);
            if (controls.GetComponent<UnityEngine.UI.Image>() != null) controls.GetComponent<UnityEngine.UI.Image>().enabled = false;
            var controlsText = footer.Find("Controls")?.GetComponent<TMP_Text>() ?? controls.Find("Controls")?.GetComponent<TMP_Text>();
            if (controlsText != null)
            {
                controlsText.transform.SetParent(controls, false); Style(controlsText, body, 12, "FFFFFF");
                controlsText.text = "WASD / стрелки   движение       E   действие       J   журнал       Esc   пауза";
                Place(controlsText.rectTransform, 0, 1, 580, 19);
                var textShadow = Component<UnityEngine.UI.Shadow>(controlsText.rectTransform);
                textShadow.effectColor = new Color(0, 0, 0, .8f); textShadow.effectDistance = new Vector2(1, -1);
            }
            Minimap(hud, ui.Player.transform);

            var backdrop = Rect(root, "ModalBackdrop"); Stretch(backdrop);
            var shade = Component<UnityEngine.UI.Image>(backdrop);
            shade.sprite = null; shade.color = new Color(0.07f, 0.16f, 0.19f, 0.45f); shade.raycastTarget = true;
            ui.ModalBackdrop = backdrop.gameObject;
            backdrop.SetAsLastSibling();
            ConfigureDialog(ui);
            ConfigureSelection(ui);
            ui.DialogPanel.transform.SetAsLastSibling();
            ui.SelectionPanel.transform.SetAsLastSibling();
            ui.SelectionPanel.SetActive(true); ui.DialogPanel.SetActive(false);
            ui.HudGroup.alpha = 0; ui.FooterGroup.alpha = 0;
            foreach (var text in ui.GetComponentsInChildren<TMP_Text>(true)) EditorUtility.SetDirty(text);
            EditorUtility.SetDirty(ui);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Interface migration complete: static Cyrillic fonts, floating HUD, modal cards and campus minimap.");
        }

        private static TMP_Text Resource(RectTransform parent, string name, string caption, string value, int index, string color)
        {
            float x = 12 + index * 128;
            Label(parent, name + "Caption", caption, 11, Muted, x, 7, 116, 17);
            return Label(parent, name + "Value", value, 20, color, x, 25, 116, 27);
        }
        private static UnityEngine.UI.Image Meter(RectTransform parent, string name, float x, string color)
        {
            var track = Rect(parent, name); Place(track, x, 55, 106, 3); Card(track, "D4DBCF");
            var fill = Rect(track, "Fill"); Stretch(fill); var image = Component<UnityEngine.UI.Image>(fill);
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MezhduParami/Art/PrototypeSquare.png");
            image.type = UnityEngine.UI.Image.Type.Filled; image.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            image.fillOrigin = 0; image.fillAmount = .8f; image.color = ColorOf(color); image.raycastTarget = false;
            return image;
        }

        private static void Minimap(RectTransform hud, Transform player)
        {
            var panel = Rect(hud, "CampusMinimap");
            panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.one;
            panel.anchoredPosition = new Vector2(-16, -16); panel.sizeDelta = new Vector2(188, 242); Card(panel, Paper);
            Label(panel, "Title", "Кампус", 15, Ink, 12, 7, 130, 22, true);
            Label(panel, "North", "С ↑", 12, Muted, 150, 9, 30, 19);
            var map = Rect(panel, "Map"); Place(map, 14, 32, 160, 160);
            var picture = Component<UnityEngine.UI.Image>(map);
            picture.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/MezhduParami/Art/DesignerCampus/CampusMap.jpeg").OfType<Sprite>().Single();
            picture.color = Color.white; picture.raycastTarget = false;
            var minimap = Component<CampusMinimap>(panel);
            minimap.MapArea = map; minimap.Player = player;
            minimap.WorldBounds = new Rect(-20.48f, -20.48f, 40.96f, 40.96f);
            // Landmark positions come from the playable scene, never a second set of world coordinates.
            string[] names = { "Sasha", "Bed", "Noticeboard", "StudyDesk" };
            string[] captions = { "Саша", "Общага", "Работа", "Учёба" };
            string[] colors = { "9B5B99", Mint, "A66A18", "4F71AB" };
            for (int i = 0; i < names.Length; i++)
            {
                var landmark = GameObject.Find(names[i]);
                if (landmark == null) throw new InvalidOperationException("Missing map landmark: " + names[i]);
                var mark = Rect(map, names[i] + "Marker");
                mark.anchorMin = mark.anchorMax = map.pivot; mark.pivot = new Vector2(.5f, .5f);
                mark.sizeDelta = new Vector2(14, 14);
                mark.anchoredPosition = CampusMinimap.WorldToMapPosition(landmark.transform.position, minimap.WorldBounds, new Rect(0, -160, 160, 160));
                Card(mark, colors[i]);
                var number = Label(mark, "Number", (i + 1).ToString(), 10, "FFFFFF", 0, 0, 14, 14);
                number.alignment = TextAlignmentOptions.Center;
                Label(panel, "Legend" + i, (i + 1) + "  " + captions[i], 11, colors[i], 12 + i % 2 * 85, 200 + i / 2 * 18, 82, 17);
            }
            var marker = Rect(map, "PlayerMarker"); marker.sizeDelta = new Vector2(10, 10);
            marker.pivot = new Vector2(.5f, .5f); Card(marker, "FFFFFF");
            var core = Rect(marker, "Core");
            core.anchorMin = core.anchorMax = core.pivot = new Vector2(.5f, .5f); core.sizeDelta = new Vector2(6, 6);
            Card(core, "243B43"); marker.SetAsLastSibling(); minimap.PlayerMarker = marker;
            Hide(panel, "PlayerLegend");
        }

        private static void ConfigureDialog(PrototypeUI ui)
        {
            var panel = (RectTransform)ui.DialogPanel.transform; Center(panel, 780, 496); Card(panel, Paper);
            panel.GetComponent<UnityEngine.UI.Image>().color = ColorOf(Paper);
            Label(panel, "Caption", "МЕЖДУ ПАРАМИ", 11, Muted, 24, 16, 732, 18);
            Style(ui.DialogTitle, heading, 26, Ink); Place(ui.DialogTitle.rectTransform, 24, 39, 732, 36);
            var line = Rect(panel, "Divider"); Place(line, 24, 85, 732, 1); Card(line, "D4DBCF");
            Style(ui.DialogBody, body, 19, Ink); Place(ui.DialogBody.rectTransform, 24, 102, 732, 304);
            ui.DialogBody.lineSpacing = 3;
            Place((RectTransform)ui.PrimaryButton.transform, 24, 426, 354, 46); ButtonStyle(ui.PrimaryButton, null, true);
            Place((RectTransform)ui.CancelButton.transform, 402, 426, 354, 46); ButtonStyle(ui.CancelButton, null, false);
        }

        private static void ConfigureSelection(PrototypeUI ui)
        {
            var panel = (RectTransform)ui.SelectionPanel.transform; Center(panel, 620, 296); Card(panel, Paper);
            panel.GetComponent<UnityEngine.UI.Image>().color = ColorOf(Paper);
            Label(panel, "Caption", "ПЕРВЫЙ КУРС. ТВОЯ ИСТОРИЯ.", 12, Muted, 28, 20, 564, 20);
            var title = panel.Find("Title").GetComponent<TMP_Text>(); Style(title, heading, 42, Ink);
            Place(title.rectTransform, 26, 49, 568, 58);
            var description = panel.Find("Description").GetComponent<TMP_Text>(); Style(description, body, 18, Ink);
            description.text = "Новый кампус, новые люди и целый год впереди.\nУчись, знакомься и находи время для себя.";
            Place(description.rectTransform, 28, 124, 564, 52);
            Label(panel, "ChoiceHint", "Кем начнётся эта история?", 13, Muted, 28, 193, 564, 22);
            var male = panel.Find("Student").GetComponent<UnityEngine.UI.Button>();
            var female = panel.Find("StudentFemale").GetComponent<UnityEngine.UI.Button>();
            Place((RectTransform)male.transform, 28, 228, 270, 44); ButtonStyle(male, "Студент", true);
            Place((RectTransform)female.transform, 322, 228, 270, 44); ButtonStyle(female, "Студентка", false);
        }

        private static void ButtonStyle(UnityEngine.UI.Button button, string caption, bool primary)
        {
            var rect = (RectTransform)button.transform; Card(rect, primary ? Mint : "DDE2D3");
            var image = rect.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget = true;
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = ColorOf("D5EADD");
            colors.pressedColor = ColorOf("B8CEBD"); colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(.65f, .65f, .65f, .6f); colors.fadeDuration = .1f; button.colors = colors;
            var label = button.GetComponentInChildren<TMP_Text>() ?? Label(rect, "Label", "", 20, Ink, 0, 0, rect.sizeDelta.x, rect.sizeDelta.y);
            Style(label, body, 18, primary ? "FFFFFF" : Ink); Stretch(label.rectTransform);
            label.margin = new Vector4(12, 4, 12, 4); label.alignment = TextAlignmentOptions.Center;
            if (caption != null) label.text = caption;
        }

        private static TMP_FontAsset Font(string file, string assetName, bool punctuation)
        {
            string path = Fonts + assetName + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null && existing.material != null && existing.atlasTextures.Length > 0 && existing.atlasTextures[0] != null) return existing;
            if (existing != null) AssetDatabase.DeleteAsset(path);
            var source = AssetDatabase.LoadAssetAtPath<UnityEngine.Font>(Fonts + file + ".ttf");
            if (source == null) throw new InvalidOperationException("Missing source font " + file);
            var font = TMP_FontAsset.CreateFontAsset(source, 72, 8, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
            font.name = assetName;
            string chars = string.Concat(Enumerable.Range(32, 95).Select(c => (char)c))
                + "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя—–«»•…";
            if (punctuation) chars += "↑→×";
            if (!font.TryAddCharacters(chars, out string missing)) throw new InvalidOperationException(file + " missing: " + missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, path);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
            EditorUtility.SetDirty(font);
            EditorUtility.SetDirty(font.material);
            foreach (var atlas in font.atlasTextures) EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();
            return font;
        }

        private static Sprite RoundedSprite()
        {
            const string path = "Assets/MezhduParami/Art/InterfaceRounded.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null && existing.texture != null) return existing;
            if (existing != null) AssetDatabase.DeleteAsset(path);
            const int size = 64; const float radius = 14;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "RoundedCardTexture", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + .5f - size / 2f) - (size / 2f - radius), 0);
                float dy = Mathf.Max(Mathf.Abs(y + .5f - size / 2f) - (size / 2f - radius), 0);
                pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy)));
            }
            texture.SetPixels(pixels); texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
            sprite.name = "InterfaceRounded"; AssetDatabase.CreateAsset(sprite, path); AssetDatabase.AddObjectToAsset(texture, sprite);
            EditorUtility.SetDirty(sprite); EditorUtility.SetDirty(texture); AssetDatabase.SaveAssets();
            return sprite;
        }

        private static void Card(RectTransform rect, string color)
        {
            var image = Component<UnityEngine.UI.Image>(rect); image.enabled = true; image.sprite = rounded;
            image.type = UnityEngine.UI.Image.Type.Sliced; image.color = ColorOf(color); image.raycastTarget = false;
            image.pixelsPerUnitMultiplier = 2;
            if (color == Paper) { var tint = image.color; tint.a = .94f; image.color = tint; }
            var effect = rect.GetComponent<UnityEngine.UI.Shadow>();
            if (effect != null) effect.enabled = false;
        }
        private static TMP_Text Label(Transform parent, string name, string text, int size, string color, float x, float y, float w, float h, bool emphasis = false)
        {
            var rect = Rect(parent, name); Place(rect, x, y, w, h);
            var label = Component<TextMeshProUGUI>(rect); Style(label, emphasis ? heading : body, size, color); label.text = text; return label;
        }
        private static void Style(TMP_Text text, TMP_FontAsset font, int size, string color)
        {
            text.font = font; text.fontSize = size; text.fontStyle = FontStyles.Normal; text.color = ColorOf(color);
            text.enableAutoSizing = false; text.raycastTarget = false; text.alignment = TextAlignmentOptions.TopLeft;
            text.overflowMode = TextOverflowModes.Overflow; text.margin = Vector4.zero;
        }
        private static RectTransform Rect(Transform parent, string name)
        {
            var child = parent.Find(name) as RectTransform;
            if (child != null) return child;
            child = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); child.SetParent(parent, false); return child;
        }
        private static void Hide(Transform parent, string name)
        {
            var child = parent.Find(name); if (child != null) child.gameObject.SetActive(false);
        }
        private static T Component<T>(RectTransform rect) where T : Component
        {
            var existing = rect.GetComponent<T>();
            return existing != null ? existing : rect.gameObject.AddComponent<T>();
        }
        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
        }
        private static void Bottom(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h);
        }
        private static void Center(RectTransform rect, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(w, h);
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static Color ColorOf(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var color); return color; }
    }
}

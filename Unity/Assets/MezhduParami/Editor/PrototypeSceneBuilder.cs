using System;
using System.IO;
using System.Linq;
using MezhduParami;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;

namespace MezhduParami.Editor
{
    public static class PrototypeSceneBuilder
    {
        private const string Root = "Assets/MezhduParami";
        public const string ScenePath = Root + "/Scenes/PrototypeDorm.unity";
        private static Sprite square;
        private static Material unlit;
        private static TMP_FontAsset font;

        public static void ImportTextResources() => TMP_PackageResourceImporter.ImportResources(true, false, false);

        [MenuItem("Между парами/Создать сцену прототипа")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before creating the prototype scene.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Prototype scene already exists; edit it instead of replacing it.");
            if (DetectPipeline() != "URP") throw new InvalidOperationException("The prototype requires the project's URP 2D pipeline.");
            foreach (string folder in new[] { "Scenes", "Art", "Data", "Fonts" }) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            CreateSquare();
            CreateFont();
            unlit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(unlit, Root + "/Art/PrototypeUnlit.mat");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 5.625f;
            camera.backgroundColor = ColorOf("23323D");
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = camera.allowMSAA = camera.allowDynamicResolution = false;
            var pixel = camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
            pixel.assetsPPU = 16;
            pixel.refResolutionX = 320;
            pixel.refResolutionY = 180;
            pixel.gridSnapping = UnityEngine.Rendering.Universal.PixelPerfectCamera.GridSnapping.PixelSnapping;
            pixel.cropFrame = UnityEngine.Rendering.Universal.PixelPerfectCamera.CropFrame.Windowbox;
            QualitySettings.antiAliasing = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            Time.fixedDeltaTime = 1f / 60f;
            PlayerSettings.companyName = "Mezhdu Parami Team";
            PlayerSettings.productName = "Между парами";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            EditorSettings.serializationMode = SerializationMode.ForceText;

            var world = new GameObject("DormRoom").transform;
            Rect(world, "Floor", Vector2.zero, new Vector2(17, 6.4f), "D9C59C", -1000);
            for (int x = -8; x <= 8; x++)
                Rect(world, "FloorSeam", new Vector2(x, 0), new Vector2(0.025f, 6.2f), "C7AE81", -999);
            Rect(world, "BackWall", new Vector2(0, 3), new Vector2(17, 0.55f), "6F9A88", 100, true);
            Rect(world, "FrontWall", new Vector2(0, -3), new Vector2(17, 0.4f), "6F9A88", 100, true);
            Rect(world, "LeftWall", new Vector2(-8.35f, 0), new Vector2(0.4f, 6.4f), "6F9A88", 100, true);
            Rect(world, "RightWall", new Vector2(8.35f, 0), new Vector2(0.4f, 6.4f), "6F9A88", 100, true);
            Rect(world, "Rug", new Vector2(0, -0.6f), new Vector2(4, 2.1f), "C87B66", -995);
            Rect(world, "Window", new Vector2(-3, 3.05f), new Vector2(2.2f, 0.4f), "B8DDE3", 101);

            var study = MakeActivity("study", "Подготовка к занятиям", "Разобраться с первой темой и сделать заметки.", -20, 0, 1);
            var work = MakeActivity("work.simple", "Простое поручение", "Помочь коменданту разобрать коробки. Характеристики не требуются. Не больше одной смены за день.", -15, 25, 0, true);
            var rest = MakeActivity("rest", "Отдых", "Отложить дела и восстановить силы. Энергия не превысит 100.", 40, 0, 0);
            var desk = Rect(world, "StudyDesk", new Vector2(-5, 1.4f), new Vector2(2.4f, 1.2f), "9B6249", 0, true);
            Rect(desk.transform, "Notebook", new Vector2(-0.4f, 0.1f), new Vector2(0.6f, 0.5f), "FFF2CE", 1);
            Interactable(desk, "Учебный стол", study);
            WorldLabel(world, "СТОЛ / УЧЁБА", new Vector2(-5, 2.35f));
            var bed = Rect(world, "Bed", new Vector2(5.6f, 1.2f), new Vector2(2.2f, 1.7f), "547B9A", 0, true);
            Rect(bed.transform, "Pillow", new Vector2(0, 0.45f), new Vector2(1.8f, 0.4f), "E6E5D7", 1);
            Interactable(bed, "Кровать", rest);
            WorldLabel(world, "КРОВАТЬ / ОТДЫХ", new Vector2(5.6f, 2.35f));
            var board = Rect(world, "Noticeboard", new Vector2(0, 2.3f), new Vector2(1.7f, 0.7f), "AB7948", 0, true);
            Rect(board.transform, "Notice", Vector2.zero, new Vector2(0.55f, 0.45f), "F5DE8A", 1);
            Interactable(board, "Доска объявлений", work);
            WorldLabel(world, "ПОДРАБОТКА", new Vector2(0, 3.5f));
            var neighbour = Rect(world, "Sasha", new Vector2(3, -0.8f), new Vector2(0.6f, 0.9f), "CF8E49", 10, true);
            Rect(neighbour.transform, "Head", new Vector2(0, 0.55f), new Vector2(0.55f, 0.5f), "E9BB91", 11);
            var conversation = neighbour.AddComponent<WorldInteraction>();
            conversation.DisplayName = "Саша — сосед по общаге";
            conversation.Conversation = "Привет! Располагайся. За столом можно подготовиться к занятиям, на доске есть простая подработка. Устанешь — отдохни.\n\nНа утро, день и вечер приходится по одному большому делу. А поговорить и осмотреться можно бесплатно.";
            WorldLabel(world, "САША", new Vector2(3, 0.4f));

            var player = new GameObject("Player", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SortingGroup));
            player.transform.position = new Vector3(0, -1.5f, 0);
            var rigidbody = player.GetComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0;
            rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            player.GetComponent<BoxCollider2D>().size = new Vector2(0.5f, 0.5f);
            Rect(player.transform, "Shadow", new Vector2(0, -0.2f), new Vector2(0.65f, 0.22f), "72695B", -1);
            var shirt = Rect(player.transform, "Shirt", new Vector2(0, 0.15f), new Vector2(0.6f, 0.65f), "31819B", 1).GetComponent<SpriteRenderer>();
            Rect(player.transform, "Head", new Vector2(0, 0.65f), new Vector2(0.5f, 0.5f), "E9BB91", 2);
            Rect(player.transform, "Hair", new Vector2(0, 0.9f), new Vector2(0.55f, 0.18f), "5B4538", 3);
            var motor = player.AddComponent<PlayerMotor>();
            motor.Shirt = shirt;
            var interactor = player.AddComponent<PlayerInteractor>();
            var ui = CreateUI();
            ui.Player = motor;
            motor.UI = ui;
            interactor.UI = ui;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            UpgradeThreeDaySlice();
            Debug.Log("[MezhduParami] Prototype scene created: " + ScenePath);
        }

        [MenuItem("Между парами/Подключить трёхдневный сценарий")]
        public static void UpgradeThreeDaySlice()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before upgrading the scene.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.isDirty) throw new InvalidOperationException("Save the active scene before upgrading it.");
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
            var ui = UnityEngine.Object.FindFirstObjectByType<PrototypeUI>();
            if (ui == null) throw new InvalidOperationException("PrototypeUI is missing.");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/PrototypeCyrillic.asset");
            var definition = AssetDatabase.LoadAssetAtPath<SashaQuestDefinition>(Root + "/Data/SashaPromise.asset");
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<SashaQuestDefinition>();
                definition.Consultation = MakeActivity("sasha.consultation", "Консультация Саши", "Вместе разобрать трудную тему. Взамен — обещание помочь Саше позже.", -15, 0, 0);
                definition.Consultation.ReadinessDelta = 2;
                definition.Help = MakeActivity("sasha.help", "Помочь Саше с проектом", "Выделить обещанный вечер и вместе закончить проект.", -20, 0, 0);
                definition.SpecialShift = MakeActivity("work.special", "Разовая вечерняя смена", "Помочь с разгрузкой перед мероприятием. Только вечером дня 3; одна оплачиваемая смена за день.", -20, 80, 0, true);
                definition.OfferText = "Помогу разобраться с учёбой сейчас. Взамен помоги мне с проектом вечером дня 3. Согласишься? Отказ не мешает заниматься самостоятельно.";
                definition.FulfilledText = "Спасибо за помощь! Проект готов. Теперь я знаю, что на тебя можно положиться.";
                definition.BrokenText = "Я ждал тебя с проектом. Пришлось заканчивать одному. В следующий раз заранее скажи, если не успеваешь.";
                definition.DeclinedText = "Хорошо, без обязательств. Если передумаешь насчёт учёбы, за столом можно подготовиться самостоятельно.";
                AssetDatabase.CreateAsset(definition, Root + "/Data/SashaPromise.asset");
                EditorUtility.SetDirty(definition.Consultation);
            }
            ui.QuestDefinition = definition;
            var study = AssetDatabase.LoadAssetAtPath<ActivityDefinition>(Root + "/Data/study.asset");
            study.ReadinessDelta = 1;
            EditorUtility.SetDirty(study);
            var sasha = GameObject.Find("Sasha").GetComponent<WorldInteraction>();
            sasha.Id = "npc.sasha";
            var board = GameObject.Find("Noticeboard").GetComponent<WorldInteraction>();
            board.Id = "work.board";

            var header = (RectTransform)ui.Header.transform.parent;
            header.sizeDelta = new Vector2(0, 122);
            var questStatus = header.Find("QuestStatus");
            ui.QuestStatus = questStatus != null ? questStatus.GetComponent<TMP_Text>()
                : Text(header, "QuestStatus", "", 17, ColorOf("F4D987"), new Vector2(24, -86), new Vector2(1232, 27));
            ui.Status.rectTransform.anchoredPosition = new Vector2(24, -52);
            ui.Status.rectTransform.sizeDelta = new Vector2(1232, 29);
            var controls = ui.Feedback.transform.parent.Find("Controls").GetComponent<TMP_Text>();
            controls.text = "WASD / стрелки — движение    E — взаимодействие    J — журнал    Esc — отмена / пауза";
            var selectionDescription = ui.SelectionPanel.transform.Find("Description").GetComponent<TMP_Text>();
            selectionDescription.text = "Первые три дня в общаге: учёба, работа и обещание.\nВыбери героя. Возможности одинаковые.";
            var dialog = (RectTransform)ui.DialogPanel.transform;
            dialog.sizeDelta = new Vector2(800, 462);
            ui.DialogTitle.rectTransform.sizeDelta = new Vector2(740, 42);
            ui.DialogBody.fontSize = 20;
            ui.DialogBody.rectTransform.anchoredPosition = new Vector2(30, -78);
            ui.DialogBody.rectTransform.sizeDelta = new Vector2(740, 296);
            ResizeButton(ui.PrimaryButton, new Vector2(30, -390), new Vector2(354, 50));
            ResizeButton(ui.CancelButton, new Vector2(414, -390), new Vector2(356, 50));
            for (int i = ui.CancelButton.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(ui.CancelButton.onClick, i);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(ui.CancelButton.onClick, ui.OnSecondary);
            EditorUtility.SetDirty(ui);
            EditorUtility.SetDirty(sasha);
            EditorUtility.SetDirty(board);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static void ResizeButton(UnityEngine.UI.Button button, Vector2 position, Vector2 size)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            button.GetComponentInChildren<TMP_Text>().rectTransform.sizeDelta = size;
        }

        public static string DetectPipeline()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            return pipeline != null && (pipeline.GetType().FullName ?? "").Contains("Universal") ? "URP" : "Other";
        }

        private static void CreateSquare()
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            texture.SetPixels(Enumerable.Repeat(Color.white, 256).ToArray());
            texture.Apply();
            string path = Root + "/Art/PrototypeSquare.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            square = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void CreateFont()
        {
            string path = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
            var source = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (source == null) throw new InvalidOperationException("Import TMP Essential Resources first.");
            font = TMP_FontAsset.CreateFontAsset(source, 72, 8, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
            font.name = "PrototypeCyrillic";
            string characters = string.Concat(Enumerable.Range(32, 95).Select(c => (char)c))
                + "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя—–«»•…→×";
            if (!font.TryAddCharacters(characters, out string missing))
                throw new InvalidOperationException("Font is missing characters: " + missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, Root + "/Fonts/PrototypeCyrillic.asset");
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
        }

        private static ActivityDefinition MakeActivity(string id, string title, string description, int energy, int money, int xp, bool work = false)
        {
            var action = ScriptableObject.CreateInstance<ActivityDefinition>();
            action.Id = id; action.Title = title; action.Description = description;
            action.EnergyDelta = energy; action.MoneyDelta = money; action.IntelligenceExperience = xp; action.IsWork = work;
            AssetDatabase.CreateAsset(action, Root + "/Data/" + id.Replace('.', '_') + ".asset");
            return action;
        }

        private static GameObject Rect(Transform parent, string name, Vector2 position, Vector2 size, string color, int order, bool collider = false)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            // DrawMode avoids scaling children together with their parent's furniture size.
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = ColorOf(color);
            renderer.sharedMaterial = unlit;
            renderer.sortingOrder = order;
            if (collider) go.AddComponent<BoxCollider2D>().size = size;
            return go;
        }

        private static void Interactable(GameObject go, string name, ActivityDefinition action)
        {
            var interaction = go.AddComponent<WorldInteraction>();
            interaction.DisplayName = name;
            interaction.Activity = action;
        }

        private static void WorldLabel(Transform parent, string text, Vector2 position)
        {
            var go = new GameObject("Label", typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(position.x, position.y, -0.1f);
            var label = go.GetComponent<TextMeshPro>();
            label.font = font; label.text = text; label.fontSize = 2.5f;
            label.color = ColorOf("243644"); label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(4, 0.7f);
            label.GetComponent<MeshRenderer>().sortingOrder = 200;
        }

        private static PrototypeUI CreateUI()
        {
            var canvasObject = new GameObject("PrototypeUI", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            var ui = canvasObject.AddComponent<PrototypeUI>();
            var top = Panel(canvas.transform, "HeaderPanel", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), Vector2.zero, new Vector2(0, 94), "23323D");
            ui.Header = Text(top, "Title", "", 25, Color.white, new Vector2(24, -12), new Vector2(1232, 32));
            ui.Status = Text(top, "Status", "", 18, ColorOf("D6E9E0"), new Vector2(24, -54), new Vector2(1232, 27));
            var bottom = Panel(canvas.transform, "FooterPanel", Vector2.zero, Vector2.right, Vector2.zero, Vector2.zero, new Vector2(0, 113), "23323D");
            ui.Prompt = Text(bottom, "Prompt", "", 22, ColorOf("F4D987"), new Vector2(24, -12), new Vector2(1232, 30));
            ui.Feedback = Text(bottom, "Feedback", "", 17, ColorOf("E0E6E9"), new Vector2(24, -50), new Vector2(1232, 30));
            Text(bottom, "Controls", "WASD / стрелки — движение    E — взаимодействие    Esc — пауза", 15, ColorOf("B2C5CC"), new Vector2(24, -85), new Vector2(1232, 23));
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var selection = Panel(canvas.transform, "CharacterSelection", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680, 300), "F5F0DF");
            ui.SelectionPanel = selection.gameObject;
            Text(selection, "Title", "Между парами", 34, ColorOf("243644"), new Vector2(32, -26), new Vector2(616, 48));
            Text(selection, "Description", "Первый курс начинается с комнаты в общаге.\nВыбери героя. Возможности у обоих одинаковые.", 22, ColorOf("243644"), new Vector2(32, -92), new Vector2(616, 80));
            Button(selection, "Student", "Студент", new Vector2(32, -213), new Vector2(280, 54), ui, nameof(PrototypeUI.SelectStudent), false);
            Button(selection, "StudentFemale", "Студентка", new Vector2(368, -213), new Vector2(280, 54), ui, nameof(PrototypeUI.SelectStudent), true);
            var dialog = Panel(canvas.transform, "Dialog", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 380), "F5F0DF");
            ui.DialogPanel = dialog.gameObject;
            ui.DialogTitle = Text(dialog, "Title", "", 29, ColorOf("243644"), new Vector2(30, -24), new Vector2(660, 42));
            ui.DialogBody = Text(dialog, "Body", "", 21, ColorOf("243644"), new Vector2(30, -90), new Vector2(660, 203));
            ui.PrimaryButton = Button(dialog, "Confirm", "Подтвердить", new Vector2(30, -306), new Vector2(320, 52), ui, nameof(PrototypeUI.OnPrimary));
            ui.PrimaryLabel = ui.PrimaryButton.GetComponentInChildren<TextMeshProUGUI>();
            ui.CancelButton = Button(dialog, "Cancel", "Отмена / Esc", new Vector2(374, -306), new Vector2(316, 52), ui, nameof(PrototypeUI.CloseDialog));
            selection.gameObject.SetActive(true);
            dialog.gameObject.SetActive(false);
            return ui;
        }

        private static RectTransform Panel(Transform parent, string name, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size, string color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
            go.GetComponent<UnityEngine.UI.Image>().color = ColorOf(color);
            return rect;
        }

        private static TMP_Text Text(Transform parent, string name, string text, int size, Color color, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var label = go.GetComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(parent, false);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = new Vector2(0, 1);
            label.rectTransform.anchoredPosition = position; label.rectTransform.sizeDelta = dimensions;
            label.font = font; label.fontSize = size; label.color = color; label.text = text;
            label.raycastTarget = false; label.enableAutoSizing = false;
            return label;
        }

        private static UnityEngine.UI.Button Button(Transform parent, string name, string text, Vector2 position, Vector2 size, PrototypeUI ui, string method, bool? argument = null)
        {
            var rect = Panel(parent, name, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), position, size, "397F83");
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = rect.GetComponent<UnityEngine.UI.Image>();
            var label = Text(rect, "Label", text, 22, Color.white, Vector2.zero, size);
            label.alignment = TextAlignmentOptions.Center;
            if (argument.HasValue)
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(button.onClick, ui.SelectStudent, argument.Value);
            else if (method == nameof(PrototypeUI.OnPrimary))
                UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, ui.OnPrimary);
            else UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, ui.CloseDialog);
            return button;
        }

        private static Color ColorOf(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out var color);
            return color;
        }

        public static string BuildWindows()
        {
            string destination = Path.GetFullPath("../Builds/Windows/MezhduParami.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = destination,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Build failed: " + report.summary.result);
            return destination;
        }

        public static void QueueWindowsBuild()
        {
            // Return the CLI request before the long native build blocks the
            // Editor thread. The result is checked via the actual build report.
            EditorApplication.delayCall += () => Debug.Log("[MezhduParami] Windows build: " + BuildWindows());
        }
    }
}

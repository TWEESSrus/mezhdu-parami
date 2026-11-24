using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;

namespace MezhduParami.Editor
{
    public static class MechanicsSandboxBuilder
    {
        public const string ScenePath = "Assets/MezhduParami/Scenes/MechanicsSandbox.unity";
        private const string Root = "Assets/MezhduParami/";
        private static Sprite square, rounded;
        private static Material material;
        private static TMP_FontAsset body, heading;

        [MenuItem("Между парами/Создать или обновить полигон механик")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the active scene first.");
            square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/PrototypeSquare.png");
            rounded = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/InterfaceRounded.asset");
            material = AssetDatabase.LoadAssetAtPath<Material>(Root + "Art/PrototypeUnlit.mat");
            body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "Fonts/Interface/MellowPixelRegularSDF.asset");
            heading = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "Fonts/Interface/MellowPixelLightSDF.asset");
            if (square == null || rounded == null || material == null || body == null || heading == null)
                throw new InvalidOperationException("Missing existing project art/fonts.");
            bool createWorld = !File.Exists(ScenePath);
            var scene = createWorld ? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single) : EditorSceneManager.OpenScene(ScenePath);
            MechanicsSandboxUI ui;
            PlayerMotor player;
            if (createWorld)
            {
                var canvas = new GameObject("SandboxUI", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                ui = canvas.AddComponent<MechanicsSandboxUI>();
                canvas.AddComponent<TaskTrackerPanel>();
                var view = canvas.GetComponent<Canvas>(); view.renderMode = RenderMode.ScreenSpaceOverlay; view.sortingOrder = 1000;
                var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0;
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                player = BuildWorld(ui);
            }
            else
            {
                ui = UnityEngine.Object.FindFirstObjectByType<MechanicsSandboxUI>();
                player = UnityEngine.Object.FindFirstObjectByType<PlayerMotor>();
                if (ui == null || player == null || ui.GetComponent<Canvas>() == null)
                    throw new InvalidOperationException("Existing sandbox hierarchy is incomplete.");
                Debug.Log("Updating existing sandbox UI in place.");
            }
            var environment = GameObject.Find("MechanicsPlayground");
            if (environment == null) throw new InvalidOperationException("Sandbox world root is missing.");
            if (environment.transform.Find("training") == null)
                Station(environment.transform, "training", "Тренировка", SandboxStationKind.Training, new Vector2(0, -3.7f), "C69581");
            if (environment.transform.Find("warehouse") == null)
                Station(environment.transform, "warehouse", "Склад", SandboxStationKind.Warehouse, new Vector2(-4.5f, 2.05f), "C3A576");
            if (environment.transform.Find("rehearsal") == null)
                Station(environment.transform, "rehearsal", "Репетиция", SandboxStationKind.Rehearsal, new Vector2(3.65f, 2.4f), "BDA7CB");
            if (environment.transform.Find("visitorDesk") == null)
                Station(environment.transform, "visitorDesk", "Инфостойка", SandboxStationKind.VisitorDesk, new Vector2(7.8f, -2.9f), "7FB4A7");
            if (environment.transform.Find("practiceReport") == null)
                Station(environment.transform, "practiceReport", "Лист практики", SandboxStationKind.PracticeReport,
                    new Vector2(-2.45f, -0.35f), "A5B8CA", 0.78f);
            ArrangeWorld(environment.transform, player);
            Layout(ui);
            player.InputGate = ui;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true), new EditorBuildSettingsScene(PrototypeSceneBuilder.ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Mechanics sandbox ready: four separated zones and character sheet.");
        }

        private static PlayerMotor BuildWorld(MechanicsSandboxUI ui)
        {
            var environment = new GameObject("MechanicsPlayground").transform;
            WorldRect(environment, "Floor", Vector2.zero, new Vector2(20, 12), "727B64", -1000);
            for (int x = -10; x <= 10; x++) WorldRect(environment, "GridX" + x, new Vector2(x, 0), new Vector2(.025f, 12), "78856C", -999);
            for (int y = -6; y <= 6; y++) WorldRect(environment, "GridY" + y, new Vector2(0, y), new Vector2(20, .025f), "78856C", -999);
            WorldRect(environment, "TopWall", new Vector2(0, 5.8f), new Vector2(20, .4f), "3F4D40", -100, true);
            WorldRect(environment, "BottomWall", new Vector2(0, -5.8f), new Vector2(20, .4f), "3F4D40", -100, true);
            WorldRect(environment, "LeftWall", new Vector2(-9.8f, 0), new Vector2(.4f, 12), "3F4D40", -100, true);
            WorldRect(environment, "RightWall", new Vector2(9.8f, 0), new Vector2(.4f, 12), "3F4D40", -100, true);
            Station(environment, "shop", "Лавка", SandboxStationKind.Shop, new Vector2(-5.4f, 0), "BDA67C");
            Station(environment, "sorting", "Посылки", SandboxStationKind.Sorting, new Vector2(0, 2.2f), "AE9573");
            Station(environment, "copy", "Копицентр", SandboxStationKind.CopyCenter, new Vector2(5.4f, 0), "91ADAE");
            Station(environment, "study", "Учёба", SandboxStationKind.Study, new Vector2(-4, -3), "ACA6C1");
            Station(environment, "rest", "Отдых", SandboxStationKind.Rest, new Vector2(4, -3), "9EBB82");

            var hero = new GameObject("Player", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SortingGroup));
            hero.transform.position = new Vector3(0, -1.6f, 0);
            var rigidbody = hero.GetComponent<Rigidbody2D>(); rigidbody.gravityScale = 0;
            rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation; rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            hero.GetComponent<BoxCollider2D>().size = new Vector2(.5f, .5f);
            var motor = hero.AddComponent<PlayerMotor>();
            var visual = new GameObject("DesignerHero", typeof(SpriteRenderer), typeof(Animator)); visual.transform.SetParent(hero.transform, false);
            visual.transform.localPosition = new Vector3(0, .38f, 0); visual.transform.localScale = Vector3.one * 1.35f;
            var sprite = visual.GetComponent<SpriteRenderer>(); sprite.sharedMaterial = material;
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Root + "Art/DesignerCampus/anim_3.controller");
            if (controller == null) throw new InvalidOperationException("Missing designer character controller.");
            var clip = controller.animationClips.Single();
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
            sprite.sprite = AnimationUtility.GetObjectReferenceCurve(clip, binding)[0].value as Sprite;
            motor.WalkAnimator = visual.GetComponent<Animator>(); motor.WalkAnimator.runtimeAnimatorController = controller;
            motor.InputGate = ui;
            hero.AddComponent<SandboxInteractor>().UI = ui;
            var view = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            view.tag = "MainCamera"; view.orthographic = true; view.orthographicSize = 5.625f;
            view.transform.position = new Vector3(0, 0, -10); view.rect = new Rect(0, 0, 1, 1); view.ResetAspect();
            view.backgroundColor = ColorOf("3F4D40"); view.clearFlags = CameraClearFlags.SolidColor;
            view.allowHDR = view.allowMSAA = false;
            return motor;
        }
        private static void Station(Transform parent, string id, string caption, SandboxStationKind kind, Vector2 position, string color, float scale = 1f)
        {
            WorldRect(parent, id + "Pad", position, new Vector2(3.4f, 2.7f) * scale, "596653", -500);
            var rect = WorldRect(parent, id, position, new Vector2(2.2f, 1.2f) * scale, color, 20, true);
            var component = rect.AddComponent<SandboxStation>(); component.Id = "sandbox." + id; component.Kind = kind; component.DisplayName = caption;
            var text = new GameObject("Label", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            text.transform.SetParent(rect.transform, false); text.transform.localPosition = new Vector3(0, 1.15f * scale, -.1f);
            text.font = body; text.fontSize = 2.8f; text.color = ColorOf("FFF0CD"); text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(3.8f, .65f); text.GetComponent<MeshRenderer>().sortingOrder = 200;
            text.text = caption;
        }
        private static GameObject WorldRect(Transform parent, string name, Vector2 position, Vector2 size, string color, int order, bool collision = false)
        {
            var existing = parent.Find(name);
            var go = existing != null ? existing.gameObject : new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false); go.transform.localPosition = position;
            var sprite = go.GetComponent<SpriteRenderer>(); sprite.sprite = square; sprite.sharedMaterial = material;
            sprite.drawMode = SpriteDrawMode.Sliced; sprite.size = size; sprite.color = ColorOf(color); sprite.sortingOrder = order;
            if (collision)
            {
                var box = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
                box.size = size;
            }
            return go;
        }

        private static void ArrangeWorld(Transform world, PlayerMotor player)
        {
            // Move existing stations in place, preserving their IDs and interaction components.
            WorldRect(world, "Floor", Vector2.zero, new Vector2(80, 60), "727B64", -1000);
            foreach (Transform child in world)
                if (child.name.StartsWith("GridX") || child.name.StartsWith("GridY")) child.gameObject.SetActive(false);
            WorldRect(world, "TopWall", new Vector2(0, 6.4f), new Vector2(26.4f, .25f), "525D4B", -100, true);
            WorldRect(world, "BottomWall", new Vector2(0, -6.4f), new Vector2(26.4f, .25f), "525D4B", -100, true);
            WorldRect(world, "LeftWall", new Vector2(-13.1f, 0), new Vector2(.25f, 13), "525D4B", -100, true);
            WorldRect(world, "RightWall", new Vector2(13.1f, 0), new Vector2(.25f, 13), "525D4B", -100, true);
            WorldRect(world, "MainWalkway", new Vector2(0, 0), new Vector2(25.8f, 1.3f), "89917A", -900);
            WorldRect(world, "WorkWalkway", new Vector2(2.7f, 0), new Vector2(1.1f, 11.5f), "89917A", -900);
            Zone(world, "DevelopmentZone", "01  РАЗВИТИЕ", new Vector2(-4.6f, 2.85f), new Vector2(13.4f, 4.1f), "56695E", "BBCEB4");
            Zone(world, "WorkZone", "02  РАБОТА", new Vector2(7.7f, .4f), new Vector2(8.2f, 9), "686452", "E4C48B");
            Zone(world, "LivingZone", "03  БЫТ", new Vector2(-6.9f, -2.8f), new Vector2(8.8f, 3.9f), "626C50", "D1D8AE");
            Zone(world, "PracticeZone", "04  ПРАКТИКА", new Vector2(0, -2.8f), new Vector2(4.1f, 3.9f), "56616A", "BCD0D7");
            MoveStation(world, "study", new Vector2(-9.1f, 2.45f), "ACA6C1");
            MoveStation(world, "training", new Vector2(-4.6f, 2.45f), "C69581");
            MoveStation(world, "rehearsal", new Vector2(0, 2.45f), "BDA7CB");
            MoveStation(world, "sorting", new Vector2(5.5f, 2.45f), "C6A275");
            MoveStation(world, "copy", new Vector2(10, 2.45f), "91ADAE");
            MoveStation(world, "warehouse", new Vector2(5.5f, -2.9f), "C3A576");
            MoveStation(world, "visitorDesk", new Vector2(10, -2.9f), "7FB4A7");
            MoveStation(world, "shop", new Vector2(-9.1f, -2.9f), "BDA67C");
            MoveStation(world, "rest", new Vector2(-4.6f, -2.9f), "9EBB82");
            MoveStation(world, "practiceReport", new Vector2(0, -2.9f), "A5B8CA");
            player.transform.position = new Vector3(-2.2f, 0, 0);
            var view = Camera.main;
            if (view == null) throw new InvalidOperationException("Sandbox camera is missing.");
            view.orthographicSize = Mathf.Max(8f, 13.6f / view.aspect);
            view.transform.position = new Vector3(0, 0, -10);
            if (view.GetComponent<SandboxOverviewCamera>() == null) view.gameObject.AddComponent<SandboxOverviewCamera>();
        }

        private static void Zone(Transform parent, string id, string title, Vector2 center, Vector2 size, string floor, string accent)
        {
            WorldRect(parent, id, center, size, floor, -800);
            WorldRect(parent, id + "Accent", center + new Vector2(0, size.y / 2), new Vector2(size.x, .06f), accent, -799);
            WorldLabel(parent, id + "Title", title, center + new Vector2(0, size.y / 2 - .38f), size.x, 3.8f, accent);
        }

        private static void MoveStation(Transform world, string id, Vector2 position, string color)
        {
            var station = world.Find(id);
            if (station == null || station.GetComponent<SandboxStation>() == null)
                throw new InvalidOperationException("Missing station " + id);
            WorldRect(world, id + "Pad", position, new Vector2(3.5f, 2.65f), "4B564B", -500);
            WorldRect(world, id, position, new Vector2(2.35f, 1.05f), color, 20, true);
            WorldLabel(station, "Label", station.GetComponent<SandboxStation>().DisplayName, new Vector2(0, 1.03f), 3.65f, 3.8f, "FFF0CD");
            WorldRect(station, "Accent", new Vector2(0, .38f), new Vector2(2.1f, .08f), "F0DFC1", 21);
        }

        private static void WorldLabel(Transform parent, string id, string value, Vector2 position, float width, float size, string color)
        {
            var found = parent.Find(id);
            var label = found != null ? found.GetComponent<TextMeshPro>() : new GameObject(id, typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            label.transform.SetParent(parent, false);
            label.transform.localPosition = new Vector3(position.x, position.y, -.1f);
            label.font = body; label.fontSize = size; label.color = ColorOf(color); label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(width, .65f);
            label.GetComponent<MeshRenderer>().sortingOrder = 200; label.text = value;
        }

        private static void Layout(MechanicsSandboxUI ui)
        {
            var root = ui.transform;
            var header = Panel(root, "Header", 18, 18, 330, 88);
            var oldTitle = header.Find("Title");
            if (oldTitle != null) oldTitle.gameObject.SetActive(false);
            ui.Resources = Label(header, "Resources", "", 18, 14, 10, 302, 25);
            ui.Progress = Label(header, "Progress", "", 15, 14, 40, 302, 23);
            ui.Progress.color = ColorOf("C9D9AC");
            var energyBar = Rect(header, "EnergyBar");
            Place(energyBar, 14, 69, 302, 7);
            ui.EnergyBar = InterfaceRefresh.ConfigureEnergyMeter(energyBar);
            var toolbar = Rect(root, "Toolbar"); Place(toolbar, 0, 0, 448, 36);
            toolbar.anchorMin = toolbar.anchorMax = toolbar.pivot = Vector2.one;
            toolbar.anchoredPosition = new Vector2(-18, -18);
            foreach (string name in new[] { "Inventory", "Help", "Skills", "TaskToggle" })
            {
                var existing = header.Find(name);
                if (existing != null) existing.SetParent(toolbar, false);
            }
            ui.SkillsButton = Button(toolbar, "Skills", "C  Персонаж", 0, 0, 132, 36);
            ui.InventoryButton = Button(toolbar, "Inventory", "I  Рюкзак", 140, 0, 104, 36);
            ui.HelpButton = Button(toolbar, "Help", "Esc", 372, 0, 76, 36);
            ui.Tasks = ui.GetComponent<TaskTrackerPanel>();
            ui.Tasks.ToggleButton = Button(toolbar, "TaskToggle", "T  Задачи", 252, 0, 112, 36);
            var tasks = Panel(root, "TaskPanel", 0, 0, 312, 294);
            tasks.anchorMin = tasks.anchorMax = tasks.pivot = Vector2.one; tasks.anchoredPosition = new Vector2(-18, -64);
            ui.Tasks.Panel = tasks.gameObject;
            ui.Tasks.Caption = Label(tasks, "Caption", "Задачи [T]", 20, 14, 12, 284, 29, true);
            ui.Tasks.Entries = Label(tasks, "Entries", "", 15, 14, 54, 284, 224);
            ui.Tasks.Entries.lineSpacing = 5;
            tasks.gameObject.SetActive(false);

            var prompt = Panel(root, "PromptCard", 0, 0, 650, 38); Bottom(prompt, 18, 42);
            ui.PromptCard = prompt.gameObject; ui.Prompt = Label(prompt, "Prompt", "", 17, 12, 7, 626, 26);
            var toast = Panel(root, "ToastCard", 0, 0, 760, 48); Bottom(toast, 18, 91);
            ui.ToastCard = toast.gameObject; ui.Toast = Label(toast, "Toast", "", 14, 12, 6, 736, 38);
            var controls = Label(root, "Controls", "WASD / стрелки   движение     E   действие     C   персонаж     I   рюкзак     T   задачи", 13, 0, 0, 930, 22);
            Bottom(controls.rectTransform, 18, 13);

            var shade = Rect(root, "Backdrop"); shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one; shade.offsetMin = shade.offsetMax = Vector2.zero;
            var image = Component<UnityEngine.UI.Image>(shade); image.color = new Color(0, 0, 0, .35f); image.raycastTarget = true;
            ui.Backdrop = shade.gameObject;
            var window = Panel(root, "Window", 0, 0, 780, 540);
            window.anchorMin = window.anchorMax = window.pivot = new Vector2(.5f, .5f); window.anchoredPosition = Vector2.zero;
            window.GetComponent<UnityEngine.UI.Image>().color = new Color32(48, 36, 29, 242);
            ui.Window = window.gameObject;
            ui.Title = Label(window, "Title", "", 26, 24, 20, 732, 36, true);
            ui.Body = Label(window, "Body", "", 18, 24, 74, 732, 162);
            ui.Choices = new UnityEngine.UI.Button[5];
            for (int i = 0; i < ui.Choices.Length; i++) ui.Choices[i] = Button(window, "Choice" + i, "", 24, 244 + i * 43, 732, 38);
            ui.InventoryView = BuildInventory(window);
            var oldCharacter = window.Find("CharacterSheet");
            if (oldCharacter != null) oldCharacter.SetParent(root, false);
            ui.CharacterView = BuildCharacter(root);
            ui.CloseButton = Button(window, "Close", "Закрыть / Esc", 24, 468, 732, 44);
            shade.SetAsLastSibling(); window.SetAsLastSibling();
            ui.Window.SetActive(false); ui.Backdrop.SetActive(false);
            ui.PromptCard.SetActive(false); ui.ToastCard.SetActive(false);
            EditorUtility.SetDirty(ui); EditorUtility.SetDirty(ui.Tasks);
        }

        private static SandboxCharacterView BuildCharacter(Transform root)
        {
            var area = Panel(root, "CharacterSheet", 18, 118, 330, 438);
            var view = Component<SandboxCharacterView>(area);
            Label(area, "Title", "Персонаж [C]", 20, 14, 12, 254, 28, true);
            view.CloseButton = Button(area, "Close", "×", 286, 10, 30, 30);
            var summary = Panel(area, "Summary", 12, 48, 306, 86);
            summary.GetComponent<UnityEngine.UI.Image>().color = ColorOf("443A2D");
            view.Level = Label(summary, "Level", "", 17, 10, 2, 286, 24, true);
            view.Level.color = ColorOf("EBC88B");
            view.Experience = Label(summary, "Experience", "", 13, 10, 28, 286, 20);
            view.ExperienceFill = ProgressBar(summary, "ExperienceBar", 10, 51, 286, "D8B779");
            view.Limit = Label(summary, "Limit", "", 13, 10, 64, 286, 19);
            view.Limit.color = ColorOf("CBBEA6");
            view.SkillLevels = new TMP_Text[3]; view.SkillExperience = new TMP_Text[3]; view.SkillAccess = new TMP_Text[3];
            view.SkillFills = new UnityEngine.UI.Image[3];
            string[] names = { "Интеллект", "Сила", "Харизма" };
            string[] colors = { "AFC5D8", "DBAD8B", "CBB1D3" };
            for (int i = 0; i < 3; i++)
            {
                var card = Panel(area, "Skill" + i, 12, 142 + i * 65, 306, 60);
                card.GetComponent<UnityEngine.UI.Image>().color = ColorOf("40382F");
                foreach (string unused in new[] { "Accent", "Practice" })
                {
                    var old = card.Find(unused);
                    if (old != null) old.gameObject.SetActive(false);
                }
                Label(card, "Name", names[i], 17, 10, 3, 200, 24, true).color = ColorOf(colors[i]);
                view.SkillLevels[i] = Label(card, "Level", "", 17, 220, 3, 76, 24, true);
                view.SkillLevels[i].alignment = TextAlignmentOptions.TopRight;
                view.SkillExperience[i] = Label(card, "Experience", "", 13, 10, 29, 134, 20);
                view.SkillFills[i] = ProgressBar(card, "Track", 10, 52, 286, colors[i]);
                view.SkillAccess[i] = Label(card, "Access", "", 13, 148, 29, 148, 20);
                view.SkillAccess[i].alignment = TextAlignmentOptions.TopRight;
            }
            var status = Panel(area, "Status", 12, 344, 306, 82);
            status.GetComponent<UnityEngine.UI.Image>().color = ColorOf("38332B");
            foreach (string unused in new[] { "ConditionTitle", "RestTitle", "ReadinessTitle" })
            {
                var old = status.Find(unused);
                if (old != null) old.gameObject.SetActive(false);
            }
            view.Condition = Label(status, "Condition", "", 14, 10, 6, 286, 22);
            view.Rest = Label(status, "Rest", "", 14, 10, 30, 286, 22);
            view.Readiness = Label(status, "Readiness", "", 14, 10, 54, 286, 22);
            area.gameObject.SetActive(false);
            EditorUtility.SetDirty(view);
            return view;
        }

        private static UnityEngine.UI.Image ProgressBar(Transform parent, string name, float x, float y, float width, string color)
        {
            var track = Panel(parent, name, x, y, width, 6);
            track.GetComponent<UnityEngine.UI.Image>().color = ColorOf("665A48");
            var fill = Rect(track, "Fill"); Place(fill, 0, 0, width, 6);
            var image = Component<UnityEngine.UI.Image>(fill); image.sprite = square;
            image.type = UnityEngine.UI.Image.Type.Filled; image.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            image.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            image.color = ColorOf(color); image.raycastTarget = false;
            return image;
        }

        private static SandboxInventoryView BuildInventory(RectTransform window)
        {
            var area = Rect(window, "Backpack");
            Place(area, 0, 0, 780, 452);
            var view = Component<SandboxInventoryView>(area);

            view.BookSlot = InventorySlot(area, "Book", 24);
            view.BookIcon = Icon(view.BookSlot.transform, "Icon", BookSprite(), 29, 17, 48);
            view.BookLabel = Label(view.BookSlot.transform, "Label", "Учебник", 14, 5, 76, 96, 23);
            view.BookLabel.alignment = TextAlignmentOptions.Center;
            view.BookCount = Label(view.BookSlot.transform, "Count", "", 13, 69, 7, 29, 18);
            view.BookCount.alignment = TextAlignmentOptions.TopRight;

            var oldKit = area.Find("Kit");
            if (oldKit != null) oldKit.name = "Snack";
            view.SnackSlot = InventorySlot(area, "Snack", 142);
            view.SnackIcon = Icon(view.SnackSlot.transform, "Icon", SnackSprite(), 29, 17, 48);
            view.SnackLabel = Label(view.SnackSlot.transform, "Label", "Перекус", 14, 5, 76, 96, 23);
            view.SnackLabel.alignment = TextAlignmentOptions.Center;
            view.SnackCount = Label(view.SnackSlot.transform, "Count", "", 13, 69, 7, 29, 18);
            view.SnackCount.alignment = TextAlignmentOptions.TopRight;

            var oldEmpty = area.Find("EmptySlot2");
            if (oldEmpty != null) oldEmpty.name = "EnergyDrink";
            view.DrinkSlot = InventorySlot(area, "EnergyDrink", 260);
            view.DrinkIcon = Icon(view.DrinkSlot.transform, "Icon", DrinkSprite(), 29, 17, 48);
            view.DrinkLabel = Label(view.DrinkSlot.transform, "Label", "Энергетик", 14, 5, 76, 96, 23);
            view.DrinkLabel.alignment = TextAlignmentOptions.Center;
            view.DrinkCount = Label(view.DrinkSlot.transform, "Count", "", 13, 69, 7, 29, 18);
            view.DrinkCount.alignment = TextAlignmentOptions.TopRight;

            var oldCourseSlot = area.Find("EmptySlot3");
            if (oldCourseSlot != null) oldCourseSlot.name = "TrainingCourse";
            view.CourseSlot = InventorySlot(area, "TrainingCourse", 24, 198);
            view.CourseIcon = Icon(view.CourseSlot.transform, "Icon", TrainingCourseSprite(), 29, 17, 48);
            view.CourseLabel = Label(view.CourseSlot.transform, "Label", "Курс", 14, 5, 76, 96, 23);
            view.CourseLabel.alignment = TextAlignmentOptions.Center;
            view.CourseCount = Label(view.CourseSlot.transform, "Count", "", 13, 69, 7, 29, 18);
            view.CourseCount.alignment = TextAlignmentOptions.TopRight;

            var oldGuideSlot = area.Find("EmptySlot4");
            if (oldGuideSlot != null) oldGuideSlot.name = "CommunicationGuide";
            view.GuideSlot = InventorySlot(area, "CommunicationGuide", 142, 198);
            view.GuideIcon = Icon(view.GuideSlot.transform, "Icon", CommunicationGuideSprite(), 29, 17, 48);
            view.GuideLabel = Label(view.GuideSlot.transform, "Label", "Практикум", 14, 5, 76, 96, 23);
            view.GuideLabel.alignment = TextAlignmentOptions.Center;
            view.GuideCount = Label(view.GuideSlot.transform, "Count", "", 13, 69, 7, 29, 18);
            view.GuideCount.alignment = TextAlignmentOptions.TopRight;

            var empty = Panel(area, "EmptySlot5", 260, 198, 106, 106);
            empty.GetComponent<UnityEngine.UI.Image>().color = ColorOf("3C2E24");

            view.Feedback = Label(area, "Feedback", "", 16, 24, 326, 342, 112);
            view.Feedback.color = ColorOf("E4C68B");
            view.Feedback.textWrappingMode = TextWrappingModes.Normal;

            var details = Panel(area, "Details", 388, 80, 368, 362);
            details.GetComponent<UnityEngine.UI.Image>().color = ColorOf("413126");
            view.Category = Label(details, "Category", "", 14, 18, 17, 332, 23);
            view.Category.color = ColorOf("DDB477");
            view.ItemName = Label(details, "ItemName", "", 22, 18, 41, 332, 40, true);
            view.PreviewIcon = Icon(details, "Preview", null, 18, 92, 72);
            view.Description = Label(details, "Description", "", 17, 106, 94, 244, 94);
            view.Description.textWrappingMode = TextWrappingModes.Normal;
            var effect = Panel(details, "EffectPanel", 18, 196, 332, 72);
            effect.GetComponent<UnityEngine.UI.Image>().color = ColorOf("30261F");
            view.Effect = Label(effect, "Effect", "", 16, 12, 12, 308, 51);
            view.Effect.color = ColorOf("E4C68B");
            view.Effect.textWrappingMode = TextWrappingModes.Normal;
            view.UseButton = Button(details, "Use", "Применить", 18, 292, 332, 45);
            view.UseLabel = view.UseButton.GetComponentInChildren<TMP_Text>();
            EditorUtility.SetDirty(view);
            area.gameObject.SetActive(false);
            return view;
        }

        private static UnityEngine.UI.Button InventorySlot(RectTransform parent, string name, float x, float y = 80)
        {
            var slot = Button(parent, name, "", x, y, 106, 106);
            var colors = slot.colors;
            colors.disabledColor = Color.white;
            slot.colors = colors;
            return slot;
        }

        private static UnityEngine.UI.Image Icon(Transform parent, string name, Sprite sprite, float x, float y, float size)
        {
            var rect = Rect(parent, name);
            Place(rect, x, y, size, size);
            var icon = Component<UnityEngine.UI.Image>(rect);
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return icon;
        }

        private static Sprite BookSprite()
        {
            return CreateItemSprite("InventoryBook", new[]
            {
                "................",
                "....111111111...",
                "...12222222221..",
                "..123333333321..",
                "..123333333321..",
                "..123444443321..",
                "..123444443321..",
                "..123333333321..",
                "..123344333321..",
                "..123333333321..",
                "..122222222221..",
                "..155555555511..",
                "..155555555511..",
                "...1111111111...",
                "................",
                "................"
            }, "392718", "AD7735", "E3B45F", "FCE6AC", "F6DCA0");
        }

        private static Sprite SnackSprite()
        {
            return CreateItemSprite("InventorySnack", new[]
            {
                "................",
                "................",
                "................",
                ".....111111.....",
                "...1122222211...",
                "..122222222221..",
                ".12222222222221.",
                ".13333333333331.",
                ".14444444444441.",
                ".13333333333331.",
                ".12222222222221.",
                "..122222222221..",
                "...1111111111...",
                "................",
                "................",
                "................"
            }, "56351D", "F0BA6D", "82A156", "B85737");
        }

        private static Sprite DrinkSprite()
        {
            return CreateItemSprite("InventoryEnergyDrink", new[]
            {
                "................",
                ".....111111.....",
                "....12222221....",
                "....13333331....",
                "....13343331....",
                "....13443331....",
                "....14443331....",
                "....13344431....",
                "....13344331....",
                "....13343331....",
                "....13333331....",
                "....13333331....",
                "....12222221....",
                ".....111111.....",
                "................",
                "................"
            }, "392A2C", "DDD2BC", "984A61", "FFC573");
        }

        private static Sprite TrainingCourseSprite()
        {
            return CreateItemSprite("InventoryTrainingCourse", new[]
            {
                "................",
                "...1111111111...",
                "..122222222221..",
                "..122223322221..",
                "..122222222221..",
                "..123333333321..",
                "..124343434421..",
                "..124444444421..",
                "..124343434421..",
                "..123333333321..",
                "..122222222221..",
                "..122333333221..",
                "..122222222221..",
                "..111111111111..",
                "................",
                "................"
            }, "4B2D2D", "C98769", "EDCBA0", "735650");
        }

        private static Sprite CommunicationGuideSprite()
        {
            return CreateItemSprite("InventoryCommunicationGuide", new[]
            {
                "................",
                "...1111111111...",
                "..122222222221..",
                "..122222222221..",
                "..122333333221..",
                "..123444444321..",
                "..123444444321..",
                "..122333333221..",
                "..122232222221..",
                "..122222222221..",
                "..122444442221..",
                "..122222222221..",
                "..122222222221..",
                "..111111111111..",
                "................",
                "................"
            }, "342839", "8A6F9A", "D2B0CF", "F0DFC6");
        }

        private static Sprite CreateItemSprite(string name, string[] rows, params string[] palette)
        {
            string path = Root + "Art/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            int width = rows[0].Length;
            int height = rows.Length;
            var colors = palette.Select(ColorOf).ToArray();
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                if (rows[y].Length != width) throw new InvalidOperationException("Invalid icon row width for " + name);
                for (int x = 0; x < width; x++)
                {
                    char pixel = rows[y][x];
                    pixels[(height - 1 - y) * width + x] = pixel == '.' ? Color.clear : colors[pixel - '1'];
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name + "Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            AssetDatabase.CreateAsset(sprite, path);
            AssetDatabase.AddObjectToAsset(texture, sprite);
            EditorUtility.SetDirty(sprite);
            return sprite;
        }

        private static RectTransform Panel(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = Rect(parent, name); Place(rect, x, y, w, h);
            var image = Component<UnityEngine.UI.Image>(rect); image.sprite = rounded; image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2; image.color = new Color32(48, 36, 29, 215); image.raycastTarget = false;
            return rect;
        }
        private static TMP_Text Label(Transform parent, string name, string text, int size, float x, float y, float w, float h, bool title = false)
        {
            var rect = Rect(parent, name); Place(rect, x, y, w, h);
            var label = Component<TextMeshProUGUI>(rect); label.font = title ? heading : body; label.fontSize = size;
            label.color = ColorOf("F4E7C9"); label.raycastTarget = false; label.enableAutoSizing = false;
            label.text = text; label.margin = Vector4.zero; label.alignment = TextAlignmentOptions.TopLeft;
            return label;
        }
        private static UnityEngine.UI.Button Button(Transform parent, string name, string text, float x, float y, float w, float h)
        {
            var rect = Panel(parent, name, x, y, w, h); rect.GetComponent<UnityEngine.UI.Image>().color = ColorOf("63513B");
            rect.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var button = Component<UnityEngine.UI.Button>(rect); button.targetGraphic = rect.GetComponent<UnityEngine.UI.Image>();
            var colors = button.colors; colors.highlightedColor = ColorOf("FFE1AE"); colors.pressedColor = ColorOf("C1AC89");
            colors.disabledColor = new Color(.65f, .65f, .65f, .6f); button.colors = colors;
            var label = Label(rect, "Label", text, 16, 8, 5, w - 16, h - 10); label.alignment = TextAlignmentOptions.Center;
            return button;
        }
        private static RectTransform Rect(Transform parent, string name)
        {
            var existing = parent.Find(name) as RectTransform; if (existing != null) return existing;
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
        }
        private static T Component<T>(RectTransform rect) where T : Component { var value = rect.GetComponent<T>(); return value != null ? value : rect.gameObject.AddComponent<T>(); }
        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
        }
        private static void Bottom(RectTransform rect, float x, float y)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero; rect.anchoredPosition = new Vector2(x, y);
        }
        private static Color ColorOf(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var color); return color; }

        public static void BuildWindows()
        {
            string destination = Path.GetFullPath("../Builds/MechanicsSandbox/MezhduParami.exe");
            var args = Environment.GetCommandLineArgs();
            int output = Array.IndexOf(args, "--sandbox-build-output");
            if (output >= 0 && output + 1 < args.Length) destination = Path.GetFullPath(args[output + 1]);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath, PrototypeSceneBuilder.ScenePath }, locationPathName = destination,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Build failed: " + report.summary.result);
        }
    }
}

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
            Layout(ui);
            player.InputGate = ui;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true), new EditorBuildSettingsScene(PrototypeSceneBuilder.ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Mechanics sandbox ready: shop, inventory, two short jobs, study, rest and toggleable tasks. Campaign scene preserved.");
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
        private static void Station(Transform parent, string id, string caption, SandboxStationKind kind, Vector2 position, string color)
        {
            WorldRect(parent, id + "Pad", position, new Vector2(3.4f, 2.7f), "596653", -500);
            var rect = WorldRect(parent, id, position, new Vector2(2.2f, 1.2f), color, 20, true);
            var component = rect.AddComponent<SandboxStation>(); component.Id = "sandbox." + id; component.Kind = kind; component.DisplayName = caption;
            var text = new GameObject("Label", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            text.transform.SetParent(rect.transform, false); text.transform.localPosition = new Vector3(0, 1.15f, -.1f);
            text.font = body; text.fontSize = 2.8f; text.color = ColorOf("FFF0CD"); text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(3.8f, .65f); text.GetComponent<MeshRenderer>().sortingOrder = 200;
            text.text = caption;
        }
        private static GameObject WorldRect(Transform parent, string name, Vector2 position, Vector2 size, string color, int order, bool collision = false)
        {
            var go = new GameObject(name, typeof(SpriteRenderer)); go.transform.SetParent(parent, false); go.transform.localPosition = position;
            var sprite = go.GetComponent<SpriteRenderer>(); sprite.sprite = square; sprite.sharedMaterial = material;
            sprite.drawMode = SpriteDrawMode.Sliced; sprite.size = size; sprite.color = ColorOf(color); sprite.sortingOrder = order;
            if (collision) go.AddComponent<BoxCollider2D>().size = size;
            return go;
        }

        private static void Layout(MechanicsSandboxUI ui)
        {
            var root = ui.transform;
            var header = Panel(root, "Header", 18, 18, 910, 102);
            Label(header, "Title", "ПОЛИГОН МЕХАНИК • БЕЗ КАЛЕНДАРЯ", 22, 14, 10, 750, 30, true);
            ui.Resources = Label(header, "Resources", "", 17, 14, 47, 650, 48);
            ui.InventoryButton = Button(header, "Inventory", "I  Вещи", 682, 57, 100, 32);
            ui.HelpButton = Button(header, "Help", "Esc  Меню", 790, 12, 106, 30);
            ui.Tasks = ui.GetComponent<TaskTrackerPanel>();
            ui.Tasks.ToggleButton = Button(header, "TaskToggle", "T  Задачи", 790, 57, 106, 32);
            var tasks = Panel(root, "TaskPanel", 0, 0, 312, 296);
            tasks.anchorMin = tasks.anchorMax = tasks.pivot = Vector2.one; tasks.anchoredPosition = new Vector2(-18, -18);
            ui.Tasks.Panel = tasks.gameObject;
            ui.Tasks.Caption = Label(tasks, "Caption", "Задачи [T]", 20, 14, 12, 284, 29, true);
            ui.Tasks.Entries = Label(tasks, "Entries", "", 15, 14, 54, 284, 224);
            tasks.gameObject.SetActive(true);

            var prompt = Panel(root, "PromptCard", 0, 0, 650, 38); Bottom(prompt, 18, 42);
            ui.PromptCard = prompt.gameObject; ui.Prompt = Label(prompt, "Prompt", "", 17, 12, 7, 626, 26);
            var toast = Panel(root, "ToastCard", 0, 0, 760, 48); Bottom(toast, 18, 91);
            ui.ToastCard = toast.gameObject; ui.Toast = Label(toast, "Toast", "", 14, 12, 6, 736, 38);
            var controls = Label(root, "Controls", "WASD / стрелки   движение     E   действие     T   задачи     I   вещи     Esc   меню", 12, 0, 0, 880, 20);
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
            ui.Choices = new UnityEngine.UI.Button[4];
            for (int i = 0; i < 4; i++) ui.Choices[i] = Button(window, "Choice" + i, "", 24, 248 + i * 48, 732, 40);
            ui.CloseButton = Button(window, "Close", "Закрыть / Esc", 24, 468, 732, 44);
            shade.SetAsLastSibling(); window.SetAsLastSibling();
            ui.Window.SetActive(false); ui.Backdrop.SetActive(false);
            ui.PromptCard.SetActive(false); ui.ToastCard.SetActive(false);
            EditorUtility.SetDirty(ui); EditorUtility.SetDirty(ui.Tasks);
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

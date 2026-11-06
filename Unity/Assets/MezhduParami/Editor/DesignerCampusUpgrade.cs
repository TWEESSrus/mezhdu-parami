using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MezhduParami.Editor
{
    public static class DesignerCampusUpgrade
    {
        private const string Root = "Assets/MezhduParami";
        private const string Art = Root + "/Art/DesignerCampus/";
        private static readonly Rect MapBounds = new Rect(-20.48f, -20.48f, 40.96f, 40.96f);
        private static readonly Vector2 Spawn = new Vector2(-4.25f, 5.9f);
        // The fence keeps the hero inside the visible area below the HUD.
        private static readonly Rect NorthFence = new Rect(-20.35f, 17.95f, 40.7f, 2.1f);

        // World-space rectangles from the designer's SampleScene (54a2b4d).
        // The original collision root's translation is included here.
        private static readonly Rect[] DesignerObstacles =
        {
            new Rect(-12.5078f, 15.7499f, 29.5158f, 1.6194f),
            new Rect(-8.8107f, 9.4359f, 0.9629f, 7.9335f),
            new Rect(3.6551f, 9.7123f, 0.9022f, 7.6571f),
            new Rect(-8.8638f, 7.7117f, 0.9945f, 0.9984f),
            new Rect(16.1220f, 9.7061f, 0.8962f, 7.6964f),
            new Rect(17.1195f, -2.7825f, 0.8519f, 14.4111f),
            new Rect(7.4766f, -2.7825f, 10.5029f, 2.0885f),
            new Rect(7.1109f, -3.5796f, 0.8862f, 2.1410f),
            new Rect(9.8596f, -0.6952f, 0.9045f, 8.3672f),
            new Rect(4.7169f, -3.6127f, 0.8776f, 2.1676f),
            new Rect(0.5610f, -2.7856f, 4.7638f, 2.0926f),
            new Rect(-1.7396f, 2.2778f, 0.7223f, 0.6908f),
            new Rect(-1.5971f, 2.3093f, 0.3800f, 0.9313f),
            new Rect(-2.7721f, 5.2727f, 0.5633f, 1.6729f),
            new Rect(-2.2199f, 4.7847f, 2.7724f, 2.2230f),
            new Rect(-15.8334f, 9.7044f, 9.9797f, 1.8905f),
            new Rect(0.5324f, -2.6292f, 0.8956f, 10.3086f),
            new Rect(1.5790f, 9.7202f, 2.9842f, 1.8643f),
            new Rect(-4.6631f, 9.6770f, 5.0819f, 1.9169f),
            new Rect(-2.6020f, 9.6779f, 0.9149f, 7.6998f),
            new Rect(12.8818f, 6.2515f, 5.0809f, 1.4577f),
            new Rect(2.5819f, 6.2539f, 9.3117f, 1.4382f),
            new Rect(-3.6951f, 6.4498f, 5.2304f, 1.2417f),
            new Rect(-3.4314f, 4.1423f, 0.5715f, 1.6043f),
            new Rect(-3.4644f, 5.8125f, 0.6540f, 1.1758f),
            new Rect(-7.6838f, 5.8125f, 0.6484f, 1.1538f),
            new Rect(-5.7047f, 6.2999f, 0.7553f, 1.6314f),
            new Rect(-15.8394f, 6.4558f, 0.8297f, 10.9175f),
            new Rect(-15.8496f, 6.4451f, 9.0139f, 1.2195f)
        };

        [MenuItem("Между парами/Подключить карту дизайнера")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before updating the scene.");
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (active.isDirty) throw new InvalidOperationException("Save the active scene first.");
            var scene = active.path == PrototypeSceneBuilder.ScenePath ? active : EditorSceneManager.OpenScene(PrototypeSceneBuilder.ScenePath);
            var existing = GameObject.Find("CampusEnvironment");
            if (existing != null)
            {
                var wall = existing.transform.Find("CampusObstacles").GetComponentsInChildren<BoxCollider2D>()
                    .Single(box => box.size.x > 40f && box.transform.position.y > 0f);
                wall.transform.position = NorthFence.center;
                wall.size = NorthFence.size;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                return;
            }
            var room = Find("DormRoom");
            var desk = Find("StudyDesk");
            var bed = Find("Bed");
            var board = Find("Noticeboard");
            var sasha = Find("Sasha");
            var motor = Find("Player").GetComponent<PlayerMotor>();
            var camera = Camera.main;
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/PrototypeUnlit.mat");
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/PrototypeSquare.png");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/PrototypeCyrillic.asset");

            string mapPath = Art + "CampusMap.jpeg";
            ConfigureTexture(mapPath, 4096, false);
            ConfigureTexture(Art + "anim.png", 8192, true);
            var mapSprite = AssetDatabase.LoadAllAssetsAtPath(mapPath).OfType<Sprite>().Single();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Art + "anim_3.controller");
            var clip = controller != null ? controller.animationClips.SingleOrDefault() : null;
            if (clip == null || !clip.isLooping) throw new InvalidOperationException("Designer walk cycle is missing or not looping.");
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
            var frames = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (frames.Length != 19 || frames.Any(frame => !(frame.value is Sprite)))
                throw new InvalidOperationException("Could not resolve all 19 designer sprite frames.");

            var campus = new GameObject("CampusEnvironment").transform;
            var backdrop = NewChild(campus, "CampusMap", Vector2.zero).AddComponent<SpriteRenderer>();
            backdrop.sprite = mapSprite;
            backdrop.sharedMaterial = material;
            backdrop.sortingOrder = -2000;
            backdrop.transform.localScale = new Vector3(MapBounds.width / mapSprite.bounds.size.x, MapBounds.height / mapSprite.bounds.size.y, 1f);

            var obstacles = NewChild(campus, "CampusObstacles", Vector2.zero).transform;
            foreach (var rect in DesignerObstacles) AddObstacle(obstacles, rect);
            AddObstacle(obstacles, new Rect(-20.35f, -15.2f, 0.65f, 35.2f));
            AddObstacle(obstacles, new Rect(19.7f, -15.2f, 0.65f, 35.2f));
            AddObstacle(obstacles, new Rect(-20.35f, -15.35f, 40.7f, 0.85f));
            AddObstacle(obstacles, NorthFence);
            // Furniture baked into the map image needs physical footprints.
            AddObstacle(obstacles, new Rect(12.05f, 11.85f, 2f, 2.7f));

            PlaceInteraction(desk, campus, new Vector2(7.6f, 13.5f), new Vector2(2f, 2.6f));
            PlaceInteraction(bed, campus, new Vector2(-7.15f, 14.7f), new Vector2(1.3f, 2.6f));
            PlaceInteraction(board, campus, new Vector2(-3.55f, 9.95f), new Vector2(1.7f, 0.45f), true);
            sasha.transform.SetParent(campus, false);
            sasha.transform.position = new Vector2(-4.15f, 4.4f);
            room.SetActive(false);
            AddLabel(campus, desk.transform.position + new Vector3(0, -0.75f), "СТОЛ / УЧЁБА", font, square, material);
            AddLabel(campus, bed.transform.position + new Vector3(0, 0.4f), "КРОВАТЬ", font, square, material);
            AddLabel(campus, board.transform.position + new Vector3(0, 0.55f), "ПОДРАБОТКА", font, square, material);
            AddLabel(campus, sasha.transform.position + new Vector3(0, 1.03f), "САША", font, square, material);

            motor.transform.position = Spawn;
            foreach (var renderer in motor.GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
            var hero = NewChild(motor.transform, "DesignerHero", new Vector2(0f, 0.38f));
            hero.transform.localScale = Vector3.one * 1.35f;
            var sprite = hero.AddComponent<SpriteRenderer>();
            sprite.sprite = (Sprite)frames[0].value;
            sprite.sharedMaterial = material;
            var animator = hero.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            motor.WalkAnimator = animator;

            // The illustrated backdrop and character atlas have different source PPU.
            // Keep their native sampling instead of forcing the placeholder 320x180 grid.
            camera.GetComponent<PixelPerfectCamera>().enabled = false;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(Spawn.x, Spawn.y, camera.transform.position.z);
            var follow = camera.gameObject.AddComponent<CampusCamera>();
            follow.Target = motor.transform;
            follow.MapBounds = MapBounds;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save the campus scene.");
            AssetDatabase.SaveAssets();
            Debug.Log("[MezhduParami] Designer campus imported: 29 source obstacles and 19 walk frames.");
        }

        private static GameObject Find(string name) => GameObject.Find(name)
            ?? throw new InvalidOperationException("Missing scene object: " + name);

        private static void ConfigureTexture(string path, int maxSize, bool point)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter
                ?? throw new InvalidOperationException("Missing artwork: " + path);
            importer.filterMode = point ? FilterMode.Point : FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = maxSize;
            var settings = importer.GetDefaultPlatformTextureSettings();
            settings.maxTextureSize = maxSize;
            settings.textureCompression = point ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
            importer.SetPlatformTextureSettings(settings);
            var standalone = importer.GetPlatformTextureSettings("Standalone");
            if (standalone.overridden)
            {
                standalone.maxTextureSize = maxSize;
                standalone.textureCompression = settings.textureCompression;
                importer.SetPlatformTextureSettings(standalone);
            }
            importer.SaveAndReimport();
        }

        private static GameObject NewChild(Transform parent, string name, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go;
        }

        private static void AddObstacle(Transform parent, Rect rect) =>
            NewChild(parent, "Obstacle", rect.center).AddComponent<BoxCollider2D>().size = rect.size;

        private static void PlaceInteraction(GameObject go, Transform parent, Vector2 position, Vector2 size, bool trigger = false)
        {
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            foreach (var renderer in go.GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
            var shape = go.GetComponent<BoxCollider2D>();
            shape.size = size;
            shape.isTrigger = trigger;
        }

        private static void AddLabel(Transform parent, Vector2 position, string caption, TMP_FontAsset font, Sprite square, Material material)
        {
            var plate = NewChild(parent, caption, position).AddComponent<SpriteRenderer>();
            plate.sprite = square;
            plate.sharedMaterial = material;
            plate.drawMode = SpriteDrawMode.Sliced;
            plate.size = new Vector2(2.35f, 0.36f);
            plate.color = new Color32(28, 43, 52, 238);
            plate.sortingOrder = 200;
            var text = NewChild(plate.transform, "Text", Vector2.zero).AddComponent<TextMeshPro>();
            text.font = font;
            text.text = caption;
            text.fontSize = 2.5f;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(2.35f, 0.36f);
            text.GetComponent<MeshRenderer>().sortingOrder = 201;
        }
    }
}

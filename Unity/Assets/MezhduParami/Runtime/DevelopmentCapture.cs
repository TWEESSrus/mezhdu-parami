#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;

namespace MezhduParami
{
    // Opt-in rendering check of the real Windows player, including overlay UI.
    // In ordinary launches no object is created and the game behaves normally.
    public sealed class DevelopmentCapture : MonoBehaviour
    {
        private string destination;
        private string scenario = "selection";
        private int width = 1280, height = 720;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--capture-prototype");
            if (index < 0 || index + 1 >= args.Length) return;
            var capture = new GameObject("DevelopmentCapture").AddComponent<DevelopmentCapture>();
            capture.destination = args[index + 1];
            int scenarioIndex = Array.IndexOf(args, "--capture-scenario");
            if (scenarioIndex >= 0 && scenarioIndex + 1 < args.Length) capture.scenario = args[scenarioIndex + 1];
            capture.width = CaptureDimension(args, "--capture-width", 1280);
            capture.height = CaptureDimension(args, "--capture-height", 720);
        }
        private static int CaptureDimension(string[] args, string flag, int fallback)
        {
            int index = Array.IndexOf(args, flag);
            return index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out int value)
                && value >= 480 && value <= 3840 ? value : fallback;
        }
        private IEnumerator Start()
        {
            yield return new WaitUntil(() => UnityEngine.Rendering.SplashScreen.isFinished);
            yield return PrepareScenario();
            // Native captures keep the actual viewport and Overlay pipeline.
            // The offscreen fallback below is useful for typography only and
            // must not be used as evidence that window compositing is correct.
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--capture-native") >= 0)
            {
                yield return new WaitForSecondsRealtime(2f);
                yield return new WaitForEndOfFrame();
                var native = ScreenCapture.CaptureScreenshotAsTexture();
                System.IO.File.WriteAllBytes(destination, native.EncodeToPNG());
                Destroy(native);
                Debug.Log("Native capture: viewport=" + Camera.main.rect + ", UI=" + FindFirstObjectByType<PrototypeUI>().GetComponent<Canvas>().renderMode);
                Application.Quit();
                yield break;
            }
            // A hidden Windows window may have an unpresented black backbuffer.
            // Render the same camera and UI into an explicit target for this
            // opt-in check instead of depending on the desktop swapchain.
            var camera = Camera.main;
            var target = new RenderTexture(width, height, 24);
            target.Create();
            camera.targetTexture = target;
            camera.aspect = (float)width / height;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                canvas.overrideSorting = true;
                canvas.sortingOrder = short.MaxValue;
            }
            yield return null;
            Canvas.ForceUpdateCanvases();
            foreach (var text in FindFirstObjectByType<PrototypeUI>().GetComponentsInChildren<TMPro.TMP_Text>())
            {
                text.ForceMeshUpdate();
                if (text.isTextOverflowing) Debug.LogError("QA text overflow: " + text.name);
            }
            yield return new WaitForEndOfFrame();
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            System.IO.File.WriteAllBytes(destination, texture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();
            Destroy(target);
            Destroy(texture);
            Debug.Log("Prototype rendering captured.");
            Application.Quit();
        }

        private IEnumerator PrepareScenario()
        {
            if (scenario == "selection") yield break;
            var ui = FindFirstObjectByType<PrototypeUI>();
            ui.SelectStudent(true);
            yield return null;
            if (scenario == "campus") yield break;
            if (scenario == "away")
            {
                ui.Player.transform.position = new Vector3(-4.25f, 0f, 0f);
                ui.Player.GetComponent<Rigidbody2D>().position = ui.Player.transform.position;
                yield return new WaitForSeconds(1f);
                yield break;
            }
            if (scenario == "library" || scenario == "north")
            {
                var at = scenario == "north" ? new Vector3(-4.25f, 17.68f)
                    : GameObject.Find("StudyDesk").transform.position + Vector3.down * 2f;
                ui.Player.transform.position = at;
                ui.Player.GetComponent<Rigidbody2D>().position = at;
                yield return new WaitForSeconds(1f);
                yield break;
            }
            if (scenario == "offer")
            {
                ui.OpenInteraction(GameObject.Find("Sasha").GetComponent<WorldInteraction>());
                yield break;
            }
            yield return Confirm(ui, "Sasha");
            if (scenario == "journal") { ui.OpenJournal(); yield break; }
            while (ui.Day.DayNumber < 3)
            {
                while (!ui.Day.IsComplete) yield return Confirm(ui, "Bed");
                if (scenario == "night") yield break;
                ui.PrimaryButton.onClick.Invoke();
                yield return null;
            }
            while (ui.Day.Period < 2) yield return Confirm(ui, "Bed");
            ui.OpenInteraction(GameObject.Find(scenario == "fulfilled" ? "Sasha" : "Noticeboard").GetComponent<WorldInteraction>());
            if (scenario == "conflict") yield break;
            yield return null;
            ui.PrimaryButton.onClick.Invoke();
            yield return null;
        }

        private static IEnumerator Confirm(PrototypeUI ui, string target)
        {
            ui.OpenInteraction(GameObject.Find(target).GetComponent<WorldInteraction>());
            ui.PrimaryButton.onClick.Invoke();
            yield return null;
        }
    }
}
#endif

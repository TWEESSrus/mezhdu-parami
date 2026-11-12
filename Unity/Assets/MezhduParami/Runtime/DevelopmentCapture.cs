#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            if (FindFirstObjectByType<MechanicsSandboxUI>() != null && scenario.StartsWith("campus-"))
            {
                DontDestroyOnLoad(gameObject);
                yield return SceneManager.LoadSceneAsync("PrototypeDorm");
                yield return null;
            }
            yield return PrepareScenario();
            Component uiRoot = FindFirstObjectByType<PrototypeUI>();
            if (uiRoot == null) uiRoot = FindFirstObjectByType<MechanicsSandboxUI>();
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
                Debug.Log("Native capture: viewport=" + Camera.main.rect + ", UI=" + uiRoot.GetComponent<Canvas>().renderMode);
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
            foreach (var text in uiRoot.GetComponentsInChildren<TMPro.TMP_Text>())
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
            var sandbox = FindFirstObjectByType<MechanicsSandboxUI>();
            if (sandbox != null)
            {
                if (scenario == "shop") sandbox.OpenStation(SandboxStationKind.Shop);
                else if (scenario == "purchase")
                {
                    sandbox.OpenStation(SandboxStationKind.Shop);
                    yield return null;
                    sandbox.Choices[0].onClick.Invoke();
                }
                else if (scenario == "inventory-empty") sandbox.OpenInventory();
                else if (scenario == "inventory" || scenario == "inventory-snack" || scenario == "inventory-after-use")
                {
                    sandbox.State.BuyBook(out _);
                    sandbox.State.BuySnack(out _);
                    sandbox.State.BuySnack(out _);
                    sandbox.Refresh();
                    sandbox.OpenInventory();
                    if (scenario != "inventory") sandbox.InventoryView.SnackSlot.onClick.Invoke();
                    if (scenario == "inventory-after-use") sandbox.InventoryView.UseButton.onClick.Invoke();
                }
                else if (scenario == "fatigue" || scenario == "rested")
                {
                    for (int i = 0; i < 6; i++)
                    {
                        if (i == 3) sandbox.State.Rest(out _);
                        sandbox.State.Study(out _);
                    }
                    sandbox.State.Rest(out _);
                    for (int i = 0; i < 2; i++)
                    {
                        sandbox.State.BuySnack(out _);
                        sandbox.State.EatSnack(out _);
                    }
                    sandbox.Refresh();
                    if (scenario == "rested")
                    {
                        sandbox.OpenStation(SandboxStationKind.Rest);
                        yield return null;
                        sandbox.Choices[1].onClick.Invoke();
                    }
                }
                else if (scenario == "drink-item" || scenario == "drink-active" || scenario == "drink-spent")
                {
                    for (int i = 0; i < 3; i++) sandbox.State.Study(out _);
                    for (int i = 0; i < 2; i++)
                    {
                        sandbox.State.BuySnack(out _);
                        sandbox.State.EatSnack(out _);
                    }
                    sandbox.State.BuyEnergyDrink(out _);
                    if (scenario == "drink-item")
                        sandbox.OpenInventory();
                    else
                    {
                        sandbox.State.DrinkEnergyDrink(out _);
                        sandbox.State.TickEnergy(3f);
                        if (scenario == "drink-spent") sandbox.State.Study(out _);
                    }
                    sandbox.Refresh();
                }
                else if (scenario == "sorting" || scenario == "copy")
                {
                    if (scenario == "copy") { sandbox.State.BuyBook(out _); sandbox.State.ReadBook(out _); sandbox.Refresh(); }
                    sandbox.OpenStation(scenario == "copy" ? SandboxStationKind.CopyCenter : SandboxStationKind.Sorting);
                    yield return null; sandbox.Choices[0].onClick.Invoke();
                }
                else if (scenario == "tasks-hidden") sandbox.Tasks.Toggle();
                else if (scenario == "completed")
                {
                    sandbox.State.BuyBook(out _); sandbox.State.ReadBook(out _);
                    sandbox.State.BuySnack(out _); sandbox.State.EatSnack(out _);
                    sandbox.State.BeginWork(SandboxJob.Sorting, 7, out _); var sort = sandbox.State.ActiveOrder;
                    for (int i = 0; i < 3; i++) sandbox.State.Sort(sort.Id, sort.Target(i), out _);
                    sandbox.State.BeginWork(SandboxJob.CopyCenter, 8, out _); var copy = sandbox.State.ActiveOrder;
                    sandbox.State.Copy(copy.Id, copy.Target(0), copy.Target(1), copy.Target(2), out _);
                    sandbox.Refresh();
                }
                yield return null;
                yield break;
            }
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
            if (scenario == "campus-bed" || scenario == "campus-morning")
            {
                yield return Confirm(ui, "Bed");
                yield return Confirm(ui, "Bed");
                ui.OpenInteraction(GameObject.Find("Bed").GetComponent<WorldInteraction>());
                if (scenario == "campus-morning") ui.SleepOption.onClick.Invoke();
                yield break;
            }
            yield return Confirm(ui, "Sasha");
            if (scenario == "journal") { ui.OpenJournal(); yield break; }
            if (scenario == "campus-night" || scenario == "campus-sleepless")
            {
                yield return Confirm(ui, "StudyDesk");
                yield return Confirm(ui, "StudyDesk");
                if (scenario == "campus-sleepless") ui.SleepOption.onClick.Invoke();
                yield break;
            }
            while (ui.Day.DayNumber < 3)
            {
                while (!ui.Day.IsComplete) yield return Confirm(ui, ui.Day.RestsRemaining > 0 ? "Bed" : "StudyDesk");
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

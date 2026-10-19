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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--capture-prototype");
            if (index < 0 || index + 1 >= args.Length) return;
            var capture = new GameObject("DevelopmentCapture").AddComponent<DevelopmentCapture>();
            capture.destination = args[index + 1];
        }
        private IEnumerator Start()
        {
            yield return new WaitUntil(() => UnityEngine.Rendering.SplashScreen.isFinished);
            // A hidden Windows window may have an unpresented black backbuffer.
            // Render the same camera and UI into an explicit target for this
            // opt-in check instead of depending on the desktop swapchain.
            var camera = Camera.main;
            var target = new RenderTexture(1280, 720, 24);
            target.Create();
            camera.targetTexture = target;
            camera.aspect = 1280f / 720f;
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
            yield return new WaitForEndOfFrame();
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
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
    }
}
#endif

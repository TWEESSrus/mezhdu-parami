using UnityEngine;

namespace MezhduParami
{
    [RequireComponent(typeof(Camera))]
    public sealed class SandboxOverviewCamera : MonoBehaviour
    {
        private Camera view;
        private void Awake() => view = GetComponent<Camera>();
        private void LateUpdate()
        {
            // Keep every station inside the overview when the window is resized.
            view.orthographicSize = Mathf.Max(8f, 13.6f / view.aspect);
        }
    }
}

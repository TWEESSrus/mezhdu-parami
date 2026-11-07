using UnityEngine;
using UnityEngine.InputSystem;

namespace MezhduParami
{
    public sealed class SandboxInteractor : MonoBehaviour
    {
        public MechanicsSandboxUI UI;
        private void Update()
        {
            if (UI == null || UI.InputBlocked) return;
            SandboxStation nearest = null;
            float distance = float.PositiveInfinity;
            foreach (var hit in Physics2D.OverlapCircleAll(transform.position, 1.25f))
            {
                var station = hit.GetComponent<SandboxStation>();
                if (station == null) continue;
                float d = ((Vector2)transform.position - hit.ClosestPoint(transform.position)).sqrMagnitude;
                if (d >= distance) continue;
                nearest = station; distance = d;
            }
            UI.SetPrompt(nearest == null ? "" : "E — " + nearest.DisplayName);
            if (nearest != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                UI.OpenStation(nearest.Kind);
        }
    }
}

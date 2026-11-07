using UnityEngine;
using UnityEngine.InputSystem;

namespace MezhduParami
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        public PrototypeUI UI;
        [SerializeField] private float radius = 1.15f;
        private WorldInteraction target;

        private void Update()
        {
            if (UI == null || UI.InputBlocked) return;
            target = null;
            float closest = float.PositiveInfinity;
            foreach (var hit in Physics2D.OverlapCircleAll(transform.position, radius))
            {
                var candidate = hit.GetComponentInParent<WorldInteraction>();
                if (candidate == null) continue;
                float distance = ((Vector2)transform.position - hit.ClosestPoint(transform.position)).sqrMagnitude;
                if (distance >= closest) continue;
                closest = distance;
                target = candidate;
            }
            UI.SetPrompt(target == null ? ""
                : "E — " + target.DisplayName);
            if (target != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                UI.OpenInteraction(target);
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace MezhduParami
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public PrototypeUI UI;
        public SpriteRenderer Shirt;
        [SerializeField] private float speed = 3f;
        private Rigidbody2D body;
        private Vector2 movement;
        public float Speed => speed;

        private void Awake() => body = GetComponent<Rigidbody2D>();

        public static Vector2 NormalizeInput(Vector2 input) => Vector2.ClampMagnitude(input, 1f);

        private void Update()
        {
            movement = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard == null || UI == null || UI.InputBlocked) return;
            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)
                - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            movement = NormalizeInput(new Vector2(x, y));
        }

        private void FixedUpdate()
        {
            body.linearVelocity = UI != null && !UI.InputBlocked ? movement * speed : Vector2.zero;
        }

        private void LateUpdate()
        {
            var group = GetComponent<SortingGroup>();
            if (group != null) group.sortingOrder = Mathf.RoundToInt(-transform.position.y * 16);
        }

        private void OnDisable()
        {
            movement = Vector2.zero;
            if (body != null) body.linearVelocity = Vector2.zero;
        }
    }
}

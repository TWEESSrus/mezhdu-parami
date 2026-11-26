using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace MezhduParami
{
    public interface IPlayerInputGate { bool InputBlocked { get; } }

    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public PrototypeUI UI;
        public MonoBehaviour InputGate;
        public SpriteRenderer Shirt;
        public Animator WalkAnimator;
        [SerializeField] private float speed = 3f;

        private Rigidbody2D body;
        private SortingGroup sortingGroup;
        private Vector2 movement;
        private bool walking;
        private int currentStateHash;

        // Точные имена состояний из Main_walk_anim_controller
        private static readonly int WalkDownHash = Animator.StringToHash("walk_front_clip");
        private static readonly int WalkUpHash = Animator.StringToHash("walk_back_clip");
        private static readonly int WalkLeftHash = Animator.StringToHash("walk_left_clip");
        private static readonly int WalkRightHash = Animator.StringToHash("walk_right_clip");

        public float Speed => speed;
        private bool IsBlocked => InputGate is IPlayerInputGate gate ? gate.InputBlocked : UI == null || UI.InputBlocked;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            sortingGroup = GetComponent<SortingGroup>();
            currentStateHash = WalkDownHash;
        }

        public static Vector2 NormalizeInput(Vector2 input) => Vector2.ClampMagnitude(input, 1f);

        private void Update()
        {
            movement = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard == null || IsBlocked) return;

            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)
                - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);

            movement = NormalizeInput(new Vector2(x, y));
        }

        private void FixedUpdate()
        {
            body.linearVelocity = !IsBlocked ? movement * speed : Vector2.zero;
        }

        private void LateUpdate()
        {
            if (sortingGroup != null)
                sortingGroup.sortingOrder = Mathf.RoundToInt(-transform.position.y * 16);

            if (WalkAnimator == null) return;

            bool moving = !IsBlocked && movement.sqrMagnitude > 0f && body.linearVelocity.sqrMagnitude > 0.01f;

            if (moving)
            {
                int targetHash = GetDirectionHash(movement);

                if (!walking || targetHash != currentStateHash)
                {
                    currentStateHash = targetHash;
                    WalkAnimator.Play(currentStateHash, 0, 0f);
                }

                WalkAnimator.speed = 1f;
            }
            else
            {
                WalkAnimator.speed = 0f;
                if (walking)
                {
                    WalkAnimator.Play(currentStateHash, 0, 0f);
                }
            }

            walking = moving;
        }

        private int GetDirectionHash(Vector2 dir)
        {
            if (Mathf.Abs(dir.y) >= Mathf.Abs(dir.x))
            {
                return dir.y > 0 ? WalkUpHash : WalkDownHash;
            }
            return dir.x > 0 ? WalkRightHash : WalkLeftHash;
        }

        private void OnDisable()
        {
            movement = Vector2.zero;
            if (body != null) body.linearVelocity = Vector2.zero;
        }
    }
}

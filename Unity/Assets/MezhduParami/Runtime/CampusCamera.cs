using UnityEngine;

namespace MezhduParami
{
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(50)]
    public sealed class CampusCamera : MonoBehaviour
    {
        public Transform Target;
        public Rect MapBounds;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.18f;
        private Camera view;
        private Vector2 velocity;

        private void Awake()
        {
            view = GetComponent<Camera>();
            if (Target != null) PositionAt(ClampToMap(Target.position));
        }

        private void LateUpdate()
        {
            if (Target == null) return;
            var target = ClampToMap(Target.position);
            PositionAt(ClampToMap(Vector2.SmoothDamp(transform.position, target, ref velocity, smoothTime)));
        }

        private Vector2 ClampToMap(Vector2 position)
        {
            float halfHeight = view.orthographicSize;
            float halfWidth = halfHeight * view.aspect;
            return new Vector2(
                ClampAxis(position.x, MapBounds.xMin, MapBounds.xMax, halfWidth),
                ClampAxis(position.y, MapBounds.yMin, MapBounds.yMax, halfHeight));
        }

        private static float ClampAxis(float value, float min, float max, float halfView) =>
            max - min > halfView * 2f ? Mathf.Clamp(value, min + halfView, max - halfView) : (min + max) * 0.5f;

        private void PositionAt(Vector2 position) => transform.position = new Vector3(position.x, position.y, transform.position.z);
    }
}

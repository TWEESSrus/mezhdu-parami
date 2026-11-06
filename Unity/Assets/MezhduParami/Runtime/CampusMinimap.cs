using UnityEngine;

namespace MezhduParami
{
    // The image represents the entire campus, with north at the top. Marker
    // RectTransforms are direct children of MapArea; no render camera is needed.
    public sealed class CampusMinimap : MonoBehaviour
    {
        public RectTransform MapArea;
        public RectTransform PlayerMarker;
        public Transform Player;
        public Rect WorldBounds = new Rect(-20.48f, -20.48f, 40.96f, 40.96f);
        public Camera WorldCamera;
        public RectTransform Viewport;

        private void LateUpdate()
        {
            if (MapArea == null) return;
            if (PlayerMarker != null && Player != null)
            {
                AlignMarkerAnchor(PlayerMarker);
                PlayerMarker.anchoredPosition = WorldToMapPosition(Player.position, WorldBounds, MapArea.rect);
            }

            if (Viewport == null || WorldCamera == null || !WorldCamera.orthographic) return;
            // This is an optional indicator of the existing gameplay camera's
            // footprint, not a second camera rendering the map.
            float halfHeight = WorldCamera.orthographicSize;
            float halfWidth = halfHeight * WorldCamera.aspect;
            Vector2 center = WorldCamera.transform.position;
            var min = WorldToMapPosition(center - new Vector2(halfWidth, halfHeight), WorldBounds, MapArea.rect);
            var max = WorldToMapPosition(center + new Vector2(halfWidth, halfHeight), WorldBounds, MapArea.rect);
            AlignMarkerAnchor(Viewport);
            Viewport.pivot = new Vector2(0.5f, 0.5f);
            Viewport.anchoredPosition = (min + max) * 0.5f;
            Viewport.sizeDelta = max - min;
        }

        private void AlignMarkerAnchor(RectTransform marker)
        {
            // The parent pivot is local (0,0), including for top-left layouts.
            // This keeps the same conversion valid when the HUD is resized.
            if (marker.anchorMin != MapArea.pivot) marker.anchorMin = MapArea.pivot;
            if (marker.anchorMax != MapArea.pivot) marker.anchorMax = MapArea.pivot;
        }

        public static Vector2 WorldToMapPosition(Vector2 worldPosition, Rect worldBounds, Rect mapRect)
        {
            if (worldBounds.width <= 0f || worldBounds.height <= 0f) return mapRect.center;
            float x = Mathf.InverseLerp(worldBounds.xMin, worldBounds.xMax, worldPosition.x);
            float y = Mathf.InverseLerp(worldBounds.yMin, worldBounds.yMax, worldPosition.y);
            return new Vector2(Mathf.Lerp(mapRect.xMin, mapRect.xMax, x), Mathf.Lerp(mapRect.yMin, mapRect.yMax, y));
        }
    }
}

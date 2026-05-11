#pragma warning disable UDR0001
using System.Linq;
using PigBrain.LegacyCore.Utility;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PigBrain.LegacyCore.UI
{
    public static class RectTransformX
    {
        static Vector3[] Corners = new Vector3[4];

        public static Vector2 GetLocalPosition(this RectTransform rect, PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
               rect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            return localPoint;
        }

        #region Corners
        // 1---2
        // |   |
        // 0---3
        public static Vector3[] GetWorldCorners(this RectTransform rect)
        {
            rect.GetWorldCorners(Corners);
            return Corners.ToArray();
        }
        public static Vector3 GetWorldCenter(this RectTransform rect) =>
            rect.GetWorldCorners().Aggregate(Vector3.zero, (a, c) => a + c) / 4f;
        public static Vector3 GetWorldSize(this RectTransform rect)
        {
            Vector3[] c = rect.GetLocalCorners();
            return c[2] - c[0];
        }

        // 1---2
        // |   |
        // 0---3
        public static Vector3[] GetLocalCorners(this RectTransform rect)
        {
            rect.GetLocalCorners(Corners);
            return Corners.ToArray();
        }
        public static Vector3 GetLocalCenter(this RectTransform rect) =>
            rect.GetLocalCorners().Aggregate(Vector3.zero, (a, c) => a + c) / 4f;
        public static Vector3 GetLocalSize(this RectTransform rect)
        {
            Vector3[] c = rect.GetLocalCorners();
            return c[2] - c[0];
        }
        #endregion


        #region Distances
        public static float DistanceToRect(this RectTransform a, RectTransform b, Camera cam)
        {
            a.GetWorldCorners(Corners);
            Vector2[] ascr = Corners.Select(c => RectTransformUtility.WorldToScreenPoint(cam, c)).ToArray();
            float aminX = ascr.Min(c => c.x), amaxX = ascr.Max(c => c.x);
            float aminY = ascr.Min(c => c.y), amaxY = ascr.Max(c => c.y);

            b.GetWorldCorners(Corners);
            Vector2[] bscr = Corners.Select(c => RectTransformUtility.WorldToScreenPoint(cam, c)).ToArray();
            float bminX = bscr.Min(c => c.x), bmaxX = bscr.Max(c => c.x);
            float bminY = bscr.Min(c => c.y), bmaxY = bscr.Max(c => c.y);

            float dx = Mathf.Max(aminX - bmaxX, bminX - amaxX, 0);
            float dy = Mathf.Max(aminY - bmaxY, bminY - amaxY, 0);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public static float DistanceToEdge(this RectTransform rt, Vector2 screen, Camera cam)
        {
            Vector2[] corners = rt.GetWorldCorners()
                .Select(c => RectTransformUtility.WorldToScreenPoint(cam, c)).ToArray();
            float minX = corners.Min(c => c.x), maxX = corners.Max(c => c.x);
            float minY = corners.Min(c => c.y), maxY = corners.Max(c => c.y);
            float dx = Mathf.Max(minX - screen.x, 0, screen.x - maxX);
            float dy = Mathf.Max(minY - screen.y, 0, screen.y - maxY);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public static T FindClosestEdge<T>(this Canvas canvas, Vector2 screenPos, float maxDistance = 10000) where T : Behaviour
        {
            (T item, float dist) closest = (null, maxDistance);
            foreach (var handler in canvas.GetComponentsInChildren<T>().Where(b => b.enabled))
            {
                float dist = (handler.transform as RectTransform).DistanceToEdge(screenPos, canvas.worldCamera);
                if (dist < closest.dist) closest = (handler, dist);
            }
            return closest.item;
        }
        #endregion
    }
}
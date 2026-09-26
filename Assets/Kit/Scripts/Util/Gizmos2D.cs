using UnityEngine;

namespace ArenaKit
{
    /// <summary>Draws the shape of 2D colliders in the Scene view, so invisible zones can be seen while building.</summary>
    public static class Gizmos2D
    {
        public static void DrawCollider(Collider2D collider, Color color)
        {
            if (collider == null)
                return;

            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = collider.transform.localToWorldMatrix;

            switch (collider)
            {
                case BoxCollider2D box:
                    Gizmos.color = color;
                    Gizmos.DrawCube(box.offset, box.size);
                    Gizmos.color = new Color(color.r, color.g, color.b, 1f);
                    Gizmos.DrawWireCube(box.offset, box.size);
                    break;

                case CircleCollider2D circle:
                    Gizmos.color = new Color(color.r, color.g, color.b, 1f);
                    Gizmos.DrawWireSphere(circle.offset, circle.radius);
                    break;

                default:
                    Gizmos.matrix = previous;
                    Gizmos.color = new Color(color.r, color.g, color.b, 1f);
                    Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
                    break;
            }

            Gizmos.matrix = previous;
        }

        public static void DrawLabel(Vector3 position, string text, Color color)
        {
#if UNITY_EDITOR
            var style = new GUIStyle(UnityEditor.EditorStyles.boldLabel) { normal = { textColor = color }, alignment = TextAnchor.MiddleCenter };
            UnityEditor.Handles.Label(position, text, style);
#endif
        }
    }
}

using UnityEngine;

namespace ArenaKit
{
    /// <summary>
    /// Where a player starts (and comes back after dying, if the level rules allow it).
    /// Place one for each player. Only visible in the Scene view.
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [Tooltip("Which player starts here")]
        [Range(1, GameManager.MaxPlayers)] public int PlayerNumber = 1;

        [Tooltip("Weapon the player holds right from the start. Leave empty to start without a weapon")]
        public Weapon StartWeapon;

        void OnDrawGizmos()
        {
            Color color = GameManager.GetPlayerColor(PlayerNumber);
            Vector3 position = transform.position;

            Gizmos.color = new Color(color.r, color.g, color.b, 0.35f);
            Gizmos.DrawCube(position, new Vector3(0.8f, 1.6f, 0f));
            Gizmos.color = color;
            Gizmos.DrawWireCube(position, new Vector3(0.8f, 1.6f, 0f));
            Gizmos.DrawLine(position + Vector3.down * 0.8f, position + Vector3.down * 1.2f);

#if UNITY_EDITOR
            var style = new GUIStyle(UnityEditor.EditorStyles.boldLabel) { normal = { textColor = color }, alignment = TextAnchor.MiddleCenter };
            UnityEditor.Handles.Label(position + Vector3.up * 1.2f, $"P{PlayerNumber}", style);
#endif
        }
    }
}

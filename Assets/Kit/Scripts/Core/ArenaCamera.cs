using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ArenaKit
{
    /// <summary>
    /// Duck Game camera: always shows all living players and zooms in and out to fit them.
    /// It never shows anything outside the level. The level size is taken from the painted
    /// Ground and Platform tiles, so your level can be as big as you like.
    /// Works with every screen shape (16:9, 16:10, 4:3...).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class ArenaCamera : MonoBehaviour
    {
        public static ArenaCamera Current { get; private set; }

        [Header("Zoom")]
        [Tooltip("Closest zoom when the players are near each other (half of the visible height, in tiles)")]
        [Min(2f)] public float MinSize = 6f;
        [Tooltip("Widest zoom when the players are far apart (half of the visible height, in tiles)")]
        [Min(2f)] public float MaxSize = 14f;
        [Tooltip("How much of the screen the players fill. Smaller = more space around them")]
        [Range(0.3f, 1f)] public float Framing = 0.6f;
        [Tooltip("How smoothly the camera moves and zooms. 0 = instantly")]
        [Range(0f, 5f)] public float Smoothing = 1f;

        [Header("Level Bounds")]
        [Tooltip("On = the camera stays inside the painted Ground and Platform tiles. Off = set the size of the Bounds box yourself")]
        public bool AutoBounds = true;
        [Tooltip("Extra space around the tiles (only with Auto Bounds)")]
        [Min(0f)] public float BoundsPadding = 1f;
        [Tooltip("The camera never shows anything outside this box")]
        public BoxCollider2D Bounds;

        [Header("Shake")]
        [Tooltip("How much explosions shake the camera. 0 = no shake")]
        [Range(0f, 3f)] public float ShakeStrength = 1f;

        [Header("Parts (set up by the kit)")]
        public Camera OutputCamera;
        public CinemachineCamera VirtualCamera;
        public CinemachineTargetGroup TargetGroup;
        public CinemachinePositionComposer Composer;
        public CinemachineGroupFraming GroupFraming;
        public ArenaConfiner Confiner;
        public CinemachineImpulseSource Impulse;
        [Tooltip("The camera looks here (at the whole level) while no player is alive")]
        public Transform LevelCenter;

        public const float PlayerRadius = 1.5f;
        static readonly Vector2 DefaultLevelSize = new Vector2(32f, 18f);
        const float EditorBoundsInterval = 1f;

        float nextEditorBoundsUpdate;

        void OnEnable()
        {
            Current = this;
            ApplySettings();
            UpdateBounds();
            UpdateFallbackTarget();
        }

        void OnDisable()
        {
            if (Current == this)
                Current = null;
        }

        void OnValidate()
        {
            if (MaxSize < MinSize)
                MaxSize = MinSize;
            ApplySettings();

#if UNITY_EDITOR
            // Colliders can't be changed directly in OnValidate.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled)
                    UpdateBounds();
            };
#endif
        }

        void Start()
        {
            // All tilemaps are loaded now.
            if (Application.isPlaying)
                UpdateBounds();
        }

        void LateUpdate()
        {
            // While building the level, follow the painted tiles.
            if (!Application.isPlaying && Time.realtimeSinceStartup >= this.nextEditorBoundsUpdate)
            {
                this.nextEditorBoundsUpdate = Time.realtimeSinceStartup + EditorBoundsInterval;
                UpdateBounds();
            }

            RemoveMissingTargets();
            UpdateFallbackTarget();
            LimitZoomToBounds();
        }

        // ---------------------------------------------------------------
        // Targets
        // ---------------------------------------------------------------

        /// <summary>The camera keeps this object on screen.</summary>
        public void AddTarget(Transform target, float weight = 1f, float radius = PlayerRadius)
        {
            if (TargetGroup == null || target == null)
                return;
            if (TargetGroup.FindMember(target) < 0)
                TargetGroup.AddMember(target, weight, radius);
            UpdateFallbackTarget();
        }

        /// <summary>The camera stops following this object.</summary>
        public void RemoveTarget(Transform target)
        {
            if (TargetGroup == null || target == null)
                return;
            TargetGroup.RemoveMember(target);
            UpdateFallbackTarget();
        }

        public static void Follow(Transform target, float weight = 1f, float radius = PlayerRadius)
        {
            if (Current != null)
                Current.AddTarget(target, weight, radius);
        }

        public static void Unfollow(Transform target)
        {
            if (Current != null)
                Current.RemoveTarget(target);
        }

        /// <summary>Shakes the camera, e.g. for explosions.</summary>
        public static void Shake(float amount)
        {
            if (Current == null || Current.Impulse == null || Current.ShakeStrength <= 0f || !Application.isPlaying)
                return;
            Current.Impulse.GenerateImpulseWithForce(amount * Current.ShakeStrength);
        }

        void RemoveMissingTargets()
        {
            if (TargetGroup == null)
                return;
            for (int i = TargetGroup.Targets.Count - 1; i >= 0; i--)
                if (TargetGroup.Targets[i].Object == null)
                    TargetGroup.Targets.RemoveAt(i);
        }

        /// <summary>Without anybody to follow, the camera shows the whole level.</summary>
        void UpdateFallbackTarget()
        {
            if (TargetGroup == null || LevelCenter == null)
                return;

            bool hasTargets = false;
            foreach (CinemachineTargetGroup.Target target in TargetGroup.Targets)
                if (target.Object != null && target.Object != LevelCenter && target.Weight > 0f)
                    hasTargets = true;

            int index = TargetGroup.FindMember(LevelCenter);
            float radius = FallbackRadius();
            if (hasTargets && index >= 0)
                TargetGroup.RemoveMember(LevelCenter);
            else if (!hasTargets && index < 0)
                TargetGroup.AddMember(LevelCenter, 1f, radius);
            else if (!hasTargets && !Mathf.Approximately(TargetGroup.Targets[index].Radius, radius))
                TargetGroup.Targets[index].Radius = radius;
        }

        float FallbackRadius()
        {
            Rect level = BoundsRect();
            return Mathf.Max(level.width, level.height) * 0.5f;
        }

        // ---------------------------------------------------------------
        // Zoom and bounds
        // ---------------------------------------------------------------

        void ApplySettings()
        {
            if (GroupFraming != null)
            {
                GroupFraming.FramingMode = CinemachineGroupFraming.FramingModes.HorizontalAndVertical;
                GroupFraming.SizeAdjustment = CinemachineGroupFraming.SizeAdjustmentModes.ZoomOnly;
                GroupFraming.LateralAdjustment = CinemachineGroupFraming.LateralAdjustmentModes.ChangePosition;
                GroupFraming.FramingSize = Framing;
                GroupFraming.Damping = Smoothing;
            }

            if (Composer != null)
                Composer.Damping = new Vector3(Smoothing * 0.5f, Smoothing * 0.5f, 0f);

            LimitZoomToBounds();
        }

        /// <summary>
        /// Zoom out at most until the whole level is visible. On screens that are wider or narrower than
        /// the level a bit of background shows above/below or left/right, but players never leave the screen.
        /// </summary>
        void LimitZoomToBounds()
        {
            if (GroupFraming == null)
                return;

            float aspect = OutputCamera != null && OutputCamera.aspect > 0.01f ? OutputCamera.aspect : 16f / 9f;
            Rect level = BoundsRect();
            float showsWholeLevel = Mathf.Max(level.height * 0.5f, level.width * 0.5f / aspect);
            float max = Mathf.Max(1f, Mathf.Min(MaxSize, showsWholeLevel));
            float min = Mathf.Min(MinSize, max);

            var range = new Vector2(min, max);
            if (GroupFraming.OrthoSizeRange != range)
                GroupFraming.OrthoSizeRange = range;
        }

        /// <summary>The area the camera may show, in world units.</summary>
        public Rect LevelRect => BoundsRect();

        Rect BoundsRect()
        {
            if (Bounds == null)
                return new Rect(-DefaultLevelSize * 0.5f, DefaultLevelSize);

            // Calculated from the box itself, so it is also correct before the physics ran.
            Vector2 center = Bounds.transform.TransformPoint(Bounds.offset);
            Vector2 size = Vector2.Scale(Bounds.size, Bounds.transform.lossyScale);
            size = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
            return new Rect(center - size * 0.5f, size);
        }

        /// <summary>Recalculates the level bounds from the painted tiles.</summary>
        public void UpdateBounds()
        {
            if (Bounds == null)
                return;

            if (AutoBounds)
            {
                Rect tiles = FindTileBounds(out bool found);
                if (!found)
                    tiles = new Rect(-DefaultLevelSize * 0.5f, DefaultLevelSize);
                tiles.xMin -= BoundsPadding;
                tiles.yMin -= BoundsPadding;
                tiles.xMax += BoundsPadding;
                tiles.yMax += BoundsPadding;

                Vector2 offset = tiles.center - (Vector2)Bounds.transform.position;
                if (Bounds.offset != offset || Bounds.size != tiles.size)
                {
                    Bounds.offset = offset;
                    Bounds.size = tiles.size;
                }
            }

            if (Confiner != null && Confiner.Rig != this)
                Confiner.Rig = this;

            if (LevelCenter != null)
            {
                Vector3 center = BoundsRect().center;
                if ((LevelCenter.position - center).sqrMagnitude > 0.0001f)
                    LevelCenter.position = center;
            }

            LimitZoomToBounds();
        }

        /// <summary>The area covered by solid tiles (Ground and Platforms, not decoration or hazards).</summary>
        static Rect FindTileBounds(out bool found)
        {
            found = false;
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);

            foreach (Tilemap tilemap in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                var tilemapCollider = tilemap.GetComponent<TilemapCollider2D>();
                if (tilemapCollider == null || tilemapCollider.isTrigger)
                    continue;

                BoundsInt cells = tilemap.cellBounds;
                foreach (Vector3Int cell in cells.allPositionsWithin)
                {
                    if (!tilemap.HasTile(cell))
                        continue;
                    Vector2 cellMin = tilemap.CellToWorld(cell);
                    Vector2 cellMax = tilemap.CellToWorld(cell + new Vector3Int(1, 1, 0));
                    min = Vector2.Min(min, Vector2.Min(cellMin, cellMax));
                    max = Vector2.Max(max, Vector2.Max(cellMin, cellMax));
                    found = true;
                }
            }

            return found ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : default;
        }

        void OnDrawGizmos()
        {
            Rect level = BoundsRect();
            Gizmos.color = new Color(1f, 1f, 1f, 0.6f);
            Gizmos.DrawWireCube(level.center, level.size);
        }
    }
}

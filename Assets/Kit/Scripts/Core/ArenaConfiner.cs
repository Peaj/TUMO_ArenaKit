using Unity.Cinemachine;
using UnityEngine;

namespace ArenaKit
{
    /// <summary>
    /// Keeps the camera view inside the level box of the ArenaCamera. Where the view is wider (or taller)
    /// than the level, the level is centered instead.
    /// Replaces Cinemachine's Confiner2D, which works with precomputed polygon shapes and made the camera
    /// jump at the zoom where the view stops fitting into the level in one direction.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class ArenaConfiner : CinemachineExtension
    {
        [Tooltip("The camera rig whose level bounds are used")]
        public ArenaCamera Rig;

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            // After Body: the position composer and the group framing (zoom) are done.
            if (stage != CinemachineCore.Stage.Body || Rig == null || !state.Lens.Orthographic)
                return;

            Rect level = Rig.LevelRect;
            float halfHeight = state.Lens.OrthographicSize;
            float halfWidth = halfHeight * state.Lens.Aspect;
            Vector3 position = state.GetCorrectedPosition();

            float x = Confine(position.x, halfWidth, level.xMin, level.xMax);
            float y = Confine(position.y, halfHeight, level.yMin, level.yMax);
            state.PositionCorrection += new Vector3(x - position.x, y - position.y, 0f);
        }

        /// <summary>Clamps the view (center ± half) into [min, max], or centers it if it is larger.</summary>
        static float Confine(float center, float half, float min, float max)
        {
            if (half * 2f >= max - min)
                return (min + max) * 0.5f;
            return Mathf.Clamp(center, min + half, max - half);
        }
    }
}

using UnityEngine;
using UnityEngine.Rendering;

namespace ArenaKit
{
    /// <summary>
    /// Stretches the background picture so it always fills the whole screen,
    /// no matter where the camera is, how far it zooms or which screen shape is used.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class CameraBackground : MonoBehaviour
    {
        [Tooltip("The camera the background fills. Empty = the main camera")]
        public Camera TargetCamera;

        SpriteRenderer spriteRenderer;

        void OnEnable()
        {
            this.spriteRenderer = GetComponent<SpriteRenderer>();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera rendering)
        {
            Camera target = TargetCamera != null ? TargetCamera : Camera.main;
            if (rendering == target)
                Fit(target);
        }

        void Fit(Camera target)
        {
            if (target == null || !target.orthographic || this.spriteRenderer == null || this.spriteRenderer.sprite == null)
                return;

            float viewHeight = target.orthographicSize * 2f;
            float viewWidth = viewHeight * target.aspect;
            Vector2 spriteSize = this.spriteRenderer.sprite.bounds.size;
            if (spriteSize.y <= 0f)
                return;

            // Scale so the picture covers the height, repeat it sideways to cover the width.
            float scale = viewHeight / spriteSize.y * 1.02f;
            var size = new Vector2(viewWidth / scale + 0.1f, spriteSize.y);

            this.spriteRenderer.drawMode = SpriteDrawMode.Tiled;
            if (this.spriteRenderer.size != size)
                this.spriteRenderer.size = size;

            var localScale = new Vector3(scale, scale, 1f);
            if (transform.localScale != localScale)
                transform.localScale = localScale;

            Vector3 cameraPosition = target.transform.position;
            var position = new Vector3(cameraPosition.x, cameraPosition.y, transform.position.z);
            if (transform.position != position)
                transform.position = position;
        }
    }
}

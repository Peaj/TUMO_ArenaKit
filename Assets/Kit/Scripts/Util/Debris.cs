using UnityEngine;

namespace ArenaKit
{
    /// <summary>Little visual extras: flying pieces and dropped weapons. They don't collide with anything.</summary>
    public static class Debris
    {
        /// <summary>Lets pieces of a sprite fly around, e.g. when a crate breaks.</summary>
        public static void Burst(SpriteRenderer source, int pieces)
        {
            if (source == null || source.sprite == null)
                return;

            for (int i = 0; i < pieces; i++)
            {
                var piece = new GameObject("Debris");
                piece.transform.position = source.transform.position + (Vector3)(Random.insideUnitCircle * 0.3f);
                piece.transform.localScale = source.transform.lossyScale * Random.Range(0.3f, 0.45f);

                var spriteRenderer = piece.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = source.sprite;
                spriteRenderer.color = source.color;
                spriteRenderer.sortingLayerID = source.sortingLayerID;
                spriteRenderer.sortingOrder = source.sortingOrder + 1;

                var body = piece.AddComponent<Rigidbody2D>();
                body.gravityScale = 2f;
                body.linearVelocity = new Vector2(Random.Range(-4f, 4f), Random.Range(3f, 8f));
                body.angularVelocity = Random.Range(-720f, 720f);

                piece.AddComponent<FadeAndDestroy>().Duration = 0.8f;
            }
        }

        /// <summary>Detaches an object (e.g. an empty weapon) and lets it fall and fade.</summary>
        public static void Drop(GameObject droppedObject, Vector2 velocity)
        {
            if (droppedObject == null)
                return;

            droppedObject.transform.SetParent(null, true);
            foreach (MonoBehaviour behaviour in droppedObject.GetComponentsInChildren<MonoBehaviour>())
                behaviour.enabled = false;
            foreach (Collider2D collider in droppedObject.GetComponentsInChildren<Collider2D>())
                collider.enabled = false;

            var body = droppedObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 2f;
            body.linearVelocity = velocity * 0.5f + new Vector2(Random.Range(-2f, 2f), 5f);
            body.angularVelocity = Random.Range(-540f, 540f);

            droppedObject.AddComponent<FadeAndDestroy>().Duration = 1.2f;
        }
    }
}

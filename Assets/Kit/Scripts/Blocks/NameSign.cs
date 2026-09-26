using UnityEngine;

namespace ArenaKit
{
    /// <summary>
    /// A sign with text on it. Without own text it shows the builder name from the level rules.
    /// </summary>
    [ExecuteAlways]
    public class NameSign : MonoBehaviour
    {
        [Tooltip("Text on the sign. Empty = your name from 'Level Rules > Built By'")]
        public string Text = "";
        public TextMesh Label;

        void OnEnable()
        {
            Refresh();
        }

        void Update()
        {
            if (!Application.isPlaying)
                Refresh();
        }

        public void Refresh()
        {
            if (Label == null)
                return;

            string text = Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                LevelRules rules = LevelRules.Current != null ? LevelRules.Current : FindAnyObjectByType<LevelRules>();
                text = rules != null && !string.IsNullOrWhiteSpace(rules.BuiltBy) ? rules.BuiltBy : "Dein Name";
            }

            if (Label.text != text)
                Label.text = text;

            // Long names get smaller so they fit on the sign.
            float scale = Mathf.Min(1f, 7f / Mathf.Max(1, text.Length));
            if (Label.transform.localScale.x != scale)
                Label.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}

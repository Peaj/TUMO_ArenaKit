using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace ArenaKit
{
    /// <summary>
    /// Scores, level title, round result and the help screen (key H).
    /// Built from code by the GameManager, so it works in every level without setup.
    /// </summary>
    public class ArenaHud : MonoBehaviour
    {
        public const string FontResource = "ArenaKit/ArenaFont";

        Font font;
        Text scoreText;
        Text timerText;
        Text titleText;
        Text subtitleText;
        Text bannerText;
        Text messageText;
        Text helpText;
        GameObject helpPanel;
        Coroutine titleRoutine;
        Coroutine messageRoutine;

        public static ArenaHud Create(Transform parent)
        {
            var root = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(parent, false);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var hud = root.AddComponent<ArenaHud>();
            hud.Build();
            return hud;
        }

        void Build()
        {
            this.font = Resources.Load<Font>(FontResource);
            if (this.font == null)
                this.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            this.scoreText = CreateText("Scores", 46, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(1600f, 80f));
            this.timerText = CreateText("Timer", 46, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-30f, -20f), new Vector2(300f, 80f));
            this.titleText = CreateText("Title", 96, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(1800f, 140f));
            this.subtitleText = CreateText("Subtitle", 52, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1800f, 80f));
            this.bannerText = CreateText("Banner", 110, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1900f, 160f));
            this.messageText = CreateText("Message", 40, TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1600f, 60f));

            Text hint = CreateText("Hint", 26, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(20f, 12f), new Vector2(400f, 40f));
            hint.text = "H = Hilfe";
            hint.color = new Color(1f, 1f, 1f, 0.6f);

            this.helpPanel = new GameObject("Help", typeof(RectTransform), typeof(Image));
            this.helpPanel.transform.SetParent(transform, false);
            var panelRect = (RectTransform)this.helpPanel.transform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            this.helpPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);
            this.helpText = CreateText("HelpText", 34, TextAnchor.UpperLeft, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1700f, 980f), this.helpPanel.transform);
            this.helpPanel.SetActive(false);

            this.titleText.text = this.subtitleText.text = this.bannerText.text = this.messageText.text = this.timerText.text = "";
        }

        Text CreateText(string objectName, int size, TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 dimensions, Transform parent = null)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text), typeof(Outline));
            textObject.transform.SetParent(parent != null ? parent : transform, false);

            var rect = (RectTransform)textObject.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;

            var text = textObject.GetComponent<Text>();
            text.font = this.font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            var outline = textObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(3f, -3f);
            return text;
        }

        void Update()
        {
            if (this.helpPanel.activeSelf)
                this.helpText.text = BuildHelpText();
        }

        // ---------------------------------------------------------------

        public void RefreshScores()
        {
            GameManager manager = GameManager.Instance;
            LevelRules rules = LevelRules.Current;
            if (manager == null)
                return;

            var numbers = new List<int>();
            if (rules != null)
                foreach (Character player in rules.Players)
                    if (player != null && !numbers.Contains(player.PlayerNumber))
                        numbers.Add(player.PlayerNumber);
            numbers.Sort();

            var builder = new StringBuilder();
            foreach (int number in numbers)
            {
                if (builder.Length > 0)
                    builder.Append("        ");
                builder.Append($"<color=#{GameManager.GetPlayerColorHex(number)}>P{number}: {manager.Wins[number]}</color>");
                if (rules != null && rules.PointsToWin > 1)
                    builder.Append($" <size=32>({rules.GetPoints(number)}/{rules.PointsToWin})</size>");
            }

            this.scoreText.text = builder.ToString();
        }

        public void SetTimer(float seconds)
        {
            this.timerText.text = seconds > 0f ? Mathf.CeilToInt(seconds).ToString() : "";
        }

        public void ShowTitle(string title, string builtBy, float duration)
        {
            if (this.titleRoutine != null)
                StopCoroutine(this.titleRoutine);
            this.titleRoutine = StartCoroutine(TitleRoutine(title, builtBy, duration));
            this.timerText.text = "";
        }

        IEnumerator TitleRoutine(string title, string builtBy, float duration)
        {
            this.titleText.text = string.IsNullOrWhiteSpace(title) ? "" : title;
            this.subtitleText.text = string.IsNullOrWhiteSpace(builtBy) ? "" : $"gebaut von {builtBy}";
            SetAlpha(this.titleText, 1f);
            SetAlpha(this.subtitleText, 1f);

            yield return new WaitForSeconds(duration);

            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                SetAlpha(this.titleText, 1f - t / 0.5f);
                SetAlpha(this.subtitleText, 1f - t / 0.5f);
                yield return null;
            }

            this.titleText.text = this.subtitleText.text = "";
            this.titleRoutine = null;
        }

        public void ShowRoundResult(int winner)
        {
            this.bannerText.text = winner > 0
                ? $"<color=#{GameManager.GetPlayerColorHex(winner)}>Spieler {winner} gewinnt!</color>"
                : "Unentschieden!";
        }

        public void HideRoundResult()
        {
            this.bannerText.text = "";
        }

        public void ShowMessage(string message, float duration = 2.5f)
        {
            if (this.messageRoutine != null)
                StopCoroutine(this.messageRoutine);
            this.messageRoutine = StartCoroutine(MessageRoutine(message, duration));
        }

        IEnumerator MessageRoutine(string message, float duration)
        {
            this.messageText.text = message;
            yield return new WaitForSeconds(duration);
            this.messageText.text = "";
            this.messageRoutine = null;
        }

        public void ToggleHelp()
        {
            this.helpPanel.SetActive(!this.helpPanel.activeSelf);
        }

        static void SetAlpha(Text text, float alpha)
        {
            Color color = text.color;
            text.color = new Color(color.r, color.g, color.b, alpha);
        }

        // ---------------------------------------------------------------

        static string BuildHelpText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("<size=52>Steuerung</size>");
            builder.AppendLine($"<color=#{GameManager.GetPlayerColorHex(1)}>Tastatur links</color>:   A / D laufen    W springen    linke Shift-Taste schießen    S + W durch Plattform fallen");
            builder.AppendLine($"<color=#{GameManager.GetPlayerColorHex(2)}>Tastatur rechts</color>:  Pfeile links / rechts laufen    Pfeil hoch springen    rechte alt-Taste schießen");
            builder.AppendLine("Gamepad:  Stick laufen    A springen    X oder RT schießen");
            builder.AppendLine();

            builder.AppendLine("<size=44>Spieler</size>");
            GameManager manager = GameManager.Instance;
            if (manager != null)
            {
                foreach (GameManager.PlayerSlot slot in manager.GetPlayerSlots())
                {
                    string device = slot.ControlScheme == "KeyboardLeft" ? "Tastatur links"
                        : slot.ControlScheme == "KeyboardRight" ? "Tastatur rechts"
                        : slot.Device != null ? slot.Device.displayName : "?";
                    builder.AppendLine($"<color=#{GameManager.GetPlayerColorHex(slot.Number)}>Spieler {slot.Number}</color>:  {device}");
                }
            }
            builder.AppendLine();

            builder.AppendLine("<size=44>Tasten-Test</size>  (alle Tasten gleichzeitig gedrückt halten, fehlt eine, schafft die Tastatur das nicht)");
            builder.AppendLine("Gedrückt:  " + PressedKeys());
            builder.AppendLine();

            builder.AppendLine("<size=44>Lehrer-Tasten</size>");
            builder.AppendLine("N  nächstes Level      R  Level neu starten      K  Anzahl Tastatur-Spieler (2 / 1 / 0)      0  Punkte zurücksetzen      H  Hilfe schließen");
            return builder.ToString();
        }

        static string PressedKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return "(keine Tastatur)";

            var names = new List<string>();
            foreach (KeyControl key in keyboard.allKeys)
                if (key != null && key.isPressed)
                    names.Add(key.displayName);

            foreach (Gamepad gamepad in Gamepad.all)
                foreach (InputControl control in gamepad.allControls)
                    if (control is ButtonControl button && button.isPressed && !button.synthetic)
                        names.Add($"[{gamepad.displayName}] {button.displayName}");

            return names.Count > 0 ? string.Join("   ", names) : "-";
        }
    }
}

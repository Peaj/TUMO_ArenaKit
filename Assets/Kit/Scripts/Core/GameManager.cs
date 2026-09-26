using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ArenaKit
{
    /// <summary>
    /// Lives for the whole game session (across all levels).
    /// Decides which players take part, counts the rounds everybody has won
    /// and loads the next level after each round.
    /// It is created automatically by <see cref="LevelRules"/>, you never have to place it yourself.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public const int MaxPlayers = 4;

        public static GameManager Instance { get; private set; }

        public static readonly Color[] PlayerColors =
        {
            new Color(0.95f, 0.30f, 0.25f), // 1 red
            new Color(0.25f, 0.60f, 1.00f), // 2 blue
            new Color(0.35f, 0.85f, 0.30f), // 3 green
            new Color(1.00f, 0.80f, 0.15f), // 4 yellow
        };

        [Tooltip("How many players share the keyboard (0-2). Every connected gamepad adds one more player.")]
        public int KeyboardPlayers = 2;

        [Tooltip("Seconds between the end of a round and the next level")]
        public float NextLevelDelay = 3f;

        /// <summary>Won rounds per player. Index = player number (1-4), index 0 is unused.</summary>
        public readonly int[] Wins = new int[MaxPlayers + 1];

        public ArenaHud Hud { get; private set; }

        static bool quitting;
        AudioSource audioSource;
        bool loading;

        public struct PlayerSlot
        {
            public int Number;
            public string ControlScheme;
            public InputDevice Device;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            quitting = false;
        }

        public static GameManager Ensure()
        {
            if (Instance != null || quitting)
                return Instance;

            var gameObject = new GameObject("Game Manager");
            return gameObject.AddComponent<GameManager>();
        }

        public static Color GetPlayerColor(int playerNumber)
        {
            return playerNumber >= 1 && playerNumber <= MaxPlayers ? PlayerColors[playerNumber - 1] : Color.white;
        }

        public static string GetPlayerColorHex(int playerNumber)
        {
            return ColorUtility.ToHtmlStringRGB(GetPlayerColor(playerNumber));
        }

        /// <summary>Plays a sound that is equally loud everywhere on screen.</summary>
        public static void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
                return;

            var manager = Ensure();
            if (manager != null)
                manager.audioSource.PlayOneShot(clip, volume);
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            this.audioSource = gameObject.AddComponent<AudioSource>();
            this.audioSource.playOnAwake = false;
            this.audioSource.spatialBlend = 0f;

            Hud = ArenaHud.Create(transform);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this)
                Instance = null;
        }

        void OnApplicationQuit()
        {
            quitting = true;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            this.loading = false;
            Hud.HideRoundResult();
        }

        void Update()
        {
            HandleTeacherKeys();
        }

        /// <summary>
        /// Which players play in the next level and with which device.
        /// The keyboard players come first (left half, right half), then one player per gamepad.
        /// </summary>
        public List<PlayerSlot> GetPlayerSlots()
        {
            var slots = new List<PlayerSlot>();
            Keyboard keyboard = Keyboard.current;
            int keyboardPlayers = keyboard != null ? Mathf.Clamp(KeyboardPlayers, 0, 2) : 0;

            if (keyboardPlayers >= 1)
                slots.Add(new PlayerSlot { Number = 1, ControlScheme = "KeyboardLeft", Device = keyboard });
            if (keyboardPlayers >= 2)
                slots.Add(new PlayerSlot { Number = 2, ControlScheme = "KeyboardRight", Device = keyboard });

            foreach (Gamepad gamepad in Gamepad.all)
            {
                if (slots.Count >= MaxPlayers)
                    break;
                slots.Add(new PlayerSlot { Number = slots.Count + 1, ControlScheme = "Gamepad", Device = gamepad });
            }

            // Nobody would be able to play: fall back to two keyboard players.
            if (slots.Count == 0 && keyboard != null)
            {
                slots.Add(new PlayerSlot { Number = 1, ControlScheme = "KeyboardLeft", Device = keyboard });
                slots.Add(new PlayerSlot { Number = 2, ControlScheme = "KeyboardRight", Device = keyboard });
            }

            return slots;
        }

        /// <summary>Called by LevelRules when a round is over. winner = 0 means draw.</summary>
        public void EndRound(int winner)
        {
            if (this.loading)
                return;

            if (winner >= 1 && winner <= MaxPlayers)
                Wins[winner]++;

            Hud.ShowRoundResult(winner);
            Hud.RefreshScores();
            StartCoroutine(LoadNextLevelDelayed(NextLevelDelay));
        }

        IEnumerator LoadNextLevelDelayed(float delay)
        {
            this.loading = true;
            yield return new WaitForSeconds(delay);
            LoadLevel(next: true);
        }

        public void LoadNextLevel()
        {
            LoadLevel(next: true);
        }

        public void RestartLevel()
        {
            LoadLevel(next: false);
        }

        public void ResetWins()
        {
            for (int i = 0; i < Wins.Length; i++)
                Wins[i] = 0;
            Hud.RefreshScores();
        }

        /// <summary>
        /// Loads the next scene of the build list (or the same scene again if it is the only one).
        /// Your own level scene is the only one in your build list, so it simply restarts.
        /// </summary>
        void LoadLevel(bool next)
        {
            StopAllCoroutines();
            this.loading = true;

            Scene active = SceneManager.GetActiveScene();
            int sceneCount = SceneManager.sceneCountInBuildSettings;
            int index = active.buildIndex >= 0 ? active.buildIndex : SceneUtility.GetBuildIndexByScenePath(active.path);

            if (index >= 0)
            {
                int nextIndex = next && sceneCount > 1 ? (index + 1) % sceneCount : index;
                SceneManager.LoadScene(nextIndex);
                return;
            }

            // The scene is not in the build list: just restart it.
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(active.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(active.name);
#endif
        }

        void HandleTeacherKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.hKey.wasPressedThisFrame)
                Hud.ToggleHelp();

            if (keyboard.nKey.wasPressedThisFrame)
                LoadNextLevel();

            if (keyboard.rKey.wasPressedThisFrame)
                RestartLevel();

            if (keyboard.digit0Key.wasPressedThisFrame)
            {
                ResetWins();
                Hud.ShowMessage("Punkte zurückgesetzt");
            }

            if (keyboard.kKey.wasPressedThisFrame)
            {
                // 2 -> 1 -> 0 -> 2
                KeyboardPlayers = (KeyboardPlayers + 2) % 3;
                Hud.ShowMessage($"Tastatur-Spieler: {KeyboardPlayers}  (Gamepads: {Gamepad.all.Count})");
                RestartLevel();
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace ArenaKit
{
    /// <summary>
    /// The rules of your level. Every level needs exactly one of these.
    /// It spawns the players, counts points and decides who wins the round.
    /// Other building blocks call AddPoint / WinRound on it, you can do that with events as well.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelRules : MonoBehaviour
    {
        public static LevelRules Current { get; private set; }

        [Header("Level")]
        [Tooltip("Shown at the start of the level")]
        public string LevelName = "Meine Arena";
        [Tooltip("Your name. Shown at the start of the level so everybody knows who built it")]
        public string BuiltBy = "";

        [Header("Winning")]
        [Tooltip("The round ends as soon as only one player is still alive. That player wins.")]
        public bool LastPlayerStandingWins = true;
        [Tooltip("Points a player needs to win the round. Points come from goals, score zones, collectibles or kills.")]
        [Min(1)] public int PointsToWin = 1;
        [Tooltip("Killing another player gives you one point")]
        public bool KillsGivePoints = false;
        [Tooltip("Seconds until the round ends. The player with the most points wins. 0 = no time limit")]
        [Min(0f)] public float TimeLimit = 0f;

        [Header("Players")]
        [Tooltip("Your own player prefab (variant) to change speed, jump, health... Leave empty for the standard player")]
        public Character PlayerPrefab;
        [Tooltip("Health of every player in this level. 0 = use the value of the player prefab")]
        [Min(0f)] public float PlayerHealth = 0f;
        [Tooltip("Dead players come back after a short time")]
        public bool RespawnPlayers = false;
        [Min(0f)] public float RespawnDelay = 2f;

        [Header("World")]
        [Tooltip("1 = normal, 0.3 = moon, 2 = heavy. Affects players, balls, grenades...")]
        public float Gravity = 1f;

        [Header("Events")]
        public UnityEvent OnRoundStart = new UnityEvent();
        [Tooltip("Called with the number of the player who scored")]
        public UnityEvent<int> OnPointScored = new UnityEvent<int>();
        [Tooltip("Called with the number of the winner (0 = draw)")]
        public UnityEvent<int> OnRoundOver = new UnityEvent<int>();

        public bool IsRoundOver { get; private set; }
        public bool IsRunning { get; private set; }
        public float TimeLeft { get; private set; }
        public IReadOnlyList<Character> Players => this.players;

        public const string DefaultPlayerResource = "ArenaKit/Player";
        const float StartFreezeTime = 1.2f;
        const float LastPlayerCheckDelay = 0.4f;
        static readonly Vector2 DefaultGravity = new Vector2(0f, -9.81f);

        readonly List<Character> players = new List<Character>();
        readonly Dictionary<int, GameManager.PlayerSlot> slots = new Dictionary<int, GameManager.PlayerSlot>();
        readonly int[] points = new int[GameManager.MaxPlayers + 1];
        SpawnPoint[] spawnPoints;
        bool checkingLastPlayer;

        void Awake()
        {
            if (Current != null && Current != this)
                Debug.LogWarning("Es gibt mehr als ein 'Level Rules' Objekt in dieser Szene. Bitte nur eines benutzen!", this);

            Current = this;
            Physics2D.gravity = DefaultGravity * Gravity;
            GameManager.Ensure();
            RemovePlacedPlayers();
            RebuildEmptyTileColliders();
        }

        /// <summary>Safety net: a tilemap with tiles but without collision shapes would let everybody fall through.</summary>
        static void RebuildEmptyTileColliders()
        {
            foreach (var tilemapCollider in FindObjectsByType<UnityEngine.Tilemaps.TilemapCollider2D>(FindObjectsSortMode.None))
            {
                var tilemap = tilemapCollider.GetComponent<UnityEngine.Tilemaps.Tilemap>();
                if (tilemap == null || tilemap.GetUsedTilesCount() == 0)
                    continue;

                var composite = tilemapCollider.GetComponent<CompositeCollider2D>();
                bool empty = composite != null && tilemapCollider.compositeOperation != Collider2D.CompositeOperation.None
                    ? composite.pathCount == 0
                    : tilemapCollider.shapeCount == 0;
                if (!empty)
                    continue;

                tilemapCollider.enabled = false;
                tilemapCollider.enabled = true;
                tilemapCollider.ProcessTilemapChanges();
                if (composite != null)
                    composite.GenerateGeometry();
            }
        }

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        IEnumerator Start()
        {
            this.spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            if (this.spawnPoints.Length == 0)
                Debug.LogWarning("Dein Level hat keine Startpunkte (Spawn Point). Die Spieler starten beim 'Level Rules' Objekt.", this);

            foreach (GameManager.PlayerSlot slot in GameManager.Instance.GetPlayerSlots())
            {
                this.slots[slot.Number] = slot;
                SpawnPlayer(slot.Number);
            }

            ArenaHud hud = GameManager.Instance.Hud;
            hud.ShowTitle(LevelName, BuiltBy, StartFreezeTime + 0.8f);
            hud.RefreshScores();

            SetPlayersFrozen(true);
            yield return new WaitForSeconds(StartFreezeTime);
            SetPlayersFrozen(false);

            TimeLeft = TimeLimit;
            IsRunning = true;
            OnRoundStart.Invoke();
        }

        void Update()
        {
            if (!IsRunning || IsRoundOver || TimeLimit <= 0f)
                return;

            TimeLeft -= Time.deltaTime;
            GameManager.Instance.Hud.SetTimer(TimeLeft);
            if (TimeLeft <= 0f)
            {
                TimeLeft = 0f;
                WinRound(GetLeader());
            }
        }

        // ---------------------------------------------------------------
        // Points and winning (these can be called from events)
        // ---------------------------------------------------------------

        /// <summary>Gives one point to a player (1-4).</summary>
        public void AddPoint(int player)
        {
            AddPoints(player, 1);
        }

        /// <summary>Takes one point away from a player (1-4).</summary>
        public void RemovePoint(int player)
        {
            AddPoints(player, -1);
        }

        public void AddPoints(int player, int amount)
        {
            if (IsRoundOver || player < 1 || player > GameManager.MaxPlayers || amount == 0)
                return;

            this.points[player] = Mathf.Max(0, this.points[player] + amount);
            if (amount > 0)
                OnPointScored.Invoke(player);

            GameManager.Instance.Hud.RefreshScores();

            if (this.points[player] >= PointsToWin)
                WinRound(player);
        }

        public int GetPoints(int player)
        {
            return player >= 1 && player <= GameManager.MaxPlayers ? this.points[player] : 0;
        }

        /// <summary>Ends the round, the player (1-4) wins. 0 = draw.</summary>
        public void WinRound(int player)
        {
            if (IsRoundOver)
                return;

            IsRoundOver = true;
            OnRoundOver.Invoke(player);
            GameManager.Instance.EndRound(player);
        }

        /// <summary>Ends the round without a winner.</summary>
        public void Draw()
        {
            WinRound(0);
        }

        /// <summary>The player with the most points, 0 if nobody leads.</summary>
        public int GetLeader()
        {
            int leader = 0;
            int best = 0;
            bool tie = false;
            for (int player = 1; player <= GameManager.MaxPlayers; player++)
            {
                if (this.points[player] > best)
                {
                    best = this.points[player];
                    leader = player;
                    tie = false;
                }
                else if (this.points[player] == best && best > 0)
                {
                    tie = true;
                }
            }

            if (tie || best == 0)
            {
                // Without points the last player standing leads.
                if (best == 0 && LastPlayerStandingWins)
                {
                    List<Character> alive = GetAlivePlayers();
                    if (alive.Count == 1)
                        return alive[0].PlayerNumber;
                }

                return 0;
            }

            return leader;
        }

        // ---------------------------------------------------------------
        // Players
        // ---------------------------------------------------------------

        public Character GetPlayer(int playerNumber)
        {
            foreach (Character player in this.players)
                if (player != null && player.PlayerNumber == playerNumber)
                    return player;
            return null;
        }

        public List<Character> GetAlivePlayers()
        {
            var alive = new List<Character>();
            foreach (Character player in this.players)
                if (player != null && !player.IsDead)
                    alive.Add(player);
            return alive;
        }

        /// <summary>Puts a player back to their spawn point (without killing them).</summary>
        public void SendToSpawn(Character character)
        {
            if (character == null)
                return;

            SpawnPoint spawn = FindSpawnPoint(character.PlayerNumber);
            character.Teleport(GetSpawnPosition(character.PlayerNumber, spawn));
        }

        /// <summary>Called by a player when they die.</summary>
        public void NotifyPlayerDied(Character victim, int killer)
        {
            if (IsRoundOver)
                return;

            if (KillsGivePoints && killer >= 1 && killer != victim.PlayerNumber)
                AddPoint(killer);

            if (IsRoundOver)
                return;

            if (RespawnPlayers)
            {
                StartCoroutine(RespawnAfterDelay(victim.PlayerNumber));
            }
            else if (!this.checkingLastPlayer)
            {
                StartCoroutine(CheckAlivePlayers());
            }
        }

        IEnumerator CheckAlivePlayers()
        {
            // Wait a moment: if both players die at the same time it is a draw.
            this.checkingLastPlayer = true;
            yield return new WaitForSeconds(LastPlayerCheckDelay);
            this.checkingLastPlayer = false;

            if (IsRoundOver)
                yield break;

            List<Character> alive = GetAlivePlayers();
            if (alive.Count == 0)
            {
                Draw();
            }
            else if (LastPlayerStandingWins && alive.Count == 1 && this.players.Count > 1)
            {
                WinRound(alive[0].PlayerNumber);
            }
        }

        IEnumerator RespawnAfterDelay(int playerNumber)
        {
            yield return new WaitForSeconds(RespawnDelay);
            if (!IsRoundOver)
                SpawnPlayer(playerNumber);
        }

        void SpawnPlayer(int playerNumber)
        {
            if (!this.slots.TryGetValue(playerNumber, out GameManager.PlayerSlot slot))
                return;

            Character prefab = PlayerPrefab != null ? PlayerPrefab : Resources.Load<Character>(DefaultPlayerResource);
            if (prefab == null)
            {
                Debug.LogError("Kein Spieler-Prefab gefunden!", this);
                return;
            }

            SpawnPoint spawn = FindSpawnPoint(playerNumber);
            Vector3 position = GetSpawnPosition(playerNumber, spawn);

            PlayerInput input = PlayerInput.Instantiate(prefab.gameObject, -1, slot.ControlScheme, -1, slot.Device);
            Character character = input.GetComponent<Character>();
            character.name = $"Spieler {playerNumber}";
            character.transform.position = new Vector3(position.x, position.y, prefab.transform.position.z);
            character.Body.position = position;
            character.Setup(playerNumber, PlayerHealth, this);

            if (spawn != null && spawn.StartWeapon != null)
                character.Equip(spawn.StartWeapon);

            ArenaCamera.Follow(character.transform);

            this.players.RemoveAll(p => p == null || p.PlayerNumber == playerNumber);
            this.players.Add(character);
            this.players.Sort((a, b) => a.PlayerNumber.CompareTo(b.PlayerNumber));
        }

        SpawnPoint FindSpawnPoint(int playerNumber)
        {
            if (this.spawnPoints == null || this.spawnPoints.Length == 0)
                return null;

            foreach (SpawnPoint spawn in this.spawnPoints)
                if (spawn != null && spawn.PlayerNumber == playerNumber)
                    return spawn;

            // Not enough spawn points: share them.
            var sorted = new List<SpawnPoint>(this.spawnPoints);
            sorted.RemoveAll(s => s == null);
            sorted.Sort((a, b) => a.PlayerNumber.CompareTo(b.PlayerNumber));
            return sorted.Count > 0 ? sorted[(playerNumber - 1) % sorted.Count] : null;
        }

        Vector3 GetSpawnPosition(int playerNumber, SpawnPoint spawn)
        {
            if (spawn == null)
                return transform.position + Vector3.right * (playerNumber - 2.5f) * 2f;

            Vector3 position = spawn.transform.position;
            if (spawn.PlayerNumber != playerNumber)
                position += Vector3.right * 0.7f * (playerNumber - spawn.PlayerNumber);
            return position;
        }

        void SetPlayersFrozen(bool frozen)
        {
            foreach (Character player in this.players)
                if (player != null)
                    player.InputEnabled = !frozen;
        }

        void RemovePlacedPlayers()
        {
            foreach (Character placed in FindObjectsByType<Character>(FindObjectsSortMode.None))
            {
                Debug.LogWarning("Spieler werden automatisch an den Startpunkten (Spawn Points) erzeugt. " +
                                 "Den Spieler, den du in die Szene gezogen hast, habe ich entfernt. " +
                                 "Eigenen Spieler benutzen? Trage ihn bei 'Level Rules > Player Prefab' ein.", placed);
                Destroy(placed.gameObject);
            }
        }
    }
}

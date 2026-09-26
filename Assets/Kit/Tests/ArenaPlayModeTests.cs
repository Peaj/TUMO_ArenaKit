using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ArenaKit.Tests
{
    /// <summary>
    /// Plays the template and demo levels with a simulated keyboard.
    /// Run them in Window > General > Test Runner > PlayMode.
    /// </summary>
    public class ArenaPlayModeTests : InputTestFixture
    {
        const string TemplateScene = "Assets/Kit/Scenes/Template.unity";
        const string ArenaScene = "Assets/Levels/Demo_Arena/Demo_Arena.unity";
        const string VolleyballScene = "Assets/Levels/Demo_RocketVolleyball/Demo_RocketVolleyball.unity";

        Keyboard keyboard;

        public override void Setup()
        {
            base.Setup();
            this.keyboard = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
            if (GameManager.Instance != null)
                Object.DestroyImmediate(GameManager.Instance.gameObject);
            base.TearDown();
        }

        // ---------------------------------------------------------------

        [UnityTest]
        public IEnumerator Template_SpawnsTwoUnarmedKeyboardPlayers()
        {
            yield return LoadLevel(TemplateScene);
            LevelRules rules = LevelRules.Current;

            Assert.AreEqual(2, rules.Players.Count);
            Assert.AreEqual(1, rules.Players[0].PlayerNumber);
            Assert.AreEqual(2, rules.Players[1].PlayerNumber);
            Assert.IsNull(rules.Players[0].CurrentWeapon, "Players start without a weapon");

            yield return new WaitForSeconds(0.5f);
            Capture("Template");
        }

        [UnityTest]
        public IEnumerator Template_LeftPlayerRunsWithD_RightPlayerStays()
        {
            yield return LoadLevel(TemplateScene);
            Character left = Player(1);
            Character right = Player(2);
            yield return new WaitForSeconds(0.3f);
            float leftStart = left.transform.position.x;
            float rightStart = right.transform.position.x;

            Press(this.keyboard.dKey);
            yield return new WaitForSeconds(0.6f);
            Release(this.keyboard.dKey);

            Assert.Greater(left.transform.position.x, leftStart + 1.5f, "Player 1 should run right");
            Assert.AreEqual(rightStart, right.transform.position.x, 0.2f, "Player 2 should not move");
            Assert.Greater(left.transform.localScale.x, 0f, "Player 1 faces right");
        }

        [UnityTest]
        public IEnumerator Template_RightPlayerRunsWithArrowKeys()
        {
            yield return LoadLevel(TemplateScene);
            Character left = Player(1);
            Character right = Player(2);
            yield return new WaitForSeconds(0.3f);
            float leftStart = left.transform.position.x;
            float rightStart = right.transform.position.x;

            Press(this.keyboard.leftArrowKey);
            yield return new WaitForSeconds(0.6f);
            Release(this.keyboard.leftArrowKey);

            Assert.Less(right.transform.position.x, rightStart - 1.5f, "Player 2 should run left");
            Assert.AreEqual(leftStart, left.transform.position.x, 0.2f, "Player 1 should not move");
            Assert.Less(right.transform.localScale.x, 0f, "Player 2 faces left");
        }

        [UnityTest]
        public IEnumerator Template_BothPlayersJump()
        {
            yield return LoadLevel(TemplateScene);
            Character left = Player(1);
            Character right = Player(2);
            yield return new WaitForSeconds(0.5f);
            float leftStart = left.transform.position.y;
            float rightStart = right.transform.position.y;

            Press(this.keyboard.wKey);
            Press(this.keyboard.upArrowKey);
            yield return new WaitForSeconds(0.1f);
            Release(this.keyboard.wKey);
            Release(this.keyboard.upArrowKey);
            yield return new WaitForSeconds(0.25f);

            Assert.Greater(left.transform.position.y, leftStart + 1.5f, "Player 1 should jump with W");
            Assert.Greater(right.transform.position.y, rightStart + 1.5f, "Player 2 should jump with the up arrow");
        }

        [UnityTest]
        public IEnumerator Template_PickupGivesWeapon()
        {
            yield return LoadLevel(TemplateScene);
            Character left = Player(1);
            WeaponPickup pickup = Object.FindAnyObjectByType<WeaponPickup>();
            Assert.IsNotNull(pickup);

            left.Teleport(pickup.transform.position);
            yield return new WaitForSeconds(0.3f);

            Assert.IsNotNull(left.CurrentWeapon, "Walking into a pickup gives a weapon");
            Assert.AreEqual(left.WeaponMount, left.CurrentWeapon.transform.parent, "The weapon sits in the hand");
        }

        [UnityTest]
        public IEnumerator Template_LeftShiftFiresBazooka_KillEndsRound()
        {
            yield return LoadLevel(TemplateScene);
            Character left = Player(1);
            Character right = Player(2);
            yield return new WaitForSeconds(0.5f);

            left.Equip(LoadWeapon("Bazooka"));
            left.Teleport(right.Body.position + Vector2.left * 5f);
            yield return new WaitForSeconds(0.4f);

            Press(this.keyboard.leftShiftKey);
            yield return null;
            Release(this.keyboard.leftShiftKey);
            Assert.IsNotNull(Object.FindAnyObjectByType<Projectile>(), "A rocket was fired");
            yield return new WaitForSeconds(0.1f);
            Capture("Template_Rocket");

            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(right.IsDead, "The rocket kills player 2");
            Assert.IsTrue(LevelRules.Current.IsRoundOver, "Last player standing wins");
            Assert.AreEqual(1, GameManager.Instance.Wins[1], "Player 1 got the round");
        }

        [UnityTest]
        public IEnumerator Template_EmptyWeaponDrops()
        {
            yield return LoadLevel(TemplateScene);
            Character left = Player(1);
            yield return new WaitForSeconds(0.3f);

            left.Equip(LoadWeapon("Pistol"));
            left.CurrentWeapon.Ammo = 1;
            yield return new WaitForSeconds(0.3f);

            Press(this.keyboard.leftShiftKey);
            yield return null;
            Release(this.keyboard.leftShiftKey);

            Assert.IsNull(left.CurrentWeapon, "The empty weapon is dropped");
        }

        [UnityTest]
        public IEnumerator Arena_JumpPadLaunchesPlayer()
        {
            yield return LoadLevel(ArenaScene);
            Character left = Player(1);
            JumpPad pad = Object.FindAnyObjectByType<JumpPad>();
            yield return new WaitForSeconds(0.3f);

            left.Teleport((Vector2)pad.transform.position + Vector2.up * 1.2f);
            left.Body.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.4f);

            Assert.Greater(left.transform.position.y, pad.transform.position.y + 3f, "The jump pad launches the player up");
            Capture("Arena");
        }

        [UnityTest]
        public IEnumerator Template_ExplosionBreaksCrateAndPushesPlayer()
        {
            yield return LoadLevel(TemplateScene);
            Character left = Player(1);
            yield return new WaitForSeconds(0.3f);

            Vector2 position = left.Body.position;
            Breakable crate = Object.Instantiate(LoadPrefab<Breakable>("Blocks/WeaponCrate"), position + new Vector2(2.6f, 0f), Quaternion.identity);
            int pickupsBefore = Object.FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None).Length;
            yield return new WaitForSeconds(0.2f);

            Explosion.Create(position + new Vector2(1.3f, 0f), 1.6f, 30f, 14f, 2);
            yield return null;

            Assert.Less(left.Body.linearVelocity.x, -2f, "The explosion pushes the player away");
            Assert.Less(left.CurrentHealth, left.Health, "The explosion hurts the player");
            Assert.IsTrue(crate == null, "The weapon crate breaks");
            Assert.AreEqual(pickupsBefore + 1, Object.FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None).Length, "The crate drops a weapon");
        }

        [UnityTest]
        public IEnumerator Arena_RoundOverLoadsNextLevel()
        {
#if UNITY_EDITOR
            // The test runner replaces the build list with its own scene, so set up the arcade order here.
            UnityEditor.EditorBuildSettingsScene[] previous = UnityEditor.EditorBuildSettings.scenes;
            UnityEditor.EditorBuildSettings.scenes = new[]
            {
                new UnityEditor.EditorBuildSettingsScene(ArenaScene, true),
                new UnityEditor.EditorBuildSettingsScene(VolleyballScene, true),
            };
#endif
            try
            {
                yield return LoadLevel(ArenaScene);
                Player(2).TakeDamage(1000f, 1);
                Assert.IsTrue(Player(2).IsDead);

                float timeout = Time.realtimeSinceStartup + 8f;
                while (SceneManager.GetActiveScene().name != "Demo_RocketVolleyball" && Time.realtimeSinceStartup < timeout)
                    yield return null;

                Assert.AreEqual("Demo_RocketVolleyball", SceneManager.GetActiveScene().name, "After the round the next level is loaded");
                Assert.AreEqual(1, GameManager.Instance.Wins[1], "The win is kept across levels");
            }
            finally
            {
#if UNITY_EDITOR
                UnityEditor.EditorBuildSettings.scenes = previous;
#endif
            }
        }

        [UnityTest]
        public IEnumerator Volleyball_BallInGoalGivesPoint_PlayersStartArmed()
        {
            yield return LoadLevel(VolleyballScene);
            LevelRules rules = LevelRules.Current;
            Assert.IsNotNull(Player(1).CurrentWeapon, "The spawn point gives the volley bazooka");
            Assert.AreEqual(-1, Player(1).CurrentWeapon.Ammo, "The volley bazooka never runs out");

            yield return new WaitForSeconds(1.3f);
            Capture("Volleyball");

            Ball ball = Object.FindAnyObjectByType<Ball>();
            var body = ball.GetComponent<Rigidbody2D>();
            Assert.AreEqual(RigidbodyType2D.Dynamic, body.bodyType, "The ball falls after the start delay");

            Goal rightGoal = null;
            foreach (Goal goal in Object.FindObjectsByType<Goal>(FindObjectsSortMode.None))
                if (goal.PointForPlayer == 1)
                    rightGoal = goal;

            body.position = rightGoal.transform.position;
            body.linearVelocity = Vector2.down;
            yield return new WaitForSeconds(0.3f);

            Assert.AreEqual(1, rules.GetPoints(1), "Ball on the right floor = point for player 1");
            Assert.IsFalse(rules.IsRoundOver, "Volleyball needs 5 points");
        }

        // ---------------------------------------------------------------

        static Character Player(int number)
        {
            Character player = LevelRules.Current.GetPlayer(number);
            Assert.IsNotNull(player, $"Player {number} exists");
            return player;
        }

        static IEnumerator LoadLevel(string path)
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#endif
            float timeout = Time.realtimeSinceStartup + 6f;
            while ((LevelRules.Current == null || !LevelRules.Current.IsRunning) && Time.realtimeSinceStartup < timeout)
                yield return null;

            Assert.IsNotNull(LevelRules.Current, "The level has Level Rules");
            Assert.IsTrue(LevelRules.Current.IsRunning, "The round started");
        }

        static Weapon LoadWeapon(string name)
        {
            return LoadPrefab<Weapon>("Weapons/" + name);
        }

        static T LoadPrefab<T>(string path) where T : Object
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>($"Assets/Kit/Prefabs/{path}.prefab");
#else
            return null;
#endif
        }

        /// <summary>Saves what the camera sees, if ARENA_PREVIEW_DIR is set.</summary>
        static void Capture(string name)
        {
            string folder = System.Environment.GetEnvironmentVariable("ARENA_PREVIEW_DIR");
            Camera camera = Camera.main;
            if (string.IsNullOrEmpty(folder) || camera == null)
                return;

            const int width = 1600;
            const int height = 900;
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            camera.targetTexture = null;
            RenderTexture.active = null;

            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG());
            Object.Destroy(texture);
            Object.Destroy(target);
        }
    }
}

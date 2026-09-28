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

            float leftTop = leftStart;
            float rightTop = rightStart;
            for (float end = Time.time + 0.6f; Time.time < end;)
            {
                leftTop = Mathf.Max(leftTop, left.transform.position.y);
                rightTop = Mathf.Max(rightTop, right.transform.position.y);
                yield return null;
            }

            Assert.Greater(leftTop, leftStart + 1f, "Player 1 should jump with W");
            Assert.Greater(rightTop, rightStart + 1f, "Player 2 should jump with the up arrow");
        }

        [UnityTest]
        public IEnumerator Template_PlayerStopsQuicklyWithoutInput()
        {
            yield return LoadLevel(TemplateScene);
            Character left = Player(1);
            yield return new WaitForSeconds(0.3f);

            Press(this.keyboard.dKey);
            yield return new WaitForSeconds(0.6f);
            Assert.Greater(left.Body.linearVelocity.x, 7f, "Player 1 runs");
            Release(this.keyboard.dKey);

            yield return new WaitForSeconds(0.3f);
            Assert.Less(Mathf.Abs(left.Body.linearVelocity.x), 0.3f, "Player 1 stops quickly when D is released");
        }

        [UnityTest]
        public IEnumerator Template_HoldingJumpJumpsHigher_FallingIsFaster()
        {
            yield return LoadLevel(TemplateScene);
            Character tapper = Player(1);
            Character holder = Player(2);
            yield return new WaitForSeconds(0.6f);
            float tapStart = tapper.transform.position.y;
            float holdStart = holder.transform.position.y;

            // Player 1 taps jump, player 2 holds it down.
            Press(this.keyboard.wKey);
            Press(this.keyboard.upArrowKey);
            float jumpTime = Time.time;
            yield return null;
            Release(this.keyboard.wKey);

            float tapTop = tapStart;
            float holdTop = holdStart;
            float holdTopTime = 0f;
            float holdLandTime = 0f;
            while (Time.time - jumpTime < 2f)
            {
                if (Time.time - jumpTime > 0.8f && this.keyboard.upArrowKey.isPressed)
                    Release(this.keyboard.upArrowKey);

                tapTop = Mathf.Max(tapTop, tapper.transform.position.y);
                float y = holder.transform.position.y;
                if (y > holdTop)
                {
                    holdTop = y;
                    holdTopTime = Time.time;
                }
                else if (holdLandTime == 0f && holdTopTime > 0f && y <= holdStart + 0.05f)
                {
                    holdLandTime = Time.time;
                }
                yield return null;
            }

            float tapHeight = tapTop - tapStart;
            float holdHeight = holdTop - holdStart;
            Assert.That(tapHeight, Is.InRange(tapper.MinJumpHeight - 0.4f, tapper.MinJumpHeight + 0.6f), "A tap makes a short hop");
            Assert.That(holdHeight, Is.InRange(holder.JumpHeight - 0.5f, holder.JumpHeight + 0.3f), "Holding jump reaches the full jump height");

            float rise = holdTopTime - jumpTime;
            float fall = holdLandTime - holdTopTime;
            Assert.Greater(holdLandTime, 0f, "Player 2 lands again");
            Assert.Less(fall, rise * 0.85f, "Falling down is faster than going up");
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
        public IEnumerator Template_RocketFlyingLeftOnlyTurnsAroundZ()
        {
            yield return LoadLevel(TemplateScene);
            Character right = Player(2);
            yield return new WaitForSeconds(0.3f);

            // Face left, then fire.
            Press(this.keyboard.leftArrowKey);
            yield return new WaitForSeconds(0.1f);
            Release(this.keyboard.leftArrowKey);
            right.Equip(LoadWeapon("Bazooka"));
            yield return new WaitForSeconds(0.4f);

            Press(this.keyboard.rightAltKey);
            yield return null;
            Release(this.keyboard.rightAltKey);
            yield return new WaitForSeconds(0.05f);

            Projectile rocket = Object.FindAnyObjectByType<Projectile>();
            Assert.IsNotNull(rocket, "A rocket was fired");
            Assert.Less(rocket.GetComponent<Rigidbody2D>().linearVelocity.x, 0f, "The rocket flies left");
            Vector3 euler = rocket.transform.eulerAngles;
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, euler.x), 0.5f, "No rotation around x");
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, euler.y), 0.5f, "No rotation around y");
            Assert.AreEqual(180f, Mathf.Abs(Mathf.DeltaAngle(0f, euler.z)), 1f, "Turned 180 degrees around z");
            Assert.AreEqual(2f, rocket.transform.localScale.x, 0.01f, "Rocket has its original size");
            Capture("Rocket_Left");
        }

        [UnityTest]
        public IEnumerator Template_GrenadeLauncherShootsWhereTheMuzzlePoints([Values(false, true)] bool facingLeft)
        {
            yield return LoadLevel(TemplateScene);
            Character left = Player(1);
            yield return new WaitForSeconds(0.3f);

            if (facingLeft)
            {
                Press(this.keyboard.aKey);
                yield return new WaitForSeconds(0.1f);
                Release(this.keyboard.aKey);
            }

            left.Equip(LoadWeapon("GrenadeLauncher"));
            yield return new WaitForSeconds(0.4f);
            Weapon weapon = left.CurrentWeapon;
            Vector2 aim = weapon.AimDirection();
            float expectedAngle = facingLeft ? 155f : 25f;
            Assert.AreEqual(expectedAngle, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg, 1f, "The muzzle points 25 degrees up");

            Press(this.keyboard.leftShiftKey);
            yield return null;
            Release(this.keyboard.leftShiftKey);
            yield return new WaitForFixedUpdate();

            Vector2 velocity = Object.FindAnyObjectByType<Projectile>().GetComponent<Rigidbody2D>().linearVelocity;
            Assert.AreEqual(expectedAngle, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg, 3f, "The grenade flies where the muzzle points");
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

        [UnityTest]
        public IEnumerator Camera_FollowsLivingPlayers()
        {
            yield return LoadLevel(TemplateScene);
            ArenaCamera arenaCamera = ArenaCamera.Current;
            Assert.IsNotNull(arenaCamera, "The level has a camera rig");
            Character left = Player(1);
            Character right = Player(2);

            Assert.GreaterOrEqual(arenaCamera.TargetGroup.FindMember(left.transform), 0, "Player 1 is a camera target");
            Assert.GreaterOrEqual(arenaCamera.TargetGroup.FindMember(right.transform), 0, "Player 2 is a camera target");
            Assert.Less(arenaCamera.TargetGroup.FindMember(arenaCamera.LevelCenter), 0, "The level overview is off while players live");

            yield return new WaitForSeconds(2f);
            AssertOnScreen(left, "Player 1");
            AssertOnScreen(right, "Player 2");
            float bothSize = Camera.main.orthographicSize;

            right.TakeDamage(1000f, 1);
            Assert.Less(arenaCamera.TargetGroup.FindMember(right.transform), 0, "Dead players are no camera targets");

            yield return new WaitForSeconds(2.5f);
            AssertOnScreen(left, "Player 1");
            Assert.Less(Camera.main.orthographicSize, bothSize - 0.5f, "The camera zooms in on the winner");
        }

        [UnityTest]
        public IEnumerator Camera_StaysInsideLevelOnEveryScreenShape()
        {
            yield return LoadLevel(ArenaScene);
            ArenaCamera arenaCamera = ArenaCamera.Current;
            Camera camera = Camera.main;
            Character left = Player(1);
            Character right = Player(2);

            // Far apart: the camera has to zoom out as far as the level allows.
            left.Teleport(new Vector2(-13f, 6f));
            right.Teleport(new Vector2(13f, -6f));

            var shapes = new[] { (16f / 9f, "16x9"), (16f / 10f, "16x10"), (4f / 3f, "4x3"), (21f / 9f, "21x9") };
            try
            {
                foreach ((float aspect, string name) in shapes)
                {
                    camera.aspect = aspect;
                    yield return new WaitForSeconds(2.5f);

                    Rect level = LevelRect(arenaCamera);
                    float height = camera.orthographicSize * 2f;
                    var view = new Rect((Vector2)camera.transform.position - new Vector2(height * aspect, height) * 0.5f, new Vector2(height * aspect, height));
                    AssertInsideOrCentered(view.xMin, view.xMax, level.xMin, level.xMax, name + " horizontally");
                    AssertInsideOrCentered(view.yMin, view.yMax, level.yMin, level.yMax, name + " vertically");
                    AssertOnScreen(left, name + ": player 1");
                    AssertOnScreen(right, name + ": player 2");
                    Capture("Camera_" + name, aspect);
                }
            }
            finally
            {
                camera.ResetAspect();
            }
        }

        [UnityTest]
        public IEnumerator Levels_AllSolidTilesHaveCollision([Values(TemplateScene, ArenaScene, VolleyballScene)] string scene)
        {
            yield return LoadLevel(scene);
            yield return new WaitForFixedUpdate();
            Physics2D.SyncTransforms();

            int checkedTiles = 0;
            foreach (var tilemapCollider in Object.FindObjectsByType<UnityEngine.Tilemaps.TilemapCollider2D>(FindObjectsSortMode.None))
            {
                if (tilemapCollider.isTrigger)
                    continue;

                var tilemap = tilemapCollider.GetComponent<UnityEngine.Tilemaps.Tilemap>();
                var composite = tilemapCollider.GetComponent<CompositeCollider2D>();
                Collider2D solid = composite != null ? composite : tilemapCollider;
                foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
                {
                    if (!tilemap.HasTile(cell) || tilemap.GetColliderType(cell) != UnityEngine.Tilemaps.Tile.ColliderType.Grid)
                        continue;
                    checkedTiles++;
                    Vector3 center = tilemap.GetCellCenterWorld(cell);
                    Assert.IsTrue(solid.OverlapPoint(center), $"{tilemap.name} tile at {cell} has collision");
                }
            }

            Assert.Greater(checkedTiles, 20, "The level has solid tiles");
        }

        // ---------------------------------------------------------------

        static void AssertOnScreen(Character character, string label)
        {
            Vector3 viewport = Camera.main.WorldToViewportPoint(character.transform.position);
            Assert.That(viewport.x, Is.InRange(0.02f, 0.98f), label + " is on screen (x)");
            Assert.That(viewport.y, Is.InRange(0.02f, 0.98f), label + " is on screen (y)");
        }

        /// <summary>The view stays inside the level, or (if the level is smaller than the view) the level sits in its middle.</summary>
        static void AssertInsideOrCentered(float viewMin, float viewMax, float levelMin, float levelMax, string label)
        {
            const float tolerance = 0.1f;
            if (viewMax - viewMin <= levelMax - levelMin + tolerance)
            {
                Assert.GreaterOrEqual(viewMin, levelMin - tolerance, label + ": nothing outside the level");
                Assert.LessOrEqual(viewMax, levelMax + tolerance, label + ": nothing outside the level");
            }
            else
            {
                Assert.AreEqual((levelMin + levelMax) * 0.5f, (viewMin + viewMax) * 0.5f, 0.5f, label + ": level centered");
            }
        }

        static Rect LevelRect(ArenaCamera arenaCamera)
        {
            BoxCollider2D box = arenaCamera.Bounds;
            Vector2 center = box.transform.TransformPoint(box.offset);
            return new Rect(center - box.size * 0.5f, box.size);
        }

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
        static void Capture(string name, float aspect = 16f / 9f)
        {
            string folder = System.Environment.GetEnvironmentVariable("ARENA_PREVIEW_DIR");
            Camera camera = Camera.main;
            if (string.IsNullOrEmpty(folder) || camera == null)
                return;

            const int height = 900;
            int width = Mathf.RoundToInt(height * aspect);
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace ArenaKit.EditorTools
{
    /// <summary>
    /// Builds all kit assets (tiles, palettes, prefabs, template and demo scenes) from the art.
    /// Run it again after changing this file: TUMO > Lehrer > Kit neu bauen.
    /// From the command line: Unity -batchmode -executeMethod ArenaKit.EditorTools.KitBuilder.BuildAll -quit
    /// </summary>
    public static class KitBuilder
    {
        const string Kit = "Assets/Kit";
        const string Art = Kit + "/Art";
        const string KenneyTiles = Art + "/Kenney/Tiles";
        const string KenneyBackgrounds = Art + "/Kenney/Backgrounds";
        const string KenneySounds = Art + "/Kenney/Sounds";
        const string Platformer = Art + "/Platformer";
        const string Generated = Art + "/Generated";
        const string Physics = Kit + "/Physics";
        const string Prefabs = Kit + "/Prefabs";
        const string PlayerFolder = Kit + "/Resources/ArenaKit";
        const string Tiles = Kit + "/Tiles";
        const string Palettes = Kit + "/Palettes";
        const string Scenes = Kit + "/Scenes";
        const string Levels = "Assets/Levels";
        const string HeroSource = Platformer + "/Prefabs/Characters/hero.prefab";
        const string RocketSource = Platformer + "/Prefabs/Props/rocket.prefab";
        const string ExplosionSource = Platformer + "/Prefabs/Props/rocketExplosion.prefab";
        const string ImportMarker = "ArenaKitImport2";

        // Sorting orders (all on the Default sorting layer).
        const int OrderBackground = -100;
        const int OrderBackDecoration = -20;
        const int OrderGround = 0;
        const int OrderProps = 5;
        const int OrderPickups = 6;
        const int OrderPlayer = 10;
        const int OrderProjectiles = 12;
        const int OrderFrontDecoration = 20;
        const int OrderEffects = 30;

        static readonly string[] TerrainMaterials = { "grass", "stone", "dirt", "sand", "snow", "purple" };

        static readonly Vector3Int Up = new Vector3Int(0, 1, 0);
        static readonly Vector3Int Down = new Vector3Int(0, -1, 0);
        static readonly Vector3Int Left = new Vector3Int(-1, 0, 0);
        static readonly Vector3Int Right = new Vector3Int(1, 0, 0);

        // Filled while building.
        static readonly Dictionary<string, TileBase> TileLookup = new Dictionary<string, TileBase>();
        static readonly Dictionary<string, GameObject> PrefabLookup = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, Weapon> WeaponLookup = new Dictionary<string, Weapon>();

        [MenuItem("TUMO/Lehrer/Kit neu bauen", priority = 200)]
        static void BuildFromMenu()
        {
            if (EditorUtility.DisplayDialog("Kit neu bauen",
                    "Baut alle Kit-Prefabs, Tiles, Paletten, die Vorlage und die Demo-Level neu.\nEigene Level in Assets/Levels bleiben unverändert.",
                    "Neu bauen", "Abbrechen"))
                BuildAll();
        }

        public static void BuildAll()
        {
            TileLookup.Clear();
            PrefabLookup.Clear();
            WeaponLookup.Clear();

            EnsureFolders();
            ConfigureProject();
            GenerateTextures();
            ConfigureTextureImport();
            BuildPhysicsMaterials();

            BuildTiles();
            BuildPalettes();

            BuildEffectsAndProjectiles();
            BuildWeapons();
            BuildPlayer();
            BuildPickups();
            BuildBlocks();

            BuildTemplateScene();
            BuildDemoArena();
            BuildDemoVolleyball();

            ArenaMenu.SetBuildScenes(new[]
            {
                $"{Levels}/Demo_Arena/Demo_Arena.unity",
                $"{Levels}/Demo_RocketVolleyball/Demo_RocketVolleyball.unity",
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            KitManifest.Write();
            Debug.Log("[ArenaKit] Kit built.");
        }

        // ---------------------------------------------------------------
        // Project setup
        // ---------------------------------------------------------------

        static void EnsureFolders()
        {
            foreach (string folder in new[]
                     {
                         Generated, Physics, Prefabs, Prefabs + "/Weapons", Prefabs + "/Projectiles", Prefabs + "/Effects",
                         Prefabs + "/Pickups", Prefabs + "/Blocks", Prefabs + "/Rules", PlayerFolder, Tiles, Tiles + "/Ground",
                         Tiles + "/Platforms", Tiles + "/Hazards", Tiles + "/Decoration", Palettes, Scenes, Levels,
                         Levels + "/Demo_Arena", Levels + "/Demo_RocketVolleyball",
                     })
                CreateFolder(folder);
        }

        static void CreateFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            CreateFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void ConfigureProject()
        {
            // Explosions and fast rockets should not tunnel through thin walls.
            Physics2D.queriesHitTriggers = true;
            Physics2D.callbacksOnDisable = true;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
        }

        static int GroundLayer => LayerMask.NameToLayer("Ground") >= 0 ? LayerMask.NameToLayer("Ground") : 0;
        static int CharacterLayer => LayerMask.NameToLayer("Character") >= 0 ? LayerMask.NameToLayer("Character") : 0;

        // ---------------------------------------------------------------
        // Textures
        // ---------------------------------------------------------------

        static void GenerateTextures()
        {
            WriteTexture(Generated + "/Square.png", 32, (x, y) => Color.white);
            WriteTexture(Generated + "/Dot.png", 32, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f));
                return new Color(1f, 1f, 1f, Mathf.Clamp01(15f - d));
            });
            WriteTexture(Generated + "/Ball.png", 128, BallPixel);
            WriteTexture(Generated + "/Marker.png", 64, MarkerPixel);
        }

        /// <summary>A small arrow pointing down, shown above the player in the player color.</summary>
        static Color MarkerPixel(int x, int y)
        {
            var p = new Vector2(x + 0.5f, y + 0.5f);
            Vector2 a = new Vector2(6f, 54f), b = new Vector2(58f, 54f), c = new Vector2(32f, 12f);
            float inside = Mathf.Min(EdgeDistance(p, a, b), Mathf.Min(EdgeDistance(p, b, c), EdgeDistance(p, c, a)));
            if (inside < -0.5f)
                return Color.clear;
            float alpha = Mathf.Clamp01(inside + 0.5f);
            return inside < 5f ? new Color(0.15f, 0.15f, 0.2f, alpha) : Color.white;
        }

        /// <summary>Signed distance of p to the edge a-b of a clockwise triangle (positive = inside).</summary>
        static float EdgeDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 edge = (b - a).normalized;
            Vector2 normal = new Vector2(edge.y, -edge.x);
            return Vector2.Dot(p - a, normal);
        }

        static Color BallPixel(int x, int y)
        {
            var center = new Vector2(64f, 64f);
            var p = new Vector2(x + 0.5f, y + 0.5f);
            float radius = 60f;
            float d = Vector2.Distance(p, center);
            float alpha = Mathf.Clamp01(radius - d + 0.5f);
            if (alpha <= 0f)
                return Color.clear;

            var outline = new Color(0.22f, 0.22f, 0.3f, alpha);
            if (d > radius - 7f)
                return outline;

            // Three curved seams like a volleyball.
            float seamA = Mathf.Abs(Vector2.Distance(p, center + new Vector2(70f, 30f)) - 62f);
            float seamB = Mathf.Abs(Vector2.Distance(p, center + new Vector2(-70f, 30f)) - 62f);
            float seamC = Mathf.Abs(Vector2.Distance(p, center + new Vector2(0f, -80f)) - 62f);
            if (Mathf.Min(seamA, Mathf.Min(seamB, seamC)) < 2.5f)
                return outline;

            bool yellow = seamA > 0f && Vector2.Distance(p, center + new Vector2(70f, 30f)) < 62f;
            Color fill = yellow ? new Color(1f, 0.85f, 0.3f) : new Color(0.98f, 0.98f, 1f);
            float shade = Mathf.Lerp(1f, 0.82f, Mathf.Clamp01((p.x - p.y) / 160f + 0.3f));
            return new Color(fill.r * shade, fill.g * shade, fill.b * shade, 1f);
        }

        static void WriteTexture(string path, int size, System.Func<int, int, Color> pixel)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    texture.SetPixel(x, y, pixel(x, y));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        static void ConfigureTextureImport()
        {
            var paths = new List<string>();
            foreach (string folder in new[] { KenneyTiles, KenneyBackgrounds, Generated })
                paths.AddRange(AssetDatabase.FindAssets("t:Texture2D", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath));

            foreach (string path in paths)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null || importer.userData == ImportMarker)
                    continue;

                bool background = path.StartsWith(KenneyBackgrounds);
                bool generated = path.StartsWith(Generated);
                int pixelsPerUnit = generated ? (path.EndsWith("Square.png") ? 32 : 128) : 64;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = background ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteGenerateFallbackPhysicsShape = true;
                importer.SetTextureSettings(settings);

                importer.userData = ImportMarker;
                importer.SaveAndReimport();
            }
        }

        static Sprite Kenney(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{KenneyTiles}/{name}.png");
            if (sprite == null)
                Debug.LogWarning("[ArenaKit] Missing Kenney sprite " + name);
            return sprite;
        }

        static Sprite GeneratedSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{Generated}/{name}.png");
        }

        static Sprite PlatformerSprite(string file)
        {
            return AssetDatabase.LoadAllAssetsAtPath($"{Platformer}/Sprites/{file}").OfType<Sprite>().FirstOrDefault();
        }

        static AudioClip Sound(string path)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static AudioClip KenneySound(string name)
        {
            return Sound($"{KenneySounds}/{name}.ogg");
        }

        static AudioClip PlatformerSound(string path)
        {
            return Sound($"{Platformer}/Audio/{path}");
        }

        // ---------------------------------------------------------------
        // Physics materials
        // ---------------------------------------------------------------

        static void BuildPhysicsMaterials()
        {
            SaveAsset(new PhysicsMaterial2D("Bouncy") { bounciness = 0.75f, friction = 0.2f }, Physics + "/Bouncy.physicsMaterial2D");
            SaveAsset(new PhysicsMaterial2D("Grenade") { bounciness = 0.45f, friction = 0.5f }, Physics + "/Grenade.physicsMaterial2D");
            SaveAsset(new PhysicsMaterial2D("Crate") { bounciness = 0.05f, friction = 0.6f }, Physics + "/Crate.physicsMaterial2D");
        }

        static PhysicsMaterial2D LoadPhysics(string name)
        {
            return AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>($"{Physics}/{name}.physicsMaterial2D");
        }

        /// <summary>Creates the asset, or overwrites an existing one while keeping its GUID.</summary>
        static T SaveAsset<T>(T asset, string path) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                existing.name = Path.GetFileNameWithoutExtension(path);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(asset);
                return existing;
            }

            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // ---------------------------------------------------------------
        // Tiles
        // ---------------------------------------------------------------

        static void BuildTiles()
        {
            foreach (string material in TerrainMaterials)
            {
                string title = Capitalize(material);
                Register(title, SaveAsset(TerrainRuleTile(material), $"{Tiles}/Ground/{title}.asset"));
                Register(title + "Platform", SaveAsset(CloudRuleTile(material), $"{Tiles}/Platforms/{title}Platform.asset"));

                foreach (string ramp in new[] { "ramp_long_a", "ramp_long_b", "ramp_long_c", "ramp_short_a", "ramp_short_b" })
                    Register($"{title}_{ramp}", SaveAsset(SimpleTile($"terrain_{material}_{ramp}", Tile.ColliderType.Sprite), $"{Tiles}/Ground/{title}_{Capitalize(ramp)}.asset"));

                Register("BackWall" + title, SaveAsset(SimpleTile($"terrain_{material}_block_center", Tile.ColliderType.None, new Color(0.55f, 0.55f, 0.62f)), $"{Tiles}/Decoration/BackWall{title}.asset"));
            }

            foreach (string block in new[] { "bricks_brown", "bricks_grey", "brick_brown", "brick_grey", "block_blue", "block_green", "block_red", "block_yellow", "block_plank", "block_planks", "block_empty", "block_strong_empty", "block_coin", "conveyor" })
                Register(block, SaveAsset(SimpleTile(block, Tile.ColliderType.Grid), $"{Tiles}/Ground/{Capitalize(block)}.asset"));

            Register("bridge", SaveAsset(SimpleTile("bridge", Tile.ColliderType.Sprite), $"{Tiles}/Platforms/Bridge.asset"));
            Register("bridge_logs", SaveAsset(SimpleTile("bridge_logs", Tile.ColliderType.Sprite), $"{Tiles}/Platforms/BridgeLogs.asset"));

            Register("Lava", SaveAsset(SurfaceRuleTile("lava", "lava_top", Tile.ColliderType.Grid), $"{Tiles}/Hazards/Lava.asset"));
            Register("spikes", SaveAsset(SimpleTile("spikes", Tile.ColliderType.Sprite), $"{Tiles}/Hazards/Spikes.asset"));

            Register("Water", SaveAsset(SurfaceRuleTile("water", "water_top", Tile.ColliderType.None), $"{Tiles}/Decoration/Water.asset"));
            foreach (string decoration in new[]
                     {
                         "bush", "cactus", "grass", "grass_purple", "hill", "hill_top", "hill_top_smile", "mushroom_brown", "mushroom_red", "rock",
                         "fence", "fence_broken", "sign", "sign_exit", "sign_left", "sign_right", "window", "chain", "ladder_top", "ladder_middle",
                         "ladder_bottom", "rope", "rop_attached", "weight", "torch_off", "heart", "star",
                     })
                Register(decoration, SaveAsset(SimpleTile(decoration, Tile.ColliderType.None), $"{Tiles}/Decoration/{Capitalize(decoration)}.asset"));

            Register("Torch", SaveAsset(AnimatedTile(new[] { "torch_on_a", "torch_on_b" }, 4f), $"{Tiles}/Decoration/Torch.asset"));
            foreach (string color in new[] { "red", "blue", "green", "yellow" })
                Register("Flag" + Capitalize(color), SaveAsset(AnimatedTile(new[] { $"flag_{color}_a", $"flag_{color}_b" }, 3f), $"{Tiles}/Decoration/Flag{Capitalize(color)}.asset"));
        }

        static void Register(string key, TileBase tile)
        {
            TileLookup[key] = tile;
        }

        static TileBase TileByKey(string key)
        {
            return TileLookup.TryGetValue(key, out TileBase tile) ? tile : null;
        }

        static Tile SimpleTile(string sprite, Tile.ColliderType collider, Color? color = null)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Kenney(sprite);
            tile.colliderType = collider;
            tile.color = color ?? Color.white;
            return tile;
        }

        static AnimatedTile AnimatedTile(string[] sprites, float speed)
        {
            var tile = ScriptableObject.CreateInstance<AnimatedTile>();
            tile.m_AnimatedSprites = sprites.Select(Kenney).ToArray();
            tile.m_MinSpeed = speed;
            tile.m_MaxSpeed = speed;
            tile.m_TileColliderType = Tile.ColliderType.None;
            return tile;
        }

        /// <summary>A terrain tile that picks the right edge and corner sprite automatically.</summary>
        static RuleTile TerrainRuleTile(string material)
        {
            var tile = ScriptableObject.CreateInstance<RuleTile>();
            tile.m_DefaultSprite = Kenney($"terrain_{material}_block");
            tile.m_DefaultColliderType = Tile.ColliderType.Grid;

            // up, down, left, right -> sprite
            var rules = new (bool up, bool down, bool left, bool right, string sprite)[]
            {
                (false, false, false, false, "block"),
                (false, false, false, true, "horizontal_left"),
                (false, false, true, true, "horizontal_middle"),
                (false, false, true, false, "horizontal_right"),
                (false, true, false, false, "vertical_top"),
                (true, true, false, false, "vertical_middle"),
                (true, false, false, false, "vertical_bottom"),
                (false, true, false, true, "block_top_left"),
                (false, true, true, true, "block_top"),
                (false, true, true, false, "block_top_right"),
                (true, true, false, true, "block_left"),
                (true, true, true, true, "block_center"),
                (true, true, true, false, "block_right"),
                (true, false, false, true, "block_bottom_left"),
                (true, false, true, true, "block_bottom"),
                (true, false, true, false, "block_bottom_right"),
            };

            foreach (var rule in rules)
                AddRule(tile, Kenney($"terrain_{material}_{rule.sprite}"), Tile.ColliderType.Grid,
                    (Up, rule.up), (Down, rule.down), (Left, rule.left), (Right, rule.right));
            return tile;
        }

        /// <summary>A one-way platform tile with rounded ends.</summary>
        static RuleTile CloudRuleTile(string material)
        {
            var tile = ScriptableObject.CreateInstance<RuleTile>();
            tile.m_DefaultSprite = Kenney($"terrain_{material}_cloud");
            tile.m_DefaultColliderType = Tile.ColliderType.Grid;
            AddRule(tile, Kenney($"terrain_{material}_cloud"), Tile.ColliderType.Grid, (Left, false), (Right, false));
            AddRule(tile, Kenney($"terrain_{material}_cloud_left"), Tile.ColliderType.Grid, (Left, false), (Right, true));
            AddRule(tile, Kenney($"terrain_{material}_cloud_middle"), Tile.ColliderType.Grid, (Left, true), (Right, true));
            AddRule(tile, Kenney($"terrain_{material}_cloud_right"), Tile.ColliderType.Grid, (Left, true), (Right, false));
            return tile;
        }

        /// <summary>Water and lava: wavy top when nothing is above.</summary>
        static RuleTile SurfaceRuleTile(string inside, string top, Tile.ColliderType collider)
        {
            var tile = ScriptableObject.CreateInstance<RuleTile>();
            tile.m_DefaultSprite = Kenney(top);
            tile.m_DefaultColliderType = collider;
            AddRule(tile, Kenney(inside), collider, (Up, true));
            AddRule(tile, Kenney(top), collider, (Up, false));
            return tile;
        }

        static void AddRule(RuleTile tile, Sprite sprite, Tile.ColliderType collider, params (Vector3Int position, bool same)[] neighbors)
        {
            var rule = new RuleTile.TilingRule
            {
                m_NeighborPositions = neighbors.Select(n => n.position).ToList(),
                m_Neighbors = neighbors.Select(n => n.same ? RuleTile.TilingRuleOutput.Neighbor.This : RuleTile.TilingRuleOutput.Neighbor.NotThis).ToList(),
                m_Sprites = new[] { sprite },
                m_ColliderType = collider,
            };
            tile.m_TilingRules.Add(rule);
        }

        // ---------------------------------------------------------------
        // Palettes
        // ---------------------------------------------------------------

        static void BuildPalettes()
        {
            var ground = new List<List<string>>
            {
                TerrainMaterials.Select(Capitalize).ToList(),
                new List<string> { "bricks_brown", "bricks_grey", "brick_brown", "brick_grey", "block_planks", "block_plank", "block_empty" },
                new List<string> { "block_blue", "block_green", "block_red", "block_yellow", "block_strong_empty", "block_coin", "conveyor" },
            };
            foreach (string material in TerrainMaterials)
                ground.Add(new[] { "ramp_long_a", "ramp_long_b", "ramp_long_c", "ramp_short_a", "ramp_short_b" }.Select(r => $"{Capitalize(material)}_{r}").ToList());
            BuildPalette("1 Ground", ground);

            BuildPalette("2 Platforms", new List<List<string>>
            {
                TerrainMaterials.Select(m => Capitalize(m) + "Platform").ToList(),
                new List<string> { "bridge", "bridge_logs" },
            });

            BuildPalette("3 Hazards", new List<List<string>> { new List<string> { "Lava", "spikes" } });

            BuildPalette("4 Decoration", new List<List<string>>
            {
                new List<string> { "bush", "cactus", "grass", "grass_purple", "rock", "mushroom_brown", "mushroom_red", "hill", "hill_top", "hill_top_smile" },
                new List<string> { "fence", "fence_broken", "sign", "sign_exit", "sign_left", "sign_right", "window", "Torch", "torch_off" },
                new List<string> { "ladder_top", "ladder_middle", "ladder_bottom", "chain", "rope", "rop_attached", "weight", "heart", "star", "Water" },
                new List<string> { "FlagRed", "FlagBlue", "FlagGreen", "FlagYellow" },
                TerrainMaterials.Select(m => "BackWall" + Capitalize(m)).ToList(),
            });
        }

        static void BuildPalette(string paletteName, List<List<string>> rows)
        {
            string path = $"{Palettes}/{paletteName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                AssetDatabase.DeleteAsset(path);

            GridPaletteUtility.CreateNewPalette(Palettes, paletteName, GridLayout.CellLayout.Rectangle, GridPalette.CellSizing.Automatic, Vector3.one, GridLayout.CellSwizzle.XYZ);

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            Tilemap tilemap = root.GetComponentInChildren<Tilemap>();
            for (int y = 0; y < rows.Count; y++)
                for (int x = 0; x < rows[y].Count; x++)
                    tilemap.SetTile(new Vector3Int(x, -y, 0), TileByKey(rows[y][x]));
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // ---------------------------------------------------------------
        // Effects and projectiles
        // ---------------------------------------------------------------

        static void BuildEffectsAndProjectiles()
        {
            // Explosion effect from the platformer art.
            string explosionPath = Prefabs + "/Effects/Explosion.prefab";
            MoveIfNeeded(ExplosionSource, explosionPath);
            EditPrefab(explosionPath, root =>
            {
                foreach (SpriteRenderer spriteRenderer in root.GetComponentsInChildren<SpriteRenderer>())
                    spriteRenderer.sortingOrder = OrderEffects;
                foreach (AudioSource audio in root.GetComponentsInChildren<AudioSource>())
                    audio.spatialBlend = 0f;
            });
            GameObject explosion = AssetDatabase.LoadAssetAtPath<GameObject>(explosionPath);
            PrefabLookup["Explosion"] = explosion;

            // Small impact puff for bullets: a quiet, small explosion.
            var impactInstance = (GameObject)PrefabUtility.InstantiatePrefab(explosion);
            impactInstance.transform.localScale = Vector3.one * 0.3f;
            foreach (AudioSource audio in impactInstance.GetComponentsInChildren<AudioSource>())
                audio.mute = true;
            PrefabLookup["Impact"] = SavePrefab(impactInstance, Prefabs + "/Effects/Impact.prefab");

            // Rocket from the platformer art.
            string rocketPath = Prefabs + "/Projectiles/Rocket.prefab";
            MoveIfNeeded(RocketSource, rocketPath);
            EditPrefab(rocketPath, root =>
            {
                var projectile = root.GetComponent<Projectile>();
                projectile.Damage = 100f;
                projectile.ExplosionRadius = 1.6f;
                projectile.PushForce = 14f;
                projectile.Gravity = 0f;
                projectile.Lifetime = 4f;
                projectile.ExplodeOnImpact = true;
                projectile.FaceFlightDirection = true;
                projectile.ExplosionEffect = explosion;
                projectile.ExplosionSound = null;
                ConfigureProjectileBody(root.GetComponent<Rigidbody2D>());
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                    renderer.sortingOrder = renderer is SpriteRenderer ? OrderProjectiles : OrderProjectiles - 1;
                root.transform.localScale = Vector3.one;
            });
            PrefabLookup["Rocket"] = AssetDatabase.LoadAssetAtPath<GameObject>(rocketPath);

            PrefabLookup["Bullet"] = SavePrefab(Bullet("Bullet", 1f, 1.2f, 4f, new Color(1f, 0.85f, 0.3f)), Prefabs + "/Projectiles/Bullet.prefab");
            PrefabLookup["Pellet"] = SavePrefab(Bullet("Pellet", 0.7f, 0.35f, 2.5f, new Color(1f, 0.6f, 0.2f)), Prefabs + "/Projectiles/Pellet.prefab");
            PrefabLookup["Grenade"] = SavePrefab(Grenade(), Prefabs + "/Projectiles/Grenade.prefab");
        }

        static GameObject Bullet(string name, float size, float lifetime, float push, Color color)
        {
            var root = new GameObject(name);
            root.transform.localScale = Vector3.one * size;
            AddSprite(root, GeneratedSprite("Dot"), OrderProjectiles, color);

            var collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.1f;

            ConfigureProjectileBody(root.AddComponent<Rigidbody2D>());

            var trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.08f;
            trail.startWidth = 0.12f * size;
            trail.endWidth = 0f;
            trail.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.sortingOrder = OrderProjectiles - 1;
            trail.numCapVertices = 2;

            var projectile = root.AddComponent<Projectile>();
            projectile.Damage = 100f;
            projectile.ExplosionRadius = 0f;
            projectile.PushForce = push;
            projectile.Gravity = 0f;
            projectile.Lifetime = lifetime;
            projectile.ExplodeOnImpact = true;
            projectile.FaceFlightDirection = false;
            projectile.ExplosionEffect = PrefabLookup["Impact"];
            return root;
        }

        static GameObject Grenade()
        {
            var root = new GameObject("Grenade");
            root.transform.localScale = Vector3.one * 0.45f;
            AddSprite(root, Kenney("bomb"), OrderProjectiles);

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.36f;
            collider.sharedMaterial = LoadPhysics("Grenade");

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            ConfigureProjectileBody(body);
            body.gravityScale = 1f;
            body.angularDamping = 0.5f;

            var projectile = root.AddComponent<Projectile>();
            projectile.Damage = 100f;
            projectile.ExplosionRadius = 2.2f;
            projectile.PushForce = 18f;
            projectile.Gravity = 1f;
            projectile.Lifetime = 1.6f;
            projectile.ExplodeOnImpact = false;
            projectile.FaceFlightDirection = false;
            projectile.ExplosionEffect = PrefabLookup["Explosion"];
            return root;
        }

        static void ConfigureProjectileBody(Rigidbody2D body)
        {
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
        }

        // ---------------------------------------------------------------
        // Weapons
        // ---------------------------------------------------------------

        // Taken from the original hero prefab: bazooka sprite and the muzzle position relative to it.
        static Sprite bazookaSprite;
        static Vector3 muzzleOffset = new Vector3(1.82f, 0.18f, 0f);

        static void BuildWeapons()
        {
            ReadHeroWeaponData();

            MakeWeapon("Bazooka", Color.white, Vector3.one, "Rocket", speed: 14f, delay: 0.8f, ammo: 4, recoil: 6f,
                sound: PlatformerSound("FX/bazooka.wav"));
            MakeWeapon("Pistol", new Color(0.75f, 0.82f, 0.95f), new Vector3(0.55f, 0.55f, 1f), "Bullet", speed: 26f, delay: 0.3f, ammo: 9, recoil: 1.5f,
                sound: KenneySound("sfx_throw"));
            MakeWeapon("Shotgun", new Color(0.9f, 0.62f, 0.4f), new Vector3(0.85f, 0.7f, 1f), "Pellet", speed: 22f, delay: 0.9f, ammo: 3, recoil: 9f,
                sound: PlatformerSound("FX/bazooka.wav"), projectiles: 6, spread: 20f);
            MakeWeapon("GrenadeLauncher", new Color(0.55f, 0.9f, 0.5f), new Vector3(0.8f, 0.85f, 1f), "Grenade", speed: 11f, delay: 1f, ammo: 4, recoil: 3f,
                sound: KenneySound("sfx_throw"), aimAngle: 25f);
            MakeWeapon("Minigun", new Color(0.45f, 0.45f, 0.52f), new Vector3(0.9f, 0.6f, 1f), "Bullet", speed: 24f, delay: 0.08f, ammo: 40, recoil: 0.7f,
                sound: KenneySound("sfx_throw"), spread: 8f, automatic: true, volume: 0.3f);
        }

        static void ReadHeroWeaponData()
        {
            string source = File.Exists(HeroSource) ? HeroSource : $"{PlayerFolder}/Player.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(source);
            Transform bazooka = root.transform.Find("Bazooka");
            Transform gun = root.transform.Find("Gun");
            if (bazooka != null)
            {
                var spriteRenderer = bazooka.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                    bazookaSprite = spriteRenderer.sprite;
                if (gun != null)
                    muzzleOffset = bazooka.InverseTransformPoint(gun.position);
            }
            PrefabUtility.UnloadPrefabContents(root);

            if (bazookaSprite == null)
                bazookaSprite = AssetDatabase.LoadAllAssetsAtPath($"{Platformer}/Sprites/_Character/char_hero_beanMan.png").OfType<Sprite>().FirstOrDefault(s => s.name.ToLower().Contains("bazooka"));
        }

        static void MakeWeapon(string name, Color tint, Vector3 scale, string projectile, float speed, float delay, int ammo, float recoil,
            AudioClip sound, int projectiles = 1, float spread = 0f, float aimAngle = 0f, bool automatic = false, float volume = 0.7f)
        {
            var root = new GameObject(name);
            root.transform.localScale = scale;
            AddSprite(root, bazookaSprite, OrderPlayer, tint);

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localPosition = new Vector3(muzzleOffset.x, muzzleOffset.y, 0f);

            var weapon = root.AddComponent<Weapon>();
            weapon.Projectile = PrefabLookup[projectile].GetComponent<Projectile>();
            weapon.ProjectileSpeed = speed;
            weapon.ProjectilesPerShot = projectiles;
            weapon.Spread = spread;
            weapon.AimAngle = aimAngle;
            weapon.FireDelay = delay;
            weapon.Automatic = automatic;
            weapon.Recoil = recoil;
            weapon.Ammo = ammo;
            weapon.Muzzle = muzzle.transform;
            weapon.FireSound = sound;
            weapon.FireVolume = volume;

            GameObject prefab = SavePrefab(root, $"{Prefabs}/Weapons/{name}.prefab");
            WeaponLookup[name] = prefab.GetComponent<Weapon>();
        }

        // ---------------------------------------------------------------
        // Player
        // ---------------------------------------------------------------

        static void BuildPlayer()
        {
            string playerPath = $"{PlayerFolder}/Player.prefab";
            MoveIfNeeded(HeroSource, playerPath);

            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Kit + "/Input/ArenaControls.inputactions");

            EditPrefab(playerPath, root =>
            {
                root.name = "Player";
                root.transform.position = Vector3.zero;

                Transform gun = root.transform.Find("Gun");
                if (gun != null)
                    Object.DestroyImmediate(gun.gameObject);

                // The bazooka object stays (the animations move it) and holds the current weapon.
                Transform mount = root.transform.Find("Bazooka");
                var mountRenderer = mount.GetComponent<SpriteRenderer>();
                if (mountRenderer != null)
                    mountRenderer.enabled = false;

                foreach (AudioSource audio in root.GetComponentsInChildren<AudioSource>())
                    Object.DestroyImmediate(audio);

                foreach (SpriteRenderer spriteRenderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    spriteRenderer.sortingLayerID = 0;
                    spriteRenderer.sortingOrder = OrderPlayer;
                }

                Transform marker = root.transform.Find("Marker");
                if (marker == null)
                {
                    marker = new GameObject("Marker").transform;
                    marker.SetParent(root.transform, false);
                }
                marker.localPosition = new Vector3(0f, 1.2f, -1f);
                marker.localScale = Vector3.one * 1.3f;
                var markerRenderer = marker.GetComponent<SpriteRenderer>();
                if (markerRenderer == null)
                    markerRenderer = marker.gameObject.AddComponent<SpriteRenderer>();
                markerRenderer.sprite = GeneratedSprite("Marker");
                markerRenderer.sortingOrder = OrderPlayer + 1;

                var body = root.GetComponent<Rigidbody2D>();
                body.sleepMode = RigidbodySleepMode2D.NeverSleep;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;

                var character = root.GetComponent<Character>();
                character.Health = 100f;
                character.MoveForce = 10000f;
                character.MaxSpeed = 10f;
                character.JumpForce = 500f;
                character.ExtraJumps = 0;
                character.AirControl = 1f;
                character.WeaponMount = mount;
                character.ColoredParts = new[] { root.transform.Find("body").GetComponent<SpriteRenderer>() };
                character.ColorStrength = 0.6f;
                character.Marker = markerRenderer;
                character.JumpSounds = new[] { PlatformerSound("Player/Jumps/Player-jump1.wav"), PlatformerSound("Player/Jumps/Player-jump2.wav"), PlatformerSound("Player/Jumps/Player-jump3.wav") };
                character.HurtSounds = new[] { PlatformerSound("Player/Ouch/Player-ouch1.wav"), PlatformerSound("Player/Ouch/Player-ouch2.wav"), PlatformerSound("Player/Ouch/Player-ouch3.wav") };
                character.DeathSound = PlatformerSound("Player/Ouch/Player-ouch4.wav");

                var input = root.GetComponent<PlayerInput>();
                input.actions = controls;
                input.defaultActionMap = "Player";
                input.defaultControlScheme = null;
                input.neverAutoSwitchControlSchemes = true;
                input.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

                SetLayerRecursively(root, CharacterLayer);
            });
        }

        // ---------------------------------------------------------------
        // Pickups
        // ---------------------------------------------------------------

        static void BuildPickups()
        {
            foreach (KeyValuePair<string, Weapon> weapon in WeaponLookup)
                PrefabLookup["Pickup_" + weapon.Key] = SavePrefab(Pickup("Pickup_" + weapon.Key, weapon.Value, null, 8f), $"{Prefabs}/Pickups/Pickup_{weapon.Key}.prefab");

            Weapon[] all = WeaponLookup.Values.ToArray();
            PrefabLookup["Pickup_Random"] = SavePrefab(Pickup("Pickup_Random", all[0], all, 8f), $"{Prefabs}/Pickups/Pickup_Random.prefab");
            PrefabLookup["Drop_Random"] = SavePrefab(Pickup("Drop_Random", all[0], all, 0f), $"{Prefabs}/Pickups/Drop_Random.prefab");
        }

        static GameObject Pickup(string name, Weapon weapon, Weapon[] random, float respawn)
        {
            var root = new GameObject(name);
            var collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.6f;

            var icon = new GameObject("Icon");
            icon.transform.SetParent(root.transform, false);
            SpriteRenderer iconRenderer = AddSprite(icon, null, OrderPickups);

            var pickup = root.AddComponent<WeaponPickup>();
            pickup.Weapon = weapon;
            pickup.RandomWeapons = random;
            pickup.RespawnTime = respawn;
            pickup.SwapWeapons = true;
            pickup.PickupSound = PlatformerSound("FX/bombCollect.wav");
            pickup.Icon = iconRenderer;
            pickup.UpdateIcon();
            return root;
        }

        // ---------------------------------------------------------------
        // Building blocks
        // ---------------------------------------------------------------

        static void BuildBlocks()
        {
            string blocks = Prefabs + "/Blocks";
            string rules = Prefabs + "/Rules";

            // --- Rules ---
            {
                var root = new GameObject("LevelRules");
                root.AddComponent<LevelRules>();
                PrefabLookup["LevelRules"] = SavePrefab(root, $"{rules}/LevelRules.prefab");
            }
            {
                var root = new GameObject("SpawnPoint");
                root.AddComponent<SpawnPoint>();
                PrefabLookup["SpawnPoint"] = SavePrefab(root, $"{rules}/SpawnPoint.prefab");
            }
            {
                var root = new GameObject("KillZone");
                var box = root.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(60f, 4f);
                var hazard = root.AddComponent<Hazard>();
                hazard.InstantKill = true;
                hazard.RemoveObjects = true;
                PrefabLookup["KillZone"] = SavePrefab(root, $"{rules}/KillZone.prefab");
            }
            {
                var root = new GameObject("FinishFlag");
                SpriteRenderer spriteRenderer = AddSprite(root, Kenney("flag_yellow_a"), OrderProps);
                var box = root.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.8f, 1f);
                var flag = root.AddComponent<FinishFlag>();
                flag.Renderer = spriteRenderer;
                flag.WaveSprites = new[] { Kenney("flag_yellow_a"), Kenney("flag_yellow_b") };
                flag.Sound = KenneySound("sfx_magic");
                PrefabLookup["FinishFlag"] = SavePrefab(root, $"{rules}/FinishFlag.prefab");
            }
            {
                var root = new GameObject("ScoreZone");
                root.transform.localScale = new Vector3(4f, 3f, 1f);
                SpriteRenderer spriteRenderer = AddSprite(root, GeneratedSprite("Square"), OrderBackDecoration + 1, new Color(1f, 1f, 1f, 0.35f));
                root.AddComponent<BoxCollider2D>().size = Vector2.one;
                var zone = root.AddComponent<ScoreZone>();
                zone.Renderer = spriteRenderer;
                zone.PointSound = KenneySound("sfx_coin");
                PrefabLookup["ScoreZone"] = SavePrefab(root, $"{rules}/ScoreZone.prefab");
            }
            {
                var root = new GameObject("Goal");
                root.transform.localScale = new Vector3(1f, 3f, 1f);
                AddSprite(root, GeneratedSprite("Square"), OrderBackDecoration + 1, new Color(1f, 1f, 1f, 0.25f));
                root.AddComponent<BoxCollider2D>().size = Vector2.one;
                root.AddComponent<Goal>().GoalSound = KenneySound("sfx_gem");
                PrefabLookup["Goal"] = SavePrefab(root, $"{rules}/Goal.prefab");
            }
            {
                var root = new GameObject("Ball");
                root.transform.localScale = Vector3.one * 1.3f;
                AddSprite(root, GeneratedSprite("Ball"), OrderProps + 2);
                var circle = root.AddComponent<CircleCollider2D>();
                circle.radius = 0.47f;
                circle.sharedMaterial = LoadPhysics("Bouncy");
                var body = root.AddComponent<Rigidbody2D>();
                body.mass = 0.4f;
                body.gravityScale = 0.6f;
                body.linearDamping = 0.1f;
                body.angularDamping = 0.5f;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                var ball = root.AddComponent<Ball>();
                ball.MaxSpeed = 22f;
                ball.BounceSound = KenneySound("sfx_bump");
                PrefabLookup["Ball"] = SavePrefab(root, $"{rules}/Ball.prefab");
            }
            {
                var root = new GameObject("Coin");
                AddSprite(root, Kenney("coin_gold"), OrderProps);
                root.AddComponent<CircleCollider2D>().radius = 0.35f;
                var coin = root.AddComponent<Collectible>();
                coin.Points = 1;
                coin.Sound = KenneySound("sfx_coin");
                PrefabLookup["Coin"] = SavePrefab(root, $"{rules}/Coin.prefab");
            }
            {
                var root = new GameObject("Gem");
                AddSprite(root, Kenney("gem_blue"), OrderProps);
                root.AddComponent<CircleCollider2D>().radius = 0.35f;
                var gem = root.AddComponent<Collectible>();
                gem.Points = 3;
                gem.Sound = KenneySound("sfx_gem");
                PrefabLookup["Gem"] = SavePrefab(root, $"{rules}/Gem.prefab");
            }
            {
                var root = new GameObject("NameSign");
                root.transform.localScale = Vector3.one * 1.6f;
                AddSprite(root, Kenney("sign"), OrderProps - 1);
                var label = new GameObject("Label");
                label.transform.SetParent(root.transform, false);
                label.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                var text = label.AddComponent<TextMesh>();
                var font = AssetDatabase.LoadAssetAtPath<Font>(PlayerFolder + "/ArenaFont.ttf");
                text.font = font;
                text.fontSize = 48;
                text.characterSize = 0.06f;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.color = new Color(0.35f, 0.2f, 0.1f);
                text.text = "Dein Name";
                var meshRenderer = label.GetComponent<MeshRenderer>();
                if (font != null)
                    meshRenderer.sharedMaterial = font.material;
                meshRenderer.sortingOrder = OrderProps;
                root.AddComponent<NameSign>().Label = text;
                PrefabLookup["NameSign"] = SavePrefab(root, $"{rules}/NameSign.prefab");
            }

            // --- Logic ---
            {
                var root = new GameObject("TriggerZone");
                var box = root.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(2f, 2f);
                root.AddComponent<TriggerZone>();
                PrefabLookup["TriggerZone"] = SavePrefab(root, $"{blocks}/TriggerZone.prefab");
            }
            {
                var root = new GameObject("Timer");
                root.AddComponent<Timer>();
                PrefabLookup["Timer"] = SavePrefab(root, $"{blocks}/Timer.prefab");
            }
            {
                var root = new GameObject("Counter");
                root.AddComponent<Counter>();
                PrefabLookup["Counter"] = SavePrefab(root, $"{blocks}/Counter.prefab");
            }
            {
                var root = new GameObject("Spawner");
                root.AddComponent<Spawner>();
                PrefabLookup["Spawner"] = SavePrefab(root, $"{blocks}/Spawner.prefab");
            }
            PrefabLookup["Lever"] = SavePrefab(SwitchObject("Lever", "lever_left", "lever_right", 0f), $"{blocks}/Lever.prefab");
            PrefabLookup["Button"] = SavePrefab(SwitchObject("Button", "switch_red", "switch_red_pressed", 3f), $"{blocks}/Button.prefab");

            // --- Doors ---
            {
                var root = new GameObject("Door");
                var bottom = new GameObject("Bottom");
                bottom.transform.SetParent(root.transform, false);
                var top = new GameObject("Top");
                top.transform.SetParent(root.transform, false);
                top.transform.localPosition = Vector3.up;
                SpriteRenderer bottomRenderer = AddSprite(bottom, Kenney("door_closed"), OrderProps);
                SpriteRenderer topRenderer = AddSprite(top, Kenney("door_closed_top"), OrderProps);
                var box = root.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.8f, 2f);
                box.offset = new Vector2(0f, 0.5f);
                var door = root.AddComponent<Door>();
                door.Parts = new[]
                {
                    new Door.DoorPart { Renderer = bottomRenderer, ClosedSprite = Kenney("door_closed"), OpenSprite = Kenney("door_open") },
                    new Door.DoorPart { Renderer = topRenderer, ClosedSprite = Kenney("door_closed_top"), OpenSprite = Kenney("door_open_top") },
                };
                door.OpenSound = KenneySound("sfx_select");
                door.CloseSound = KenneySound("sfx_bump");
                root.layer = GroundLayer;
                PrefabLookup["Door"] = SavePrefab(root, $"{blocks}/Door.prefab");
            }
            {
                var root = new GameObject("LockBlock");
                SpriteRenderer spriteRenderer = AddSprite(root, Kenney("lock_red"), OrderProps);
                root.AddComponent<BoxCollider2D>().size = Vector2.one;
                var door = root.AddComponent<Door>();
                door.Parts = new[] { new Door.DoorPart { Renderer = spriteRenderer, ClosedSprite = Kenney("lock_red") } };
                door.OpenSound = KenneySound("sfx_magic");
                root.layer = GroundLayer;
                PrefabLookup["LockBlock"] = SavePrefab(root, $"{blocks}/LockBlock.prefab");
            }
            {
                var root = new GameObject("Key");
                AddSprite(root, Kenney("key_red"), OrderProps);
                root.AddComponent<CircleCollider2D>().radius = 0.35f;
                var key = root.AddComponent<Collectible>();
                key.Points = 0;
                key.Sound = KenneySound("sfx_magic");
                PrefabLookup["Key"] = SavePrefab(root, $"{blocks}/Key.prefab");
            }

            // --- Movement ---
            PrefabLookup["MovingPlatform"] = SavePrefab(Platform("MovingPlatform", new Vector2(4f, 0f)), $"{blocks}/MovingPlatform.prefab");
            PrefabLookup["Elevator"] = SavePrefab(Platform("Elevator", new Vector2(0f, 4f)), $"{blocks}/Elevator.prefab");
            {
                var root = new GameObject("JumpPad");
                SpriteRenderer spriteRenderer = AddSprite(root, Kenney("spring"), OrderProps);
                var box = root.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.8f, 0.6f);
                box.offset = new Vector2(0f, -0.15f);
                var pad = root.AddComponent<JumpPad>();
                pad.LaunchSpeed = 18f;
                pad.Renderer = spriteRenderer;
                pad.IdleSprite = Kenney("spring");
                pad.LaunchSprite = Kenney("spring_out");
                pad.Sound = KenneySound("sfx_jump-high");
                PrefabLookup["JumpPad"] = SavePrefab(root, $"{blocks}/JumpPad.prefab");
            }
            {
                var root = new GameObject("TeleporterPair");
                Teleporter a = TeleporterObject("Teleporter A", root.transform, new Vector3(-3f, 0f, 0f), new Color(0.75f, 0.5f, 1f));
                Teleporter b = TeleporterObject("Teleporter B", root.transform, new Vector3(3f, 0f, 0f), new Color(0.4f, 0.9f, 1f));
                a.Target = b;
                b.Target = a;
                PrefabLookup["TeleporterPair"] = SavePrefab(root, $"{blocks}/TeleporterPair.prefab");
            }

            // --- Danger ---
            {
                var root = new GameObject("Mine");
                root.transform.localScale = Vector3.one * 0.6f;
                SpriteRenderer spriteRenderer = AddSprite(root, Kenney("bomb"), OrderProps);
                var circle = root.AddComponent<CircleCollider2D>();
                circle.isTrigger = true;
                circle.radius = 0.45f;
                var mine = root.AddComponent<Explosive>();
                mine.Radius = 2.2f;
                mine.PushForce = 16f;
                mine.FuseTime = 0.3f;
                mine.ExplodeOnTouch = true;
                mine.ExplodeWhenHit = true;
                mine.Renderer = spriteRenderer;
                mine.ArmedSprite = Kenney("bomb_active");
                mine.ExplosionEffect = PrefabLookup["Explosion"];
                mine.FuseSound = PlatformerSound("FX/bombFuse.wav");
                PrefabLookup["Mine"] = SavePrefab(root, $"{blocks}/Mine.prefab");
            }
            {
                var root = new GameObject("TNTBlock");
                SpriteRenderer spriteRenderer = AddSprite(root, Kenney("block_strong_danger"), OrderProps);
                var box = root.AddComponent<BoxCollider2D>();
                box.size = Vector2.one * 0.95f;
                box.sharedMaterial = LoadPhysics("Crate");
                var body = root.AddComponent<Rigidbody2D>();
                body.mass = 2f;
                var tnt = root.AddComponent<Explosive>();
                tnt.Radius = 3f;
                tnt.PushForce = 20f;
                tnt.FuseTime = 0.15f;
                tnt.ExplodeOnTouch = false;
                tnt.ExplodeWhenHit = true;
                tnt.Renderer = spriteRenderer;
                tnt.ExplosionEffect = PrefabLookup["Explosion"];
                tnt.ExplosionSound = PlatformerSound("FX/bigBoom.ogg");
                PrefabLookup["TNTBlock"] = SavePrefab(root, $"{blocks}/TNTBlock.prefab");
            }
            {
                var root = new GameObject("Crate");
                AddSprite(root, Kenney("block_planks"), OrderProps);
                var box = root.AddComponent<BoxCollider2D>();
                box.size = Vector2.one * 0.95f;
                box.sharedMaterial = LoadPhysics("Crate");
                root.AddComponent<Rigidbody2D>().mass = 1.5f;
                var crate = root.AddComponent<Breakable>();
                crate.Health = 50f;
                crate.BreakSound = KenneySound("sfx_disappear");
                PrefabLookup["Crate"] = SavePrefab(root, $"{blocks}/Crate.prefab");
            }
            {
                var root = new GameObject("WeaponCrate");
                AddSprite(root, Kenney("block_exclamation"), OrderProps);
                root.AddComponent<BoxCollider2D>().size = Vector2.one;
                var crate = root.AddComponent<Breakable>();
                crate.Health = 1f;
                crate.Drops = new[] { PrefabLookup["Drop_Random"] };
                crate.BreakSound = KenneySound("sfx_disappear");
                root.layer = GroundLayer;
                PrefabLookup["WeaponCrate"] = SavePrefab(root, $"{blocks}/WeaponCrate.prefab");
            }
            {
                var root = new GameObject("BrickWall");
                AddSprite(root, Kenney("bricks_brown"), OrderProps);
                root.AddComponent<BoxCollider2D>().size = Vector2.one;
                var wall = root.AddComponent<Breakable>();
                wall.Health = 150f;
                wall.BreakSound = KenneySound("sfx_disappear");
                root.layer = GroundLayer;
                PrefabLookup["BrickWall"] = SavePrefab(root, $"{blocks}/BrickWall.prefab");
            }
            {
                var root = new GameObject("Spikes");
                AddSprite(root, Kenney("spikes"), OrderProps);
                var box = root.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(0.9f, 0.45f);
                box.offset = new Vector2(0f, -0.25f);
                root.AddComponent<Hazard>().InstantKill = true;
                PrefabLookup["Spikes"] = SavePrefab(root, $"{blocks}/Spikes.prefab");
            }
            {
                var root = new GameObject("Saw");
                AddSprite(root, Kenney("saw"), OrderProps);
                var circle = root.AddComponent<CircleCollider2D>();
                circle.isTrigger = true;
                circle.radius = 0.42f;
                root.AddComponent<Hazard>().InstantKill = true;
                var mover = root.AddComponent<Mover>();
                mover.Offset = new Vector2(3f, 0f);
                mover.Speed = 2f;
                mover.RotationSpeed = -360f;
                mover.WaitTime = 0.2f;
                PrefabLookup["Saw"] = SavePrefab(root, $"{blocks}/Saw.prefab");
            }
            {
                // Sudden death: lava starts rising after a while.
                var root = new GameObject("RisingLava");
                root.transform.position = new Vector3(0f, -20f, 0f);
                var surface = new GameObject("Surface");
                surface.transform.SetParent(root.transform, false);
                surface.transform.localPosition = new Vector3(0f, 5.5f, 0f);
                SpriteRenderer surfaceRenderer = AddSprite(surface, Kenney("lava_top"), OrderFrontDecoration - 1);
                surfaceRenderer.drawMode = SpriteDrawMode.Tiled;
                surfaceRenderer.size = new Vector2(40f, 1f);
                SpriteRenderer lavaRenderer = AddSprite(root, Kenney("lava"), OrderFrontDecoration - 1);
                lavaRenderer.drawMode = SpriteDrawMode.Tiled;
                lavaRenderer.size = new Vector2(40f, 10f);
                var box = root.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(40f, 11f);
                box.offset = new Vector2(0f, 0.3f);
                var hazard = root.AddComponent<Hazard>();
                hazard.InstantKill = true;
                hazard.RemoveObjects = true;
                var mover = root.AddComponent<Mover>();
                mover.Offset = new Vector2(0f, 12f);
                mover.Speed = 0.5f;
                mover.MoveAutomatically = false;
                var timer = root.AddComponent<Timer>();
                timer.Seconds = 30f;
                UnityEventTools.AddVoidPersistentListener(timer.OnFinished, mover.MoveToEnd);
                PrefabLookup["RisingLava"] = SavePrefab(root, $"{blocks}/RisingLava.prefab");
            }
        }

        static GameObject SwitchObject(string name, string offSprite, string onSprite, float autoReset)
        {
            var root = new GameObject(name);
            SpriteRenderer spriteRenderer = AddSprite(root, Kenney(offSprite), OrderProps);
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.8f, 0.7f);
            box.offset = new Vector2(0f, -0.15f);
            var lever = root.AddComponent<Switch>();
            lever.Renderer = spriteRenderer;
            lever.OffSprite = Kenney(offSprite);
            lever.OnSprite = Kenney(onSprite);
            lever.AutoResetTime = autoReset;
            lever.Sound = KenneySound("sfx_select");
            return root;
        }

        static GameObject Platform(string name, Vector2 offset)
        {
            var root = new GameObject(name);
            string[] parts = { "terrain_stone_cloud_left", "terrain_stone_cloud_middle", "terrain_stone_cloud_right" };
            for (int i = 0; i < parts.Length; i++)
            {
                var part = new GameObject(parts[i].Replace("terrain_stone_cloud_", ""));
                part.transform.SetParent(root.transform, false);
                part.transform.localPosition = new Vector3(i - 1f, 0f, 0f);
                AddSprite(part, Kenney(parts[i]), OrderGround + 1);
            }

            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(3f, 0.6f);
            box.offset = new Vector2(0f, 0.18f);
            box.usedByEffector = true;
            var effector = root.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 170f;

            var mover = root.AddComponent<Mover>();
            mover.Offset = offset;
            mover.Speed = 2f;
            mover.WaitTime = 0.6f;

            root.layer = GroundLayer;
            return root;
        }

        static Teleporter TeleporterObject(string name, Transform parent, Vector3 position, Color color)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            AddSprite(root, Kenney("door_open"), OrderProps - 1, color);
            var top = new GameObject("Top");
            top.transform.SetParent(root.transform, false);
            top.transform.localPosition = Vector3.up;
            AddSprite(top, Kenney("door_open_top"), OrderProps - 1, color);
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.6f, 1.4f);
            box.offset = new Vector2(0f, 0.3f);
            var teleporter = root.AddComponent<Teleporter>();
            teleporter.Sound = KenneySound("sfx_magic");
            return teleporter;
        }

        // ---------------------------------------------------------------
        // Scenes
        // ---------------------------------------------------------------

        class LevelScene
        {
            public Scene Scene;
            public Tilemap BackDecoration;
            public Tilemap Ground;
            public Tilemap Platforms;
            public Tilemap Hazards;
            public Tilemap FrontDecoration;
            public LevelRules Rules;
            public SpriteRenderer Background;
            public readonly List<SpawnPoint> Spawns = new List<SpawnPoint>();
        }

        static LevelScene CreateLevelScene(string backgroundSprite)
        {
            var level = new LevelScene { Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single) };

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 9f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.8f, 0.95f);
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 100f;
            cameraObject.AddComponent<UniversalAdditionalCameraData>();

            var lightObject = new GameObject("Global Light 2D");
            var light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            light.color = Color.white;
            light.targetSortingLayers = SortingLayer.layers.Select(layer => layer.id).ToArray();

            var background = new GameObject("Background");
            background.transform.localScale = new Vector3(4.5f, 4.5f, 1f);
            level.Background = AddSprite(background, AssetDatabase.LoadAssetAtPath<Sprite>($"{KenneyBackgrounds}/{backgroundSprite}.png"), OrderBackground);
            level.Background.drawMode = SpriteDrawMode.Tiled;
            level.Background.size = new Vector2(8f, 4f);

            var grid = new GameObject("Grid", typeof(Grid));
            level.BackDecoration = CreateTilemap(grid, "Back Decoration", OrderBackDecoration, 0);
            level.Ground = CreateTilemap(grid, "Ground", OrderGround, GroundLayer);
            AddSolidCollision(level.Ground.gameObject, oneWay: false);
            level.Platforms = CreateTilemap(grid, "Platforms", OrderGround + 1, GroundLayer);
            AddSolidCollision(level.Platforms.gameObject, oneWay: true);
            level.Hazards = CreateTilemap(grid, "Hazards", OrderGround + 2, 0);
            level.Hazards.gameObject.AddComponent<TilemapCollider2D>().isTrigger = true;
            level.Hazards.gameObject.AddComponent<Hazard>().InstantKill = true;
            level.FrontDecoration = CreateTilemap(grid, "Front Decoration", OrderFrontDecoration, 0);

            var rulesObject = new GameObject("Level Rules");
            level.Rules = rulesObject.AddComponent<LevelRules>();

            Place("KillZone", new Vector3(0f, -14f, 0f));
            return level;
        }

        static Tilemap CreateTilemap(GameObject grid, string name, int order, int layer)
        {
            var tilemapObject = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            tilemapObject.transform.SetParent(grid.transform, false);
            tilemapObject.layer = layer;
            tilemapObject.GetComponent<TilemapRenderer>().sortingOrder = order;
            return tilemapObject.GetComponent<Tilemap>();
        }

        static void AddSolidCollision(GameObject tilemapObject, bool oneWay)
        {
            var body = tilemapObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var composite = tilemapObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            var tilemapCollider = tilemapObject.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            if (!oneWay)
                return;

            composite.usedByEffector = true;
            var effector = tilemapObject.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 170f;
        }

        static GameObject Place(string prefabKey, Vector3 position)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(PrefabLookup[prefabKey]);
            instance.transform.position = position;
            return instance;
        }

        static SpawnPoint PlaceSpawn(LevelScene level, int player, Vector3 position)
        {
            GameObject instance = Place("SpawnPoint", position);
            instance.name = $"Spawn Point P{player}";
            var spawn = instance.GetComponent<SpawnPoint>();
            spawn.PlayerNumber = player;
            level.Spawns.Add(spawn);
            return spawn;
        }

        static void Fill(Tilemap tilemap, string tileKey, int xMin, int yMin, int xMax, int yMax)
        {
            TileBase tile = TileByKey(tileKey);
            for (int x = xMin; x <= xMax; x++)
                for (int y = yMin; y <= yMax; y++)
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
        }

        static void Save(LevelScene level, string path)
        {
            foreach (NameSign sign in Object.FindObjectsByType<NameSign>(FindObjectsSortMode.None))
                sign.Refresh();

            // In batch mode the editor loop never runs, so the tile colliders have to be built by hand.
            foreach (TilemapCollider2D tilemapCollider in Object.FindObjectsByType<TilemapCollider2D>(FindObjectsSortMode.None))
            {
                tilemapCollider.enabled = false;
                tilemapCollider.enabled = true;
                tilemapCollider.ProcessTilemapChanges();
            }
            foreach (CompositeCollider2D composite in Object.FindObjectsByType<CompositeCollider2D>(FindObjectsSortMode.None))
            {
                composite.GenerateGeometry();
                if (composite.pathCount == 0 && composite.GetComponent<Tilemap>().GetUsedTilesCount() > 0)
                    Debug.LogWarning($"[ArenaKit] {path}: {composite.name} has tiles but no collision shapes.");
            }

            EditorSceneManager.MarkSceneDirty(level.Scene);
            EditorSceneManager.SaveScene(level.Scene, path);
        }

        /// <summary>The starting point of every student level: a small grass arena.</summary>
        static void BuildTemplateScene()
        {
            LevelScene level = CreateLevelScene("background_color_hills");
            level.Rules.LevelName = "Meine Arena";

            // Floor and walls. The camera shows x -16..16 and y -9..9.
            Fill(level.Ground, "Grass", -16, -9, 15, -8);
            Fill(level.Ground, "Grass", -16, -7, -16, 8);
            Fill(level.Ground, "Grass", 15, -7, 15, 8);

            Fill(level.Platforms, "GrassPlatform", -11, -4, -7, -4);
            Fill(level.Platforms, "GrassPlatform", 6, -4, 10, -4);
            Fill(level.Platforms, "GrassPlatform", -3, 0, 2, 0);
            Fill(level.Platforms, "GrassPlatform", -14, 3, -11, 3);
            Fill(level.Platforms, "GrassPlatform", 10, 3, 13, 3);

            level.BackDecoration.SetTile(new Vector3Int(-9, -7, 0), TileByKey("bush"));
            level.BackDecoration.SetTile(new Vector3Int(8, -7, 0), TileByKey("grass"));
            level.BackDecoration.SetTile(new Vector3Int(-1, 1, 0), TileByKey("mushroom_red"));

            PlaceSpawn(level, 1, new Vector3(-12f, -5.8f, 0f));
            PlaceSpawn(level, 2, new Vector3(12f, -5.8f, 0f));
            PlaceSpawn(level, 3, new Vector3(-9f, -1.8f, 0f));
            PlaceSpawn(level, 4, new Vector3(8f, -1.8f, 0f));

            Place("Pickup_Random", new Vector3(-0.5f, 1.8f, 0f));
            Place("Pickup_Random", new Vector3(-12.5f, 4.8f, 0f));
            Place("Pickup_Random", new Vector3(11.5f, 4.8f, 0f));
            Place("NameSign", new Vector3(-14f, -6.2f, 0f));

            Save(level, $"{Scenes}/Template.unity");
        }

        /// <summary>Demo: a classic arena with crates, weapons, jump pads and a teleporter.</summary>
        static void BuildDemoArena()
        {
            LevelScene level = CreateLevelScene("background_color_trees");
            level.Rules.LevelName = "Kisten-Chaos";
            level.Rules.BuiltBy = "Pascal";

            Fill(level.Ground, "Stone", -16, -9, -4, -8);
            Fill(level.Ground, "Stone", 3, -9, 15, -8);
            Fill(level.Hazards, "Lava", -3, -9, 2, -9);
            Fill(level.Ground, "Stone", -16, -7, -16, 8);
            Fill(level.Ground, "Stone", 15, -7, 15, 8);
            Fill(level.Ground, "Stone", -2, -3, 1, -2);

            Fill(level.Platforms, "StonePlatform", -12, -4, -8, -4);
            Fill(level.Platforms, "StonePlatform", 7, -4, 11, -4);
            Fill(level.Platforms, "StonePlatform", -5, 2, 4, 2);
            Fill(level.Platforms, "StonePlatform", -14, 5, -11, 5);
            Fill(level.Platforms, "StonePlatform", 10, 5, 13, 5);

            Fill(level.BackDecoration, "BackWallStone", -2, -1, 1, 1);
            level.FrontDecoration.SetTile(new Vector3Int(-15, -7, 0), TileByKey("Torch"));
            level.FrontDecoration.SetTile(new Vector3Int(14, -7, 0), TileByKey("Torch"));

            PlaceSpawn(level, 1, new Vector3(-13f, -5.8f, 0f));
            PlaceSpawn(level, 2, new Vector3(13f, -5.8f, 0f));
            PlaceSpawn(level, 3, new Vector3(-10f, -1.8f, 0f));
            PlaceSpawn(level, 4, new Vector3(9f, -1.8f, 0f));

            Place("Pickup_Bazooka", new Vector3(-0.5f, 3.8f, 0f));
            Place("Pickup_Shotgun", new Vector3(-12.5f, 6.8f, 0f));
            Place("Pickup_Minigun", new Vector3(11.5f, 6.8f, 0f));
            Place("Pickup_Pistol", new Vector3(-10f, -6.2f, 0f));
            Place("Pickup_GrenadeLauncher", new Vector3(9f, -6.2f, 0f));

            Place("Crate", new Vector3(-6.5f, -6.5f, 0f));
            Place("Crate", new Vector3(-6.5f, -5.5f, 0f));
            Place("Crate", new Vector3(5.5f, -6.5f, 0f));
            Place("TNTBlock", new Vector3(6.5f, -6.5f, 0f));
            Place("WeaponCrate", new Vector3(0.5f, -0.5f, 0f));
            Place("JumpPad", new Vector3(-14.5f, -6.5f, 0f));
            Place("JumpPad", new Vector3(13.5f, -6.5f, 0f));
            Place("Mine", new Vector3(-1.2f, -0.7f, 0f));

            GameObject teleporters = Place("TeleporterPair", new Vector3(-0.5f, 5f, 0f));
            teleporters.transform.Find("Teleporter A").localPosition = new Vector3(-9f, 1.5f, 0f);
            teleporters.transform.Find("Teleporter B").localPosition = new Vector3(9f, 1.5f, 0f);

            Place("NameSign", new Vector3(-5f, -6.2f, 0f));

            Save(level, $"{Levels}/Demo_Arena/Demo_Arena.unity");
        }

        /// <summary>Demo: Blobby-Volley with rocket launchers.</summary>
        static void BuildDemoVolleyball()
        {
            string folder = $"{Levels}/Demo_RocketVolleyball";

            // A level's own weapon: a prefab variant of the kit bazooka that never runs out of ammo.
            var bazookaInstance = (GameObject)PrefabUtility.InstantiatePrefab(WeaponLookup["Bazooka"].gameObject);
            var infinite = bazookaInstance.GetComponent<Weapon>();
            infinite.Ammo = -1;
            infinite.FireDelay = 0.6f;
            Weapon volleyBazooka = SavePrefab(bazookaInstance, $"{folder}/VolleyBazooka.prefab").GetComponent<Weapon>();

            LevelScene level = CreateLevelScene("background_color_desert");
            LevelRules rules = level.Rules;
            rules.LevelName = "Raketen-Volleyball";
            rules.BuiltBy = "Pascal";
            rules.LastPlayerStandingWins = false;
            rules.PointsToWin = 5;
            rules.RespawnPlayers = true;
            rules.PlayerHealth = 100000f;

            Fill(level.Ground, "Sand", -16, -9, 15, -8);
            Fill(level.Ground, "Sand", -16, -7, -16, 9);
            Fill(level.Ground, "Sand", 15, -7, 15, 9);
            Fill(level.Ground, "Sand", -15, 9, 14, 9);
            Fill(level.Ground, "Stone", -1, -7, 0, -3);

            level.BackDecoration.SetTile(new Vector3Int(-8, -7, 0), TileByKey("cactus"));
            level.BackDecoration.SetTile(new Vector3Int(7, -7, 0), TileByKey("rock"));

            PlaceSpawn(level, 1, new Vector3(-8f, -5.8f, 0f)).StartWeapon = volleyBazooka;
            PlaceSpawn(level, 2, new Vector3(8f, -5.8f, 0f)).StartWeapon = volleyBazooka;
            PlaceSpawn(level, 3, new Vector3(-12f, -5.8f, 0f)).StartWeapon = volleyBazooka;
            PlaceSpawn(level, 4, new Vector3(12f, -5.8f, 0f)).StartWeapon = volleyBazooka;

            GameObject leftGoal = Place("Goal", new Vector3(-8f, -6.75f, 0f));
            leftGoal.name = "Goal Left (point for P2)";
            leftGoal.transform.localScale = new Vector3(14f, 0.5f, 1f);
            leftGoal.GetComponent<Goal>().PointForPlayer = 2;

            GameObject rightGoal = Place("Goal", new Vector3(8f, -6.75f, 0f));
            rightGoal.name = "Goal Right (point for P1)";
            rightGoal.transform.localScale = new Vector3(14f, 0.5f, 1f);
            rightGoal.GetComponent<Goal>().PointForPlayer = 1;

            Place("Ball", new Vector3(-0.2f, 5f, 0f));
            Place("NameSign", new Vector3(-14f, -6.2f, 0f));

            Save(level, $"{folder}/Demo_RocketVolleyball.unity");
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        static SpriteRenderer AddSprite(GameObject target, Sprite sprite, int order, Color? color = null)
        {
            var spriteRenderer = target.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = order;
            spriteRenderer.color = color ?? Color.white;
            return spriteRenderer;
        }

        static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void EditPrefab(string path, System.Action<GameObject> edit)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void MoveIfNeeded(string from, string to)
        {
            if (File.Exists(to) || !File.Exists(from))
                return;
            string error = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(error))
                Debug.LogError($"[ArenaKit] Could not move {from} to {to}: {error}");
        }

        static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        static string Capitalize(string text)
        {
            return string.Concat(text.Split('_').Select(part => part.Length > 0 ? char.ToUpper(part[0]) + part.Substring(1) : part));
        }
    }
}

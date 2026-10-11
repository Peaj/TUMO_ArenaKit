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
        const string WeaponArt = Art + "/Weapons";
        /// <summary>The weapon sprites are drawn at 2x (see Tools/weapon_sprites.py) and shown a bit bigger than the bazooka art.</summary>
        const int WeaponPixelsPerUnit = 160;
        const string Physics = Kit + "/Physics";
        const string Prefabs = Kit + "/Prefabs";
        const string PlayerFolder = Kit + "/Resources/ArenaKit";
        const string Tiles = Kit + "/Tiles";
        const string Palettes = Kit + "/Palettes";
        const string Scenes = Kit + "/Scenes";
        const string Levels = "Assets/Levels";
        /// <summary>Every building block in one row, to try them out (TUMO > Alle Bausteine ansehen).</summary>
        public const string ShowcaseScene = Scenes + "/Showcase.unity";
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
            ConfigureWeaponSpriteImport();
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
            BuildShowcase();

            ArenaMenu.SetBuildScenes(new[]
            {
                $"{Levels}/Demo_Arena/Demo_Arena.unity",
                $"{Levels}/Demo_RocketVolleyball/Demo_RocketVolleyball.unity",
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (string scene in new[]
                     {
                         $"{Scenes}/Template.unity",
                         ShowcaseScene,
                         $"{Levels}/Demo_Arena/Demo_Arena.unity",
                         $"{Levels}/Demo_RocketVolleyball/Demo_RocketVolleyball.unity",
                     })
                RebuildTileColliders(scene);

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

        [System.Serializable]
        class WeaponSpriteInfo
        {
            public string name;
            public float[] size;
            public float[] grip;
            public float[] muzzle;
        }

        [System.Serializable]
        class WeaponSpriteList
        {
            public WeaponSpriteInfo[] weapons;
        }

        static readonly Dictionary<string, WeaponSpriteInfo> WeaponSprites = new Dictionary<string, WeaponSpriteInfo>();

        /// <summary>Weapon sprites get their pivot at the grip, so swapping the sprite of a weapon keeps it in the hand.</summary>
        static void ConfigureWeaponSpriteImport()
        {
            WeaponSprites.Clear();
            string jsonPath = WeaponArt + "/weapons.json";
            if (File.Exists(jsonPath))
                foreach (WeaponSpriteInfo info in JsonUtility.FromJson<WeaponSpriteList>(File.ReadAllText(jsonPath)).weapons)
                    WeaponSprites[info.name] = info;

            foreach (string path in AssetDatabase.FindAssets("t:Texture2D", new[] { WeaponArt }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null)
                    continue;

                Vector2 pivot = new Vector2(0.5f, 0.5f);
                if (WeaponSprites.TryGetValue(Path.GetFileNameWithoutExtension(path), out WeaponSpriteInfo info) && info.grip != null && info.grip.Length == 2)
                    pivot = new Vector2(info.grip[0] / info.size[0], 1f - info.grip[1] / info.size[1]);

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                bool upToDate = importer.textureType == TextureImporterType.Sprite && importer.spritePixelsPerUnit == WeaponPixelsPerUnit
                    && settings.spriteAlignment == (int)SpriteAlignment.Custom && (settings.spritePivot - pivot).sqrMagnitude < 0.000001f;
                if (upToDate)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = WeaponPixelsPerUnit;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        static Sprite WeaponSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{WeaponArt}/{name}.png");
        }

        /// <summary>Muzzle position relative to the grip, in units.</summary>
        static Vector3 WeaponMuzzle(string name)
        {
            if (!WeaponSprites.TryGetValue(name, out WeaponSpriteInfo info) || info.muzzle == null || info.grip == null)
                return Vector3.right;
            return new Vector3(info.muzzle[0] - info.grip[0], info.grip[1] - info.muzzle[1], 0f) / WeaponPixelsPerUnit;
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
            if (!TileLookup.TryGetValue(key, out TileBase tile) || tile == null)
            {
                // "Grass" is a ground tile and a decoration tile: the solid one wins.
                tile = new[] { "Ground", "Platforms", "Hazards", "Decoration" }
                    .Select(folder => FindAsset<TileBase>(key, "t:TileBase", $"{Tiles}/{folder}"))
                    .FirstOrDefault(found => found != null);
                TileLookup[key] = tile;
            }
            return tile;
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
                // Original scale of the platformer rocket: as big as the bazooka muzzle.
                root.transform.localScale = Vector3.one * 2f;
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
        /// <summary>Where the front hand is, relative to the weapon mount. Hand weapons are held there by their grip.</summary>
        static Vector3 handOffset = new Vector3(0.81f, -0.45f, 0f);

        static void BuildWeapons()
        {
            ReadHeroWeaponData();

            MakeWeapon("Bazooka", null, "Rocket", speed: 14f, delay: 0.8f, ammo: 4, recoil: 6f,
                sound: PlatformerSound("FX/bazooka.wav"));
            MakeWeapon("Pistol", "Pistol", "Bullet", speed: 26f, delay: 0.3f, ammo: 9, recoil: 1.5f,
                sound: KenneySound("sfx_throw"));
            MakeWeapon("Shotgun", "Shotgun", "Pellet", speed: 22f, delay: 0.9f, ammo: 3, recoil: 9f,
                sound: PlatformerSound("FX/bazooka.wav"), projectiles: 6, spread: 20f);
            MakeWeapon("GrenadeLauncher", "GrenadeLauncher", "Grenade", speed: 11f, delay: 1f, ammo: 4, recoil: 3f,
                sound: KenneySound("sfx_throw"), aimAngle: 25f); // rotated upwards, the muzzle turns with it
            MakeWeapon("Minigun", "Minigun", "Bullet", speed: 24f, delay: 0.08f, ammo: 40, recoil: 0.7f,
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
                Transform hand = root.transform.Find("leftHand");
                if (hand != null)
                    handOffset = bazooka.InverseTransformPoint(hand.position);
            }
            PrefabUtility.UnloadPrefabContents(root);

            if (bazookaSprite == null)
                bazookaSprite = AssetDatabase.LoadAllAssetsAtPath($"{Platformer}/Sprites/_Character/char_hero_beanMan.png").OfType<Sprite>().FirstOrDefault(s => s.name.ToLower().Contains("bazooka"));
        }

        /// <param name="spriteName">Sprite from Assets/Kit/Art/Weapons, held by its grip. null = the original bazooka on the shoulder.</param>
        static void MakeWeapon(string name, string spriteName, string projectile, float speed, float delay, int ammo, float recoil,
            AudioClip sound, int projectiles = 1, float spread = 0f, float aimAngle = 0f, bool automatic = false, float volume = 0.7f)
        {
            var root = new GameObject(name);
            Sprite sprite = spriteName != null ? WeaponSprite(spriteName) : null;
            AddSprite(root, sprite != null ? sprite : bazookaSprite, OrderPlayer);

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(root.transform, false);
            // The weapon shoots where the muzzle points, so an upward angle is just a rotation of the whole weapon.
            root.transform.localRotation = Quaternion.Euler(0f, 0f, aimAngle);
            if (sprite != null)
            {
                // Held in the front hand by its grip (the sprite pivot).
                root.transform.localPosition = new Vector3(handOffset.x, handOffset.y, 0f);
                muzzle.transform.localPosition = WeaponMuzzle(spriteName);
            }
            else
            {
                muzzle.transform.localPosition = new Vector3(muzzleOffset.x, muzzleOffset.y, 0f);
            }

            var weapon = root.AddComponent<Weapon>();
            weapon.Projectile = PrefabLookup[projectile].GetComponent<Projectile>();
            weapon.ProjectileSpeed = speed;
            weapon.ProjectilesPerShot = projectiles;
            weapon.Spread = spread;
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
                body.linearDamping = 0f;

                var character = root.GetComponent<Character>();
                character.Health = 100f;
                character.MoveForce = 10000f;
                character.MaxSpeed = 10f;
                character.Braking = 50f;
                character.JumpHeight = 4.5f;
                character.MinJumpHeight = 1.5f;
                character.FallGravity = 2f;
                character.MaxFallSpeed = 20f;
                character.ExtraJumps = 0;
                character.AirControl = 1f;
                character.WeaponMount = mount;
                character.ColoredParts = new[] { root.transform.Find("body").GetComponent<SpriteRenderer>() };
                character.ColorStrength = 1f;
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
                // Players die, crates disappear, balls go back to their start.
                var root = new GameObject("KillZone");
                root.AddComponent<BoxCollider2D>().size = new Vector2(60f, 4f);
                TriggerZone zone = AddTriggerZone(root, TriggerTarget.Everything);
                var damager = root.AddComponent<Damager>();
                Wire(zone.OnEnter, damager.Kill);
                Wire(zone.OnEnter, root.AddComponent<Remover>().RemoveObject);
                PrefabLookup["KillZone"] = SavePrefab(root, $"{rules}/KillZone.prefab");
            }
            {
                var root = new GameObject("FinishFlag");
                SpriteRenderer spriteRenderer = AddSprite(root, Kenney("flag_yellow_a"), OrderProps);
                var wave = root.AddComponent<SpriteAnimation>();
                wave.Renderer = spriteRenderer;
                wave.Sprites = new[] { Kenney("flag_yellow_a"), Kenney("flag_yellow_b") };
                root.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 1f);
                TriggerZone zone = AddTriggerZone(root, TriggerTarget.Players);
                Wire(zone.OnEnter, AddSound(root, KenneySound("sfx_magic")).Play);
                Wire(zone.OnEnter, root.AddComponent<Score>().WinRound);
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
                // A ball gets in: the player who touched it last (or "For Player") scores, the ball goes back.
                TriggerZone zone = AddTriggerZone(root, TriggerTarget.Balls);
                Wire(zone.OnEnter, AddSound(root, KenneySound("sfx_gem")).Play);
                Wire(zone.OnEnter, root.AddComponent<Score>().AddPoints);
                var remover = root.AddComponent<Remover>();
                remover.BallResetDelay = 0.5f;
                Wire(zone.OnEnter, remover.RemoveObject);
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
            PrefabLookup["Coin"] = SavePrefab(Collectible("Coin", "coin_gold", "sfx_coin", 1), $"{rules}/Coin.prefab");
            PrefabLookup["Gem"] = SavePrefab(Collectible("Gem", "gem_blue", "sfx_gem", 3), $"{rules}/Gem.prefab");
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

            PrefabLookup["CameraRig"] = SavePrefab(CameraRig(), $"{rules}/CameraRig.prefab");

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
            PrefabLookup["Lever"] = SavePrefab(SwitchObject("Lever", "lever_left", "lever_right", 0f, toggle: true), $"{blocks}/Lever.prefab");
            PrefabLookup["Button"] = SavePrefab(SwitchObject("Button", "switch_red", "switch_red_pressed", 3f, toggle: false), $"{blocks}/Button.prefab");

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
            // A key gives no points: connect its TriggerZone > On Enter to a LockBlock > Open.
            PrefabLookup["Key"] = SavePrefab(Collectible("Key", "key_red", "sfx_magic", 0), $"{blocks}/Key.prefab");

            // --- Movement ---
            PrefabLookup["MovingPlatform"] = SavePrefab(Platform("MovingPlatform", new Vector2(4f, 0f)), $"{blocks}/MovingPlatform.prefab");
            PrefabLookup["Elevator"] = SavePrefab(Platform("Elevator", new Vector2(0f, 4f)), $"{blocks}/Elevator.prefab");
            {
                // TriggerZone > ForcePush: launches everything along the green arrow, the spring pops out.
                var root = new GameObject("JumpPad");
                SpriteRenderer spriteRenderer = AddSprite(root, Kenney("spring"), OrderProps);
                var box = root.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.8f, 0.6f);
                box.offset = new Vector2(0f, -0.15f);
                TriggerZone zone = AddTriggerZone(root, TriggerTarget.AnyPhysicsObject);
                var push = root.AddComponent<ForcePush>();
                push.Speed = 13f;
                push.Sound = KenneySound("sfx_jump-high");
                Wire(zone.OnEnter, push.Push);
                SwapSprite spring = AddSwapSprite(root, spriteRenderer, Kenney("spring_out"));
                UnityEventTools.AddFloatPersistentListener(zone.OnEnter, spring.ShowFor, 0.25f);
                PrefabLookup["JumpPad"] = SavePrefab(root, $"{blocks}/JumpPad.prefab");
            }
            {
                var root = new GameObject("TeleporterPair");
                Teleporter a = TeleporterObject("Teleporter A", root.transform, new Vector3(-3f, 0f, 0f), new Color(0.75f, 0.5f, 1f));
                Teleporter b = TeleporterObject("Teleporter B", root.transform, new Vector3(3f, 0f, 0f), new Color(0.4f, 0.9f, 1f));
                a.Target = b.transform;
                b.Target = a.transform;
                PrefabLookup["TeleporterPair"] = SavePrefab(root, $"{blocks}/TeleporterPair.prefab");
            }

            // --- Danger ---
            {
                // Step on it: the light blinks. Step off (or shoot it): boom.
                var root = new GameObject("Mine");
                root.transform.localScale = Vector3.one * 0.8f;
                SpriteRenderer spriteRenderer = AddSprite(root, PropSprite("Mine"), OrderProps);
                var box = root.AddComponent<BoxCollider2D>();
                box.size = new Vector2(1.1f, 0.6f);
                box.offset = new Vector2(0f, 0.3f);
                TriggerZone zone = AddTriggerZone(root, TriggerTarget.Players);
                SwapSprite light = AddSwapSprite(root, spriteRenderer, PropSprite("Mine_Armed"));
                light.Blink = true;
                light.BlinkSpeed = 8f;
                Wire(zone.OnEnter, light.Show);
                Wire(zone.OnEnter, AddSound(root, KenneySound("sfx_select")).Play);
                var mine = root.AddComponent<Explosive>();
                mine.Radius = 2.2f;
                mine.PushForce = 16f;
                mine.FuseTime = 0f;
                mine.ExplosionEffect = PrefabLookup["Explosion"];
                Wire(zone.OnExit, mine.Explode);
                Wire(root.AddComponent<HitSensor>().OnHit, mine.Explode);
                PrefabLookup["Mine"] = SavePrefab(root, $"{blocks}/Mine.prefab");
            }
            {
                // Breakable > On Broken > Explosive: any damage lights the fuse.
                var root = new GameObject("TNTBlock");
                SpriteRenderer spriteRenderer = AddSprite(root, Kenney("block_strong_danger"), OrderProps);
                var box = root.AddComponent<BoxCollider2D>();
                box.size = Vector2.one * 0.95f;
                box.sharedMaterial = LoadPhysics("Crate");
                var body = root.AddComponent<Rigidbody2D>();
                body.mass = 2f;
                var breakable = root.AddComponent<Breakable>();
                breakable.Health = 1f;
                breakable.RemoveWhenBroken = false;
                var tnt = root.AddComponent<Explosive>();
                tnt.Radius = 3f;
                tnt.PushForce = 20f;
                tnt.FuseTime = 0.15f;
                tnt.Renderer = spriteRenderer;
                tnt.ExplosionEffect = PrefabLookup["Explosion"];
                tnt.ExplosionSound = PlatformerSound("FX/bigBoom.ogg");
                Wire(breakable.OnBroken, tnt.Explode);
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
                // Breakable > On Broken > Spawner: a random weapon appears.
                var root = new GameObject("WeaponCrate");
                AddSprite(root, Kenney("block_exclamation"), OrderProps);
                root.AddComponent<BoxCollider2D>().size = Vector2.one;
                var crate = root.AddComponent<Breakable>();
                crate.Health = 1f;
                crate.BreakSound = KenneySound("sfx_disappear");
                var drop = root.AddComponent<Spawner>();
                drop.Prefabs = new[] { PrefabLookup["Drop_Random"] };
                drop.Interval = 0f;
                drop.SpawnAtStart = false;
                drop.MaxAlive = 0;
                Wire(crate.OnBroken, drop.Spawn);
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
                box.size = new Vector2(0.9f, 0.45f);
                box.offset = new Vector2(0f, -0.25f);
                AddDeadlyTouch(root, TriggerTarget.Players);
                PrefabLookup["Spikes"] = SavePrefab(root, $"{blocks}/Spikes.prefab");
            }
            {
                var root = new GameObject("Saw");
                AddSprite(root, Kenney("saw"), OrderProps);
                root.AddComponent<CircleCollider2D>().radius = 0.42f;
                AddDeadlyTouch(root, TriggerTarget.Players);
                var mover = root.AddComponent<Mover>();
                mover.Offset = new Vector2(3f, 0f);
                mover.Speed = 2f;
                mover.RotationSpeed = -360f;
                mover.WaitTime = 0.2f;
                PrefabLookup["Saw"] = SavePrefab(root, $"{blocks}/Saw.prefab");
            }
            {
                // Sudden death: after 30 seconds (Timer) the lava rises (Mover). Touching it kills (TriggerZone > Damager).
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
                box.size = new Vector2(40f, 11f);
                box.offset = new Vector2(0f, 0.3f);
                TriggerZone zone = AddDeadlyTouch(root, TriggerTarget.Everything);
                Wire(zone.OnEnter, root.AddComponent<Remover>().RemoveObject);
                var mover = root.AddComponent<Mover>();
                mover.Offset = new Vector2(0f, 12f);
                mover.Speed = 0.5f;
                mover.MoveAutomatically = false;
                var timer = root.AddComponent<Timer>();
                timer.Seconds = 30f;
                Wire(timer.OnFinished, mover.MoveToEnd);
                PrefabLookup["RisingLava"] = SavePrefab(root, $"{blocks}/RisingLava.prefab");
            }
        }

        /// <summary>
        /// Main camera + Cinemachine camera that frames all players (Duck Game style),
        /// stays inside the level bounds and shakes on explosions.
        /// </summary>
        static GameObject CameraRig()
        {
            var root = new GameObject("CameraRig");

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 9f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.8f, 0.95f);
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 100f;
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var brain = cameraObject.AddComponent<Unity.Cinemachine.CinemachineBrain>();
            brain.DefaultBlend = new Unity.Cinemachine.CinemachineBlendDefinition(Unity.Cinemachine.CinemachineBlendDefinition.Styles.Cut, 0f);

            var levelCenter = new GameObject("Level Center");
            levelCenter.transform.SetParent(root.transform, false);

            var boundsObject = new GameObject("Level Bounds");
            boundsObject.transform.SetParent(root.transform, false);
            var bounds = boundsObject.AddComponent<BoxCollider2D>();
            bounds.isTrigger = true;
            bounds.size = new Vector2(34f, 20f);

            var groupObject = new GameObject("Target Group");
            groupObject.transform.SetParent(root.transform, false);
            var group = groupObject.AddComponent<Unity.Cinemachine.CinemachineTargetGroup>();
            group.PositionMode = Unity.Cinemachine.CinemachineTargetGroup.PositionModes.GroupCenter;
            group.RotationMode = Unity.Cinemachine.CinemachineTargetGroup.RotationModes.Manual;
            group.Targets.Clear();
            group.AddMember(levelCenter.transform, 1f, 17f);

            var virtualObject = new GameObject("Virtual Camera");
            virtualObject.transform.SetParent(root.transform, false);
            virtualObject.transform.localPosition = new Vector3(0f, 0f, -10f);
            var virtualCamera = virtualObject.AddComponent<Unity.Cinemachine.CinemachineCamera>();
            var lens = Unity.Cinemachine.LensSettings.Default;
            lens.OrthographicSize = 9f;
            lens.NearClipPlane = 0.3f;
            lens.FarClipPlane = 100f;
            virtualCamera.Lens = lens;
            virtualCamera.Follow = group.transform;

            var composer = virtualObject.AddComponent<Unity.Cinemachine.CinemachinePositionComposer>();
            composer.CameraDistance = 10f;
            composer.CenterOnActivate = true;

            // Order matters: frame the group first, then keep the result inside the level.
            var framing = virtualObject.AddComponent<Unity.Cinemachine.CinemachineGroupFraming>();
            var confiner = virtualObject.AddComponent<Unity.Cinemachine.CinemachineConfiner2D>();
            confiner.BoundingShape2D = bounds;
            confiner.Damping = 0f;

            var listener = virtualObject.AddComponent<Unity.Cinemachine.CinemachineImpulseListener>();
            listener.Gain = 1f;
            listener.ChannelMask = 1;
            listener.Use2DDistance = true;

            var impulse = root.AddComponent<Unity.Cinemachine.CinemachineImpulseSource>();
            impulse.ImpulseDefinition.ImpulseChannel = 1;
            impulse.ImpulseDefinition.ImpulseType = Unity.Cinemachine.CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            impulse.ImpulseDefinition.ImpulseShape = Unity.Cinemachine.CinemachineImpulseDefinition.ImpulseShapes.Explosion;
            impulse.ImpulseDefinition.ImpulseDuration = 0.3f;
            impulse.DefaultVelocity = new Vector3(0.15f, -0.25f, 0f);

            var background = new GameObject("Background");
            background.transform.SetParent(root.transform, false);
            background.transform.localPosition = new Vector3(0f, 0f, 10f);
            SpriteRenderer backgroundRenderer = AddSprite(background, AssetDatabase.LoadAssetAtPath<Sprite>($"{KenneyBackgrounds}/background_color_hills.png"), OrderBackground);
            backgroundRenderer.drawMode = SpriteDrawMode.Tiled;
            background.AddComponent<CameraBackground>().TargetCamera = camera;

            var arena = root.AddComponent<ArenaCamera>();
            arena.OutputCamera = camera;
            arena.VirtualCamera = virtualCamera;
            arena.TargetGroup = group;
            arena.Composer = composer;
            arena.GroupFraming = framing;
            arena.Confiner = confiner;
            arena.Impulse = impulse;
            arena.Bounds = bounds;
            arena.LevelCenter = levelCenter.transform;
            arena.MinSize = 6f;
            arena.MaxSize = 14f;
            arena.Framing = 0.6f;
            arena.Smoothing = 1f;
            return root;
        }

        /// <summary>A Switch flipped by walking into it (TriggerZone) or shooting it (HitSensor).</summary>
        static GameObject SwitchObject(string name, string offSprite, string onSprite, float autoReset, bool toggle)
        {
            var root = new GameObject(name);
            SpriteRenderer spriteRenderer = AddSprite(root, Kenney(offSprite), OrderProps);
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.8f, 0.7f);
            box.offset = new Vector2(0f, -0.15f);
            var lever = root.AddComponent<Switch>();
            lever.Renderer = spriteRenderer;
            lever.OffSprite = Kenney(offSprite);
            lever.OnSprite = Kenney(onSprite);
            lever.AutoResetTime = autoReset;
            lever.Sound = KenneySound("sfx_select");
            UnityEngine.Events.UnityAction flip = toggle ? lever.Toggle : lever.TurnOn;
            Wire(AddTriggerZone(root, TriggerTarget.Players).OnEnter, flip);
            Wire(root.AddComponent<HitSensor>().OnHit, flip);
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
            box.size = new Vector2(0.6f, 1.4f);
            box.offset = new Vector2(0f, 0.3f);
            TriggerZone zone = AddTriggerZone(root, TriggerTarget.AnyPhysicsObject);
            var teleporter = root.AddComponent<Teleporter>();
            teleporter.Sound = KenneySound("sfx_magic");
            Wire(zone.OnEnter, teleporter.Teleport);
            return teleporter;
        }

        /// <summary>Coins, gems, keys: TriggerZone > sound, points (if any) and disappear.</summary>
        static GameObject Collectible(string name, string sprite, string sound, int points)
        {
            var root = new GameObject(name);
            AddSprite(root, Kenney(sprite), OrderProps);
            root.AddComponent<CircleCollider2D>().radius = 0.35f;
            root.AddComponent<Hover>();
            TriggerZone zone = AddTriggerZone(root, TriggerTarget.Players);
            Wire(zone.OnEnter, AddSound(root, KenneySound(sound)).Play);
            if (points != 0)
            {
                var score = root.AddComponent<Score>();
                score.Points = points;
                Wire(zone.OnEnter, score.AddPoints);
            }
            Wire(zone.OnEnter, root.AddComponent<Disappear>().Hide);
            return root;
        }

        /// <summary>Spikes, saws, lava: TriggerZone > Damager > Hurt.</summary>
        static TriggerZone AddDeadlyTouch(GameObject root, TriggerTarget target)
        {
            TriggerZone zone = AddTriggerZone(root, target);
            Wire(zone.OnEnter, root.AddComponent<Damager>().Hurt);
            return zone;
        }

        static TriggerZone AddTriggerZone(GameObject root, TriggerTarget target)
        {
            foreach (Collider2D collider in root.GetComponents<Collider2D>())
                collider.isTrigger = true;
            var zone = root.AddComponent<TriggerZone>();
            zone.ReactsTo = target;
            return zone;
        }

        static PlaySound AddSound(GameObject root, AudioClip clip)
        {
            var sound = root.AddComponent<PlaySound>();
            sound.Sound = clip;
            return sound;
        }

        static SwapSprite AddSwapSprite(GameObject root, SpriteRenderer spriteRenderer, Sprite sprite)
        {
            var swap = root.AddComponent<SwapSprite>();
            swap.Renderer = spriteRenderer;
            swap.Sprite = sprite;
            return swap;
        }

        /// <summary>Connects an event to an action, like dragging it in the Inspector.</summary>
        static void Wire(UnityEngine.Events.UnityEventBase unityEvent, UnityEngine.Events.UnityAction action)
        {
            UnityEventTools.AddVoidPersistentListener(unityEvent, action);
        }

        static Sprite PropSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{WeaponArt}/Props/{name}.png");
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

            GameObject cameraRig = Place("CameraRig", Vector3.zero);
            level.Background = cameraRig.transform.Find("Background").GetComponent<SpriteRenderer>();
            level.Background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{KenneyBackgrounds}/{backgroundSprite}.png");

            var lightObject = new GameObject("Global Light 2D");
            var light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            light.color = Color.white;
            light.targetSortingLayers = SortingLayer.layers.Select(layer => layer.id).ToArray();

            var grid = new GameObject("Grid", typeof(Grid));
            level.BackDecoration = CreateTilemap(grid, "Back Decoration", OrderBackDecoration, 0);
            level.Ground = CreateTilemap(grid, "Ground", OrderGround, GroundLayer);
            AddSolidCollision(level.Ground.gameObject, oneWay: false);
            level.Platforms = CreateTilemap(grid, "Platforms", OrderGround + 1, GroundLayer);
            AddSolidCollision(level.Platforms.gameObject, oneWay: true);
            level.Hazards = CreateTilemap(grid, "Hazards", OrderGround + 2, 0);
            level.Hazards.gameObject.AddComponent<TilemapCollider2D>().isTrigger = true;
            // Lava and spike tiles: TriggerZone > Damager > Hurt.
            AddDeadlyTouch(level.Hazards.gameObject, TriggerTarget.Everything);
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
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(PrefabByKey(prefabKey));
            instance.transform.position = position;
            return instance;
        }

        /// <summary>A kit prefab by name. Falls back to the saved asset when the kit was not built in this run.</summary>
        static GameObject PrefabByKey(string key)
        {
            if (!PrefabLookup.TryGetValue(key, out GameObject prefab) || prefab == null)
            {
                prefab = FindAsset<GameObject>(key, "t:Prefab", Prefabs);
                PrefabLookup[key] = prefab;
            }
            return prefab;
        }

        static T FindAsset<T>(string name, string filter, string folder) where T : Object
        {
            return AssetDatabase.FindAssets($"{name} {filter}", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => string.Equals(Path.GetFileNameWithoutExtension(path), name, System.StringComparison.OrdinalIgnoreCase))
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .FirstOrDefault(asset => asset != null);
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

        /// <summary>
        /// Tile colliders built in the same editor frame as the tiles are saved incomplete.
        /// So: open the saved scene again, rebuild the colliders, save, reload and check that
        /// every solid tile really has collision.
        /// </summary>
        static void RebuildTileColliders(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath);
            foreach (TilemapCollider2D tilemapCollider in Object.FindObjectsByType<TilemapCollider2D>(FindObjectsSortMode.None))
            {
                tilemapCollider.enabled = false;
                tilemapCollider.enabled = true;
                tilemapCollider.ProcessTilemapChanges();
            }
            foreach (CompositeCollider2D composite in Object.FindObjectsByType<CompositeCollider2D>(FindObjectsSortMode.None))
                composite.GenerateGeometry();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorSceneManager.OpenScene(scenePath);
            int missing = CountTilesWithoutCollision();
            if (missing > 0)
                Debug.LogError($"[ArenaKit] {scenePath}: {missing} solid tiles have no collision.");
            else
                Debug.Log($"[ArenaKit] {scenePath}: tile collision complete.");
        }

        /// <summary>Solid tiles (Ground, Platforms) whose cell center is not covered by the tilemap's collider.</summary>
        public static int CountTilesWithoutCollision()
        {
            Physics2D.SyncTransforms();
            int missing = 0;
            foreach (TilemapCollider2D tilemapCollider in Object.FindObjectsByType<TilemapCollider2D>(FindObjectsSortMode.None))
            {
                if (tilemapCollider.isTrigger)
                    continue;

                var tilemap = tilemapCollider.GetComponent<Tilemap>();
                var composite = tilemapCollider.GetComponent<CompositeCollider2D>();
                Collider2D solid = composite != null ? composite : tilemapCollider;
                foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
                {
                    var tile = tilemap.GetTile(cell);
                    if (tile == null || tilemap.GetColliderType(cell) != Tile.ColliderType.Grid)
                        continue;
                    if (!solid.OverlapPoint(tilemap.GetCellCenterWorld(cell)))
                        missing++;
                }
            }
            return missing;
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
            Fill(level.Platforms, "StonePlatform", -15, 0, -14, 0);
            Fill(level.Platforms, "StonePlatform", 13, 0, 14, 0);

            Fill(level.BackDecoration, "BackWallStone", -2, -1, 1, 1);
            level.FrontDecoration.SetTile(new Vector3Int(-15, -7, 0), TileByKey("Torch"));
            level.FrontDecoration.SetTile(new Vector3Int(14, -7, 0), TileByKey("Torch"));

            PlaceSpawn(level, 1, new Vector3(-12.27f, -5.8f, 0f));
            PlaceSpawn(level, 2, new Vector3(12.27f, -5.8f, 0f));
            PlaceSpawn(level, 3, new Vector3(-10f, -1.8f, 0f));
            PlaceSpawn(level, 4, new Vector3(9f, -1.8f, 0f));

            Place("Pickup_Bazooka", new Vector3(-0.5f, 3.8f, 0f));
            Place("Pickup_Shotgun", new Vector3(-11.29f, 6.8f, 0f));
            Place("Pickup_Minigun", new Vector3(11.5f, 6.8f, 0f));
            Place("Pickup_Pistol", new Vector3(-10f, -6.2f, 0f));
            Place("Pickup_GrenadeLauncher", new Vector3(9f, -6.2f, 0f));

            Place("Crate", new Vector3(-6.5f, -6.5f, 0f));
            Place("Crate", new Vector3(-6.5f, -5.5f, 0f));
            Place("Crate", new Vector3(5.5f, -6.5f, 0f));
            Place("TNTBlock", new Vector3(6.5f, -6.5f, 0f));
            Place("WeaponCrate", new Vector3(0.5f, -0.5f, 0f));
            Place("JumpPad", new Vector3(-13.47f, -6.5f, 0f));
            Place("JumpPad", new Vector3(13.5f, -6.5f, 0f));
            Place("Mine", new Vector3(-1.2f, -1f, 0f));

            GameObject teleporters = Place("TeleporterPair", new Vector3(-0.5f, 5f, 0f));
            teleporters.transform.Find("Teleporter A").localPosition = new Vector3(-12.77f, 1.5f, 0f);
            teleporters.transform.Find("Teleporter B").localPosition = new Vector3(13.82f, 1.5f, 0f);

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
            leftGoal.GetComponent<Score>().ForPlayer = 2;

            GameObject rightGoal = Place("Goal", new Vector3(8f, -6.75f, 0f));
            rightGoal.name = "Goal Right (point for P1)";
            rightGoal.transform.localScale = new Vector3(14f, 0.5f, 1f);
            rightGoal.GetComponent<Score>().ForPlayer = 1;

            Place("Ball", new Vector3(-0.2f, 5f, 0f));
            Place("NameSign", new Vector3(-14f, -6.2f, 0f));

            Save(level, $"{folder}/Demo_RocketVolleyball.unity");
        }

        /// <summary>Builds only the showcase scene from the saved kit prefabs (batch mode: -executeMethod ...BuildShowcaseOnly).</summary>
        public static void BuildShowcaseOnly()
        {
            BuildShowcase();
            RebuildTileColliders(ShowcaseScene);
            KitManifest.Write();
        }

        /// <summary>
        /// Every building block in one long row with a label, to try them out and see how they work.
        /// Players start on the left. Nobody wins here: respawn is on and the points never run out.
        /// </summary>
        static void BuildShowcase()
        {
            LevelScene level = CreateLevelScene("background_color_hills");
            LevelRules rules = level.Rules;
            rules.LevelName = "Alle Bausteine";
            rules.BuiltBy = "Arena-Kit";
            rules.LastPlayerStandingWins = false;
            rules.RespawnPlayers = true;
            rules.PointsToWin = 999;
            rules.MaxPlayers = 1;

            const int right = 169;
            Transform labels = new GameObject("Labels").transform;

            // Floor (top at y = 0) and two high walls so the camera has room.
            Fill(level.Ground, "Grass", -2, -2, right, -1);
            Fill(level.Ground, "Grass", -2, 0, -2, 11);
            Fill(level.Ground, "Grass", right, 0, right, 11);

            PlaceSpawn(level, 1, new Vector3(3.25f, 1.2f, 0f));
            Label(labels, "SpawnPoint", "Hier startest du", 3.25f);

            // --- Weapons ---
            string[] pickups = { "Pickup_Pistol", "Pickup_Shotgun", "Pickup_Minigun", "Pickup_Bazooka", "Pickup_GrenadeLauncher", "Pickup_Random" };
            for (int i = 0; i < pickups.Length; i++)
            {
                float x = 9f + i * 1.5f;
                Place(pickups[i], new Vector3(x, 0.8f, 0f));
                // Staggered so the long names do not run into each other.
                Label(labels, pickups[i].Replace("Pickup_", ""), null, x, i % 2 == 0 ? 2f : 2.6f, 0.55f);
            }
            Label(labels, "Pickups", "Waffen zum Aufheben", 12.75f);

            Place("WeaponCrate", new Vector3(20.5f, 2.5f, 0f));
            Place("WeaponCrate", new Vector3(22f, 0.5f, 0f));
            Label(labels, "WeaponCrate", "Aufschießen: Waffe drin", 21.25f, recipe: "Breakable > Spawner");

            // --- Breakables and explosives ---
            Place("Crate", new Vector3(26.5f, 0.5f, 0f));
            Place("Crate", new Vector3(26.5f, 1.5f, 0f));
            Place("Crate", new Vector3(27.5f, 0.5f, 0f));
            Label(labels, "Crate", "Kaputtbare Kiste", 27f, recipe: "Breakable");

            for (int y = 0; y < 3; y++)
                Place("BrickWall", new Vector3(30.5f, y + 0.5f, 0f));
            Label(labels, "BrickWall", "Hält mehr aus", 30.5f, recipe: "Breakable");

            Place("TNTBlock", new Vector3(34.5f, 0.5f, 0f));
            Label(labels, "TNTBlock", "Draufschießen!", 34.5f, recipe: "Breakable > Explosive");
            Place("Mine", new Vector3(38f, 0f, 0f));
            Label(labels, "Mine", "Drauftreten, runtergehen: Bumm!", 38f, 3.2f, recipe: "TriggerZone (On Exit) > Explosive");

            // --- Movement ---
            Place("JumpPad", new Vector3(42.5f, 0.5f, 0f));
            GameObject booster = Place("JumpPad", new Vector3(45f, 0.5f, 0f));
            booster.name = "JumpPad (rotated)";
            booster.transform.rotation = Quaternion.Euler(0f, 0f, -90f);
            Label(labels, "JumpPad", "Gedreht = Seiten-Booster", 43.75f, recipe: "TriggerZone > ForcePush");

            Place("MovingPlatform", new Vector3(48.5f, 2.5f, 0f));
            Label(labels, "MovingPlatform", "Fährt hin und her", 50.5f, recipe: "Mover");
            Place("Elevator", new Vector3(56.5f, 1f, 0f));
            Label(labels, "Elevator", "Fährt hoch und runter", 56.5f, 7f, recipe: "Mover");

            Place("TeleporterPair", new Vector3(64f, 0.5f, 0f));
            Label(labels, "TeleporterPair", "Rein und auf der anderen Seite raus", 64f, recipe: "TriggerZone > Teleporter");

            // --- Switches and doors (connected with events) ---
            var lever = Place("Lever", new Vector3(71.5f, 0.5f, 0f)).GetComponent<Switch>();
            var leverDoor = Place("Door", new Vector3(73.5f, 0.5f, 0f)).GetComponent<Door>();
            UnityEventTools.AddVoidPersistentListener(lever.OnTurnedOn, leverDoor.Open);
            UnityEventTools.AddVoidPersistentListener(lever.OnTurnedOff, leverDoor.Close);
            Label(labels, "Lever  >  Door", "Hebel öffnet und schließt", 72.5f, recipe: "TriggerZone + HitSensor > Switch");

            var button = Place("Button", new Vector3(77.5f, 0.5f, 0f)).GetComponent<Switch>();
            var buttonDoor = Place("Door", new Vector3(79.5f, 0.5f, 0f)).GetComponent<Door>();
            UnityEventTools.AddVoidPersistentListener(button.OnTurnedOn, buttonDoor.Open);
            UnityEventTools.AddVoidPersistentListener(button.OnTurnedOff, buttonDoor.Close);
            Label(labels, "Button  >  Door", "Offen für 3 Sekunden", 78.5f);

            var timer = Place("Timer", new Vector3(83f, 0.5f, 0f)).GetComponent<Timer>();
            timer.Seconds = 2f;
            timer.Repeat = true;
            var timerDoor = Place("Door", new Vector3(84.5f, 0.5f, 0f)).GetComponent<Door>();
            UnityEventTools.AddVoidPersistentListener(timer.OnFinished, timerDoor.Toggle);
            Label(labels, "Timer  >  Door", "Alle 2 Sekunden", 84f);

            var counter = Place("Counter", new Vector3(91f, 0.5f, 0f)).GetComponent<Counter>();
            counter.Target = 3;
            counter.ResetWhenReached = true;
            for (int i = 0; i < 3; i++)
            {
                GameObject coin = Place("Coin", new Vector3(88.5f + i * 1.5f, 1f, 0f));
                coin.GetComponent<Disappear>().RespawnTime = 5f;
                UnityEventTools.AddVoidPersistentListener(coin.GetComponent<TriggerZone>().OnEnter, counter.Add);
            }
            var counterDoor = Place("Door", new Vector3(94f, 0.5f, 0f)).GetComponent<Door>();
            counterDoor.AutoCloseTime = 5f;
            UnityEventTools.AddVoidPersistentListener(counter.OnReached, counterDoor.Open);
            Label(labels, "Counter  >  Door", "3 Coins öffnen die Tür", 91f);

            GameObject key = Place("Key", new Vector3(98.5f, 1f, 0f));
            key.GetComponent<Disappear>().RespawnTime = 5f;
            for (int y = 0; y < 2; y++)
            {
                var lockBlock = Place("LockBlock", new Vector3(101.5f, y + 0.5f, 0f)).GetComponent<Door>();
                lockBlock.AutoCloseTime = 5f;
                UnityEventTools.AddVoidPersistentListener(key.GetComponent<TriggerZone>().OnEnter, lockBlock.Open);
            }
            Label(labels, "Key  >  LockBlock", "Schlüssel öffnet das Schloss", 100f);

            GameObject zoneObject = Place("TriggerZone", new Vector3(106.5f, 1f, 0f));
            var zone = zoneObject.GetComponent<TriggerZone>();
            zone.Cooldown = 1f;
            var area = new GameObject("Area (only in the showcase)");
            area.transform.SetParent(zoneObject.transform, false);
            area.transform.localScale = new Vector3(2f, 2f, 1f);
            AddSprite(area, GeneratedSprite("Square"), OrderBackDecoration + 1, new Color(1f, 0.9f, 0.3f, 0.3f));
            var spawner = Place("Spawner", new Vector3(106.5f, 5f, 0f)).GetComponent<Spawner>();
            spawner.Prefabs = new[] { PrefabByKey("Crate") };
            spawner.Interval = 0f;
            spawner.SpawnAtStart = false;
            spawner.MaxAlive = 3;
            spawner.RandomOffset = new Vector2(1f, 0f);
            UnityEventTools.AddVoidPersistentListener(zone.OnEnter, spawner.Spawn);
            Label(labels, "TriggerZone  >  Spawner", "Reinlaufen: eine Kiste fällt", 106.5f, 7f);

            // --- Points ---
            Place("Coin", new Vector3(111.5f, 1f, 0f)).GetComponent<Disappear>().RespawnTime = 3f;
            Place("Gem", new Vector3(113f, 1f, 0f)).GetComponent<Disappear>().RespawnTime = 3f;
            Label(labels, "Coin / Gem", "1 und 3 Punkte", 112.25f, recipe: "TriggerZone > Score + Disappear");

            // --- Danger ---
            Place("Spikes", new Vector3(117.5f, 0.5f, 0f));
            Place("Spikes", new Vector3(118.5f, 0.5f, 0f));
            Label(labels, "Spikes", "Tödlich", 118f, recipe: "TriggerZone > Damager");
            Place("Saw", new Vector3(122f, 0.6f, 0f));
            Label(labels, "Saw", "Tödlich und bewegt sich", 123.5f, recipe: "TriggerZone > Damager + Mover");

            for (int x = 128; x <= 130; x++)
                level.Ground.SetTile(new Vector3Int(x, -1, 0), null);
            Fill(level.Hazards, "Lava", 128, -1, 130, -1);
            Label(labels, "Lava", "Tile Palette: 3 Hazards", 129.5f);

            // --- Game modes ---
            Place("ScoreZone", new Vector3(135.5f, 1.5f, 0f));
            Label(labels, "ScoreZone", "Allein drinstehen = Punkte", 135.5f);

            // The camera follows the player through the whole row, not the ball at the far end.
            Place("Ball", new Vector3(140f, 3f, 0f)).GetComponent<Ball>().CameraFollows = false;
            Place("Goal", new Vector3(144.5f, 1.5f, 0f));
            Label(labels, "Ball  +  Goal", "Ball ins Tor = Punkt", 142.25f, recipe: "TriggerZone (Balls) > Score + Remover");

            // Here the flag gives a point and sends you back, instead of winning the round.
            GameObject flag = Place("FinishFlag", new Vector3(149f, 0.5f, 0f));
            TriggerZone flagZone = flag.GetComponent<TriggerZone>();
            var flagScore = flag.GetComponent<Score>();
            for (int i = flagZone.OnEnter.GetPersistentEventCount() - 1; i >= 0; i--)
                if (flagZone.OnEnter.GetPersistentMethodName(i) == nameof(Score.WinRound))
                    UnityEventTools.RemovePersistentListener(flagZone.OnEnter, i);
            UnityEventTools.AddVoidPersistentListener(flagZone.OnEnter, flagScore.AddPoints);
            UnityEventTools.AddVoidPersistentListener(flagZone.OnEnter, flagScore.SendToStart);
            Label(labels, "FinishFlag", "Punkt und zurück zum Start", 149f, recipe: "TriggerZone > Score");

            Place("NameSign", new Vector3(153f, 0.8f, 0f));
            Label(labels, "NameSign", "Zeigt deinen Namen", 153f);

            // Rising lava in its own pit, so it does not flood the whole row.
            const int pitLeft = 159, pitRight = 165;
            for (int x = pitLeft + 1; x < pitRight; x++)
                for (int y = -2; y <= -1; y++)
                    level.Ground.SetTile(new Vector3Int(x, y, 0), null);
            Fill(level.Ground, "Grass", pitLeft, -12, pitLeft, 3);
            Fill(level.Ground, "Grass", pitRight, -12, pitRight, 3);
            Fill(level.Ground, "Grass", pitLeft + 1, -12, pitRight - 1, -12);

            float pitWidth = pitRight - pitLeft - 1;
            GameObject lava = Place("RisingLava", new Vector3((pitLeft + pitRight + 1) * 0.5f, -6.5f, 0f));
            lava.GetComponent<SpriteRenderer>().size = new Vector2(pitWidth, 10f);
            lava.transform.Find("Surface").GetComponent<SpriteRenderer>().size = new Vector2(pitWidth, 1f);
            lava.GetComponent<BoxCollider2D>().size = new Vector2(pitWidth, 11f);
            var lavaMover = lava.GetComponent<Mover>();
            lavaMover.Offset = new Vector2(0f, 4.5f);
            lavaMover.Speed = 1f;
            var lavaLever = Place("Lever", new Vector3(157f, 0.5f, 0f)).GetComponent<Switch>();
            UnityEventTools.AddVoidPersistentListener(lavaLever.OnTurnedOn, lavaMover.MoveToEnd);
            UnityEventTools.AddVoidPersistentListener(lavaLever.OnTurnedOff, lavaMover.MoveToStart);
            Label(labels, "RisingLava", "Steigt nach 30 s, oder mit dem Hebel", 161f, 6f, recipe: "Timer > Mover, TriggerZone > Damager");

            GameObject killZone = Object.FindObjectsByType<TriggerZone>(FindObjectsSortMode.None)
                .First(zone => zone.name == "KillZone").gameObject;
            killZone.transform.position = new Vector3(right * 0.5f, -16f, 0f);
            killZone.transform.localScale = new Vector3(3f, 1f, 1f);

            WarnAboutMissingBlocks();
            Save(level, ShowcaseScene);
        }

        /// <summary>
        /// A name floating above a showcase station, a short German hint below it and the recipe:
        /// which trigger and action scripts the prefab is made of.
        /// </summary>
        static void Label(Transform parent, string title, string hint, float x, float y = 5f, float size = 1f, string recipe = null)
        {
            var root = new GameObject(title);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(x, y, 0f);
            LabelLine(root.transform, title, 0f, 0.12f * size, new Color(0.13f, 0.15f, 0.25f));
            if (!string.IsNullOrEmpty(hint))
                LabelLine(root.transform, hint, -0.65f * size, 0.075f * size, new Color(0.25f, 0.28f, 0.4f));
            if (!string.IsNullOrEmpty(recipe))
                LabelLine(root.transform, recipe, -1.15f * size, 0.06f * size, new Color(0.1f, 0.45f, 0.25f));
        }

        static void LabelLine(Transform parent, string text, float y, float characterSize, Color color)
        {
            var line = new GameObject(text, typeof(TextMesh));
            line.transform.SetParent(parent, false);
            line.transform.localPosition = new Vector3(0f, y, 0f);
            var textMesh = line.GetComponent<TextMesh>();
            var font = AssetDatabase.LoadAssetAtPath<Font>(PlayerFolder + "/ArenaFont.ttf");
            textMesh.font = font;
            textMesh.fontSize = 48;
            textMesh.characterSize = characterSize;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;
            textMesh.text = text;
            var meshRenderer = line.GetComponent<MeshRenderer>();
            if (font != null)
                meshRenderer.sharedMaterial = font.material;
            meshRenderer.sortingOrder = OrderBackDecoration + 2;
        }

        /// <summary>Reminds whoever adds a new building block to put it into the showcase too.</summary>
        static void WarnAboutMissingBlocks()
        {
            var placed = new HashSet<string>(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Select(t => PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject))
                .Where(source => source != null)
                .Select(source => source.name));

            foreach (string folder in new[] { Prefabs + "/Blocks", Prefabs + "/Rules", Prefabs + "/Pickups" })
                foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath))
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    if (name != "LevelRules" && name != "Drop_Random" && !placed.Contains(name))
                        Debug.LogWarning($"[ArenaKit] Showcase: {name} is missing. Add it in KitBuilder.BuildShowcase.");
                }
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

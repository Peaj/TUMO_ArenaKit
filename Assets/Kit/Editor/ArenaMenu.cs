using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArenaKit.EditorTools
{
    /// <summary>
    /// The "TUMO" menu: create your level, export it for the arcade, and tools for the teacher.
    /// </summary>
    public static class ArenaMenu
    {
        public const string LevelsFolder = "Assets/Levels";
        public const string TemplateScene = "Assets/Kit/Scenes/Template.unity";
        public const string PlayerPrefabPath = "Assets/Kit/Resources/ArenaKit/Player.prefab";
        const string DemoPrefix = "Demo_";

        // ---------------------------------------------------------------
        // Students
        // ---------------------------------------------------------------

        [MenuItem("TUMO/Neues Level erstellen...", priority = 0)]
        static void NewLevel()
        {
            NewLevelWindow.Open();
        }

        [MenuItem("TUMO/Mein Level öffnen", priority = 1)]
        static void OpenMyLevel()
        {
            List<string> scenes = FindLevelScenes(includeDemos: false);
            if (scenes.Count == 0)
            {
                EditorUtility.DisplayDialog("Kein Level gefunden", "Du hast noch kein eigenes Level. Erstelle eines mit TUMO > Neues Level erstellen.", "OK");
                return;
            }

            if (scenes.Count == 1)
            {
                OpenScene(scenes[0]);
                return;
            }

            var menu = new GenericMenu();
            foreach (string scene in scenes)
                menu.AddItem(new GUIContent(scene.Substring(LevelsFolder.Length + 1)), false, () => OpenScene(scene));
            menu.ShowAsContext();
        }

        [MenuItem("TUMO/Level exportieren", priority = 2)]
        static void ExportLevel()
        {
            Scene scene = SceneManager.GetActiveScene();
            string folder = GetLevelFolder(scene.path);
            if (folder == null)
            {
                EditorUtility.DisplayDialog("Level exportieren",
                    "Öffne zuerst dein eigenes Level (es liegt im Ordner Assets/Levels/DeinName).", "OK");
                return;
            }

            if (scene.isDirty && !EditorSceneManager.SaveScene(scene))
                return;
            AssetDatabase.SaveAssets();

            string levelName = Path.GetFileName(folder);
            if (!CheckBeforeExport(folder, scene))
                return;

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string packagePath = Path.Combine(desktop, levelName + ".unitypackage");
            AssetDatabase.ExportPackage(folder, packagePath, ExportPackageOptions.Recurse);

            EditorUtility.RevealInFinder(packagePath);
            List<string> levels = AssetDatabase.FindAssets("t:Scene", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).Select(Path.GetFileNameWithoutExtension).ToList();
            EditorUtility.DisplayDialog("Level exportiert",
                $"Fertig! Die Datei \"{levelName}.unitypackage\" liegt auf deinem Schreibtisch.\n\n" +
                (levels.Count > 1 ? $"Darin sind alle {levels.Count} Level aus deinem Ordner: {string.Join(", ", levels)}\n\n" : "") +
                "Gib sie deiner Lehrerin oder deinem Lehrer.", "Super");
        }

        static bool CheckBeforeExport(string folder, Scene scene)
        {
            var rules = UnityEngine.Object.FindAnyObjectByType<LevelRules>();
            if (rules == null)
            {
                if (!EditorUtility.DisplayDialog("Level exportieren", "In deinem Level fehlt das Objekt 'Level Rules'. Ohne kann dein Level nicht gespielt werden.", "Trotzdem exportieren", "Abbrechen"))
                    return false;
            }
            else if (string.IsNullOrWhiteSpace(rules.BuiltBy))
            {
                EditorUtility.DisplayDialog("Level exportieren", "Trage zuerst deinen Namen bei 'Level Rules > Built By' ein, damit alle sehen, wer das Level gebaut hat.", "OK");
                Selection.activeObject = rules;
                return false;
            }

            // Changed kit files would stay behind on this Mac: the level gets its own copies of them.
            KitCopies.Result copies = KitCopies.Adopt(folder);
            if (copies.Any)
            {
                EditorSceneManager.OpenScene(scene.path);
                List<string> files = copies.Copied.Concat(copies.Moved).Select(Path.GetFileName).ToList();
                EditorUtility.DisplayDialog("Kit-Dateien kopiert",
                    "Du hast Dateien aus dem Kit verändert, die dein Level benutzt. Damit deine Änderungen mitkommen, " +
                    $"liegen sie jetzt als Kopie in deinem Ordner ({KitCopies.CopiesFolderName}), und dein Level benutzt die Kopien:\n\n" +
                    ShortList(files) + "\n\nWillst du daran weiterarbeiten? Dann ändere ab jetzt die Kopien, nicht die Originale im Kit.", "OK");
            }

            if (copies.Scripts.Count > 0)
            {
                if (!EditorUtility.DisplayDialog("Achtung: Kit-Scripts verändert",
                        "Du hast Scripts im Kit verändert oder dort angelegt. Diese Scripts kommen NICHT mit in den Export:\n\n" + ShortList(copies.Scripts) +
                        "\n\nTipp: Eigene Scripts gehören in deinen Ordner und fangen mit deinem Namen an.",
                        "Trotzdem exportieren", "Abbrechen"))
                    return false;
            }

            // Files the level uses that are neither in the kit nor in the own folder.
            var outside = new List<string>();
            string[] ownAssets = AssetDatabase.FindAssets("", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            foreach (string dependency in AssetDatabase.GetDependencies(ownAssets, true))
            {
                if (dependency.StartsWith(folder + "/") || dependency.StartsWith(KitManifest.KitFolder + "/") || dependency.StartsWith("Packages/"))
                    continue;
                if (dependency.StartsWith("ProjectSettings/") || dependency.EndsWith(".cs") && dependency.StartsWith("Assets/Kit"))
                    continue;
                outside.Add(dependency);
            }

            if (outside.Count > 0)
            {
                if (!EditorUtility.DisplayDialog("Achtung: Dateien außerhalb deines Ordners",
                        "Dein Level benutzt Dateien, die nicht in deinem Ordner liegen. Sie fehlen im Export:\n\n" + string.Join("\n", outside.Take(12)) +
                        "\n\nTipp: Verschiebe sie in deinen Ordner " + folder,
                        "Trotzdem exportieren", "Abbrechen"))
                    return false;
            }

            // Own scripts need unique class names, otherwise the arcade does not compile.
            string prefix = Path.GetFileName(folder);
            var badScripts = new List<string>();
            foreach (string script in ownAssets.Where(p => p.EndsWith(".cs")))
            {
                string className = Path.GetFileNameWithoutExtension(script);
                if (!className.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    badScripts.Add(className);
            }

            if (badScripts.Count > 0)
            {
                if (!EditorUtility.DisplayDialog("Achtung: Script-Namen",
                        $"Deine Scripts sollten mit deinem Namen anfangen (z.B. {prefix}Trampolin), sonst gibt es Streit mit gleichnamigen Scripts von anderen:\n\n" + string.Join("\n", badScripts),
                        "Trotzdem exportieren", "Abbrechen"))
                    return false;
            }

            return true;
        }

        static string ShortList(List<string> items)
        {
            return string.Join("\n", items.Take(12)) + (items.Count > 12 ? $"\n... und {items.Count - 12} weitere" : "");
        }

        // ---------------------------------------------------------------
        // Teacher
        // ---------------------------------------------------------------

        [MenuItem("TUMO/Lehrer/Level-Pakete importieren...", priority = 100)]
        static void ImportLevelPackages()
        {
            string folder = EditorUtility.OpenFolderPanel("Ordner mit den .unitypackage Dateien der Schüler", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "");
            if (string.IsNullOrEmpty(folder))
                return;

            string[] packages = Directory.GetFiles(folder, "*.unitypackage");
            if (packages.Length == 0)
            {
                EditorUtility.DisplayDialog("Import", "In diesem Ordner liegen keine .unitypackage Dateien.", "OK");
                return;
            }

            foreach (string package in packages)
                AssetDatabase.ImportPackage(package, false);

            EditorUtility.DisplayDialog("Import",
                $"{packages.Length} Pakete werden importiert.\n\nWenn Unity fertig ist: TUMO > Lehrer > Arcade vorbereiten", "OK");
        }

        [MenuItem("TUMO/Lehrer/Arcade vorbereiten", priority = 101)]
        static void PrepareArcade()
        {
            bool includeDemos = EditorUtility.DisplayDialog("Arcade vorbereiten", "Sollen die Demo-Level auch mitgespielt werden?", "Ja", "Nein");
            List<string> scenes = FindLevelScenes(includeDemos);
            if (scenes.Count == 0)
            {
                EditorUtility.DisplayDialog("Arcade vorbereiten", "Keine Level in Assets/Levels gefunden.", "OK");
                return;
            }

            SetBuildScenes(scenes);
            OpenScene(scenes[0]);
            EditorUtility.DisplayDialog("Arcade vorbereiten",
                $"{scenes.Count} Level sind in der Build-Liste:\n\n" + string.Join("\n", scenes.Select(Path.GetFileNameWithoutExtension)) +
                "\n\nDrücke Play. Nach jeder Runde kommt das nächste Level.\nN = Level überspringen, K = Tastatur-Spieler, 0 = Punkte zurücksetzen", "Los geht's");
        }

        [MenuItem("TUMO/Lehrer/Kit-Prüfsumme speichern", priority = 110)]
        static void SaveKitManifest()
        {
            KitManifest.Write();
            EditorUtility.DisplayDialog("Kit-Prüfsumme", "Gespeichert. Beim Exportieren werden Änderungen am Kit ab jetzt gemeldet.", "OK");
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        public static List<string> FindLevelScenes(bool includeDemos)
        {
            if (!AssetDatabase.IsValidFolder(LevelsFolder))
                return new List<string>();

            return AssetDatabase.FindAssets("t:Scene", new[] { LevelsFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => includeDemos || !Path.GetFileName(Path.GetDirectoryName(path)).StartsWith(DemoPrefix))
                .OrderBy(path => Path.GetFileName(Path.GetDirectoryName(path)).StartsWith(DemoPrefix) ? 1 : 0)
                .ThenBy(path => path)
                .ToList();
        }

        public static void SetBuildScenes(IEnumerable<string> scenePaths)
        {
            EditorBuildSettings.scenes = scenePaths.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
        }

        /// <summary>Assets/Levels/Name/... -> Assets/Levels/Name, null for scenes outside of a level folder.</summary>
        public static string GetLevelFolder(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith(LevelsFolder + "/"))
                return null;

            string rest = assetPath.Substring(LevelsFolder.Length + 1);
            int slash = rest.IndexOf('/');
            return slash > 0 ? LevelsFolder + "/" + rest.Substring(0, slash) : null;
        }

        [MenuItem("TUMO/Alle Bausteine ansehen", priority = 20)]
        static void OpenShowcase()
        {
            OpenScene(KitBuilder.ShowcaseScene);
        }

        [MenuItem("TUMO/Spickzettel öffnen", priority = 21)]
        static void OpenCheatSheet()
        {
            OpenDoc("Spickzettel.html");
        }

        [MenuItem("TUMO/Workshop-Material öffnen", priority = 22)]
        static void OpenMaterial()
        {
            OpenDoc("index.html");
        }

        /// <summary>Opens one of the workshop pages in Docs/ (next to Assets) in the browser.</summary>
        static void OpenDoc(string file)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", file));
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("Nicht gefunden", $"Die Datei {path} gibt es nicht.", "OK");
                return;
            }
            Application.OpenURL(new Uri(path).AbsoluteUri);
        }

        static void OpenScene(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(path);
        }

        /// <summary>"Lena Müller" -> "LenaMueller": safe for folders and class names.</summary>
        public static string ToSafeName(string name)
        {
            string safe = name.Trim()
                .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss")
                .Replace("Ä", "Ae").Replace("Ö", "Oe").Replace("Ü", "Ue");
            safe = Regex.Replace(safe, @"[^A-Za-z0-9]", "");
            if (safe.Length > 0 && char.IsDigit(safe[0]))
                safe = "Level" + safe;
            return safe;
        }

        /// <summary>
        /// Creates the level folder, the scene and a player variant for one student.
        /// If the folder exists already, another level is added to it: export and arcade take all levels of a folder.
        /// </summary>
        public static string CreateLevel(string studentName, string levelTitle)
        {
            string safeName = ToSafeName(studentName);
            string folder = LevelsFolder + "/" + safeName;

            string scenePath;
            if (AssetDatabase.IsValidFolder(folder))
            {
                string suffix = ToSafeName(levelTitle ?? "");
                scenePath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{safeName}{(suffix.Length > 0 ? "_" + suffix : "2")}.unity");
            }
            else
            {
                if (!AssetDatabase.IsValidFolder(LevelsFolder))
                    AssetDatabase.CreateFolder("Assets", "Levels");
                AssetDatabase.CreateFolder(LevelsFolder, safeName);
                scenePath = $"{folder}/{safeName}.unity";
            }

            if (!AssetDatabase.CopyAsset(TemplateScene, scenePath))
                throw new InvalidOperationException("Die Vorlage " + TemplateScene + " fehlt.");

            // A player variant, so speed, jump etc. can be changed without touching the kit. A second level shares it.
            string variantPath = $"{folder}/{safeName}Player.prefab";
            var basePlayer = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (basePlayer != null && AssetDatabase.LoadAssetAtPath<GameObject>(variantPath) == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePlayer);
                PrefabUtility.SaveAsPrefabAsset(instance, variantPath);
                UnityEngine.Object.DestroyImmediate(instance);
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath);
            var rules = UnityEngine.Object.FindAnyObjectByType<LevelRules>();
            if (rules != null)
            {
                rules.BuiltBy = studentName.Trim();
                rules.LevelName = string.IsNullOrWhiteSpace(levelTitle) ? $"Arena von {studentName.Trim()}" : levelTitle.Trim();
                rules.PlayerPrefab = AssetDatabase.LoadAssetAtPath<Character>(variantPath);
                EditorUtility.SetDirty(rules);
            }

            foreach (NameSign sign in UnityEngine.Object.FindObjectsByType<NameSign>(FindObjectsSortMode.None))
                sign.Refresh();

            EditorSceneManager.SaveScene(scene);

            // Only this level in the build list: pressing Play restarts it after every round.
            SetBuildScenes(new[] { scenePath });
            return scenePath;
        }
    }

    /// <summary>Small window that asks for the name before creating a level.</summary>
    public class NewLevelWindow : EditorWindow
    {
        string studentName = "";
        string levelTitle = "";

        public static void Open()
        {
            var window = GetWindow<NewLevelWindow>(true, "Neues Level", true);
            window.minSize = window.maxSize = new Vector2(420f, 190f);
            window.ShowUtility();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Erstellt deinen eigenen Level-Ordner mit Szene und Spieler.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(8f);

            this.studentName = EditorGUILayout.TextField("Dein Vorname", this.studentName);
            this.levelTitle = EditorGUILayout.TextField("Name des Levels", this.levelTitle);

            string safeName = ArenaMenu.ToSafeName(this.studentName);
            EditorGUILayout.Space(4f);
            string folder = $"{ArenaMenu.LevelsFolder}/{safeName}";
            string hint = safeName.Length == 0 ? " "
                : AssetDatabase.IsValidFolder(folder) ? $"Den Ordner {folder} gibt es schon: das neue Level kommt dazu."
                : $"Ordner: {folder}";
            EditorGUILayout.LabelField(hint, EditorStyles.miniLabel);

            EditorGUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(safeName.Length == 0))
            {
                if (GUILayout.Button("Level erstellen", GUILayout.Height(32f)))
                {
                    try
                    {
                        string scene = ArenaMenu.CreateLevel(this.studentName, this.levelTitle);
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene));
                        Close();
                    }
                    catch (Exception exception)
                    {
                        EditorUtility.DisplayDialog("Neues Level", exception.Message, "OK");
                    }
                }
            }
        }
    }
}

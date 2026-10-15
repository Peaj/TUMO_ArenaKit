using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace ArenaKit.EditorTools
{
    /// <summary>
    /// Makes a level folder self-contained before it is exported. Teens often change kit prefabs instead of
    /// making a variant, and those changes would stay behind on their Mac. The level gets copies of the changed
    /// kit files in its own folder (with new GUIDs, so they never collide with the kit) and uses the copies.
    /// Files a Teen created inside Assets/Kit are moved into the level folder.
    /// </summary>
    public static class KitCopies
    {
        public const string CopiesFolderName = "Kit-Kopien";

        static readonly Regex GuidReference = new Regex(@"guid: ([0-9a-f]{32})");

        public class Result
        {
            /// <summary>Kit paths of the files that were copied or moved into the level folder.</summary>
            public readonly List<string> Copied = new List<string>();
            public readonly List<string> Moved = new List<string>();
            /// <summary>Changed or new kit scripts the level uses. Scripts can't be copied (duplicate class names).</summary>
            public readonly List<string> Scripts = new List<string>();
            public bool Any => this.Copied.Count + this.Moved.Count > 0;
        }

        public static Result Adopt(string folder)
        {
            var result = new Result();
            string copiesFolder = folder + "/" + CopiesFolderName;
            var changed = new HashSet<string>(KitManifest.FindChangedFiles());
            var added = new HashSet<string>(KitManifest.FindNewFiles());
            if (changed.Count == 0 && added.Count == 0)
                return result;

            // 1. Teen files inside the kit: moving keeps their GUID, so every reference stays intact.
            foreach (string path in FindUsedKitFiles(folder, out _).Where(added.Contains))
            {
                if (IsCode(path))
                {
                    result.Scripts.Add(path);
                    continue;
                }

                EnsureFolder(copiesFolder);
                string target = AssetDatabase.GenerateUniqueAssetPath(copiesFolder + "/" + Path.GetFileName(path));
                if (string.IsNullOrEmpty(AssetDatabase.MoveAsset(path, target)))
                    result.Moved.Add(path);
            }

            // 2. Changed kit files, plus every kit file between the level and a changed file
            //    (e.g. the kit pickup that holds a changed weapon), get copied.
            List<string> used = FindUsedKitFiles(folder, out Dictionary<string, string[]> dependencies);
            var affected = new Dictionary<string, bool>();
            bool IsAffected(string path)
            {
                if (affected.TryGetValue(path, out bool known))
                    return known;
                affected[path] = false; // guards against cycles
                bool value = changed.Contains(path) && !IsCode(path) || dependencies[path].Any(IsAffected);
                affected[path] = value;
                return value;
            }

            result.Scripts.AddRange(used.Where(path => IsCode(path) && changed.Contains(path)));

            var guidMap = new Dictionary<string, string>();
            foreach (string path in used.Where(path => !IsCode(path) && IsAffected(path)))
            {
                EnsureFolder(copiesFolder);
                string target = AssetDatabase.GenerateUniqueAssetPath(copiesFolder + "/" + Path.GetFileName(path));
                if (!AssetDatabase.CopyAsset(path, target))
                    continue;
                guidMap[AssetDatabase.AssetPathToGUID(path)] = AssetDatabase.AssetPathToGUID(target);
                result.Copied.Add(path);
            }

            if (guidMap.Count > 0)
                Remap(folder, guidMap);
            return result;
        }

        /// <summary>All kit files the level folder uses, directly or through other kit files.</summary>
        static List<string> FindUsedKitFiles(string folder, out Dictionary<string, string[]> dependencies)
        {
            dependencies = new Dictionary<string, string[]>();
            var used = new List<string>();
            var queue = new Queue<string>(AssetDatabase.FindAssets("", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !AssetDatabase.IsValidFolder(path))
                .Distinct());
            var seen = new HashSet<string>(queue);

            while (queue.Count > 0)
            {
                string path = queue.Dequeue();
                string[] kitDependencies = AssetDatabase.GetDependencies(path, false)
                    .Where(dependency => dependency != path && dependency.StartsWith(KitManifest.KitFolder + "/"))
                    .ToArray();
                if (path.StartsWith(KitManifest.KitFolder + "/"))
                {
                    dependencies[path] = kitDependencies;
                    used.Add(path);
                }

                foreach (string dependency in kitDependencies)
                    if (seen.Add(dependency))
                        queue.Enqueue(dependency);
            }
            return used;
        }

        /// <summary>Points every reference in the level folder (scenes, prefabs, copies) from the kit files to the copies.</summary>
        static void Remap(string folder, Dictionary<string, string> guidMap)
        {
            var changedFiles = new List<string>();
            foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta") || !IsYaml(file))
                    continue;

                string text = File.ReadAllText(file);
                string remapped = GuidReference.Replace(text, match =>
                    guidMap.TryGetValue(match.Groups[1].Value, out string copy) ? "guid: " + copy : match.Value);
                if (remapped == text)
                    continue;

                File.WriteAllText(file, remapped, new UTF8Encoding(false));
                changedFiles.Add(file.Replace('\\', '/'));
            }

            foreach (string file in changedFiles)
                AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceUpdate);
        }

        static bool IsYaml(string file)
        {
            using var stream = File.OpenRead(file);
            var header = new byte[5];
            return stream.Read(header, 0, 5) == 5 && Encoding.ASCII.GetString(header) == "%YAML";
        }

        static bool IsCode(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            return extension == ".cs" || extension == ".asmdef" || extension == ".asmref" || extension == ".dll" || extension == ".shader";
        }

        static void EnsureFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(folder).Replace('\\', '/'), Path.GetFileName(folder));
        }
    }
}

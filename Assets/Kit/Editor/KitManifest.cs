using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ArenaKit.EditorTools
{
    /// <summary>
    /// Remembers a checksum of every kit file. Before exporting a level we compare against it,
    /// because changes to kit files would not be part of the exported level.
    /// </summary>
    public static class KitManifest
    {
        public const string KitFolder = "Assets/Kit";
        const string ManifestPath = "ProjectSettings/ArenaKitManifest.txt";

        public static bool Exists => File.Exists(ManifestPath);

        public static void Write()
        {
            var builder = new StringBuilder();
            foreach (KeyValuePair<string, string> entry in ComputeHashes())
                builder.Append(entry.Key).Append('\t').Append(entry.Value).Append('\n');
            File.WriteAllText(ManifestPath, builder.ToString());
        }

        /// <summary>Kit files that were changed, added or deleted since the manifest was written.</summary>
        public static List<string> FindChanges()
        {
            var changes = new List<string>();
            if (!Exists)
                return changes;

            var expected = new Dictionary<string, string>();
            foreach (string line in File.ReadAllLines(ManifestPath))
            {
                int tab = line.IndexOf('\t');
                if (tab > 0)
                    expected[line.Substring(0, tab)] = line.Substring(tab + 1);
            }

            SortedDictionary<string, string> current = ComputeHashes();
            foreach (KeyValuePair<string, string> entry in current)
            {
                if (!expected.TryGetValue(entry.Key, out string hash))
                    changes.Add(entry.Key + "  (neu)");
                else if (hash != entry.Value)
                    changes.Add(entry.Key + "  (geändert)");
            }

            foreach (string path in expected.Keys)
                if (!current.ContainsKey(path))
                    changes.Add(path + "  (gelöscht)");

            changes.Sort();
            return changes;
        }

        static SortedDictionary<string, string> ComputeHashes()
        {
            var hashes = new SortedDictionary<string, string>();
            using MD5 md5 = MD5.Create();
            foreach (string file in Directory.GetFiles(KitFolder, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta"))
                    continue;

                string path = file.Replace('\\', '/');

                // Ignore line endings: git on Windows and on Mac may check files out differently.
                byte[] content = File.ReadAllBytes(file).Where(b => b != (byte)'\r').ToArray();
                byte[] hash = md5.ComputeHash(content);
                hashes[path] = System.BitConverter.ToString(hash).Replace("-", "");
            }
            return hashes;
        }
    }
}

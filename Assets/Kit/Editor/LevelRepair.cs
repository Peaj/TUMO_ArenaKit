using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace ArenaKit.EditorTools
{
    /// <summary>
    /// TUMO > Level reparieren: puts back what every level needs and undoes the most common accidents
    /// (deleted camera rig, scaled or rotated tilemaps...). Everything the level was built from stays.
    /// </summary>
    public static class LevelRepair
    {
        const string CameraRigPrefab = "Assets/Kit/Prefabs/Rules/CameraRig.prefab";
        const string KillZonePrefab = "Assets/Kit/Prefabs/Rules/KillZone.prefab";
        const string UndoName = "Level reparieren";

        [MenuItem("TUMO/Level reparieren", priority = 3)]
        static void RepairFromMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(UndoName, "Bitte zuerst Play beenden.", "OK");
                return;
            }

            List<string> fixes = Repair(SceneManager.GetActiveScene(), out List<string> warnings);
            string text = fixes.Count == 0
                ? "Alles in Ordnung, es war nichts kaputt."
                : "Repariert:\n• " + string.Join("\n• ", fixes) + "\n\nSpeichern nicht vergessen (Cmd+S)!";
            if (warnings.Count > 0)
                text += "\n\nBitte selbst prüfen:\n• " + string.Join("\n• ", warnings);
            EditorUtility.DisplayDialog(UndoName, text, "OK");
        }

        /// <summary>Repairs the scene and returns what was fixed. Can be undone with Cmd+Z.</summary>
        public static List<string> Repair(Scene scene, out List<string> warnings)
        {
            var fixes = new List<string>();
            warnings = new List<string>();
            List<GameObject> all = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(t => t.gameObject).ToList();
            T Find<T>() where T : Component => all.Select(go => go.GetComponent<T>()).FirstOrDefault(c => c != null);

            // The camera ("Why is my game black?").
            if (Find<ArenaCamera>() == null)
            {
                Place(scene, CameraRigPrefab, Vector3.zero);
                fixes.Add("Die Kamera (CameraRig) war weg und ist wieder da.");
            }

            if (Find<LevelRules>() == null)
            {
                var rules = new GameObject("Level Rules");
                SceneManager.MoveGameObjectToScene(rules, scene);
                rules.AddComponent<LevelRules>();
                Undo.RegisterCreatedObjectUndo(rules, UndoName);
                fixes.Add("Level Rules war weg und ist wieder da (Einstellungen neu machen!).");
            }

            // Tilemaps that were moved, rotated, scaled or skewed.
            foreach (Grid grid in all.Select(go => go.GetComponent<Grid>()).Where(g => g != null))
            {
                bool changed = ResetTransform(grid.transform);
                if (grid.cellSize != Vector3.right + Vector3.up || grid.cellGap != Vector3.zero
                    || grid.cellLayout != GridLayout.CellLayout.Rectangle || grid.cellSwizzle != GridLayout.CellSwizzle.XYZ)
                {
                    Undo.RecordObject(grid, UndoName);
                    grid.cellSize = new Vector3(1f, 1f, 0f);
                    grid.cellGap = Vector3.zero;
                    grid.cellLayout = GridLayout.CellLayout.Rectangle;
                    grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
                    changed = true;
                }
                foreach (Tilemap tilemap in grid.GetComponentsInChildren<Tilemap>(true))
                {
                    changed |= ResetTransform(tilemap.transform);
                    if (tilemap.tileAnchor != new Vector3(0.5f, 0.5f, 0f) || tilemap.orientation != Tilemap.Orientation.XY)
                    {
                        Undo.RecordObject(tilemap, UndoName);
                        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
                        tilemap.orientation = Tilemap.Orientation.XY;
                        changed = true;
                    }
                }
                if (changed)
                    fixes.Add($"Das Raster \"{grid.name}\" war verschoben, gedreht oder verzerrt und ist wieder gerade.");
            }

            // Without a 2D light everything is dark.
            if (!all.Select(go => go.GetComponent<Light2D>()).Any(l => l != null && l.lightType == Light2D.LightType.Global && l.enabled))
            {
                var lightObject = new GameObject("Global Light 2D");
                SceneManager.MoveGameObjectToScene(lightObject, scene);
                var light = lightObject.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Global;
                light.intensity = 1f;
                light.color = Color.white;
                light.targetSortingLayers = SortingLayer.layers.Select(layer => layer.id).ToArray();
                Undo.RegisterCreatedObjectUndo(lightObject, UndoName);
                fixes.Add("Das Licht (Global Light 2D) war weg und ist wieder da.");
            }

            // The pit below the level.
            if (!all.Any(go => go.name.StartsWith("KillZone")))
            {
                ArenaCamera rig = Find<ArenaCamera>();
                Rect level = rig != null ? rig.LevelRect : new Rect(-16f, -9f, 32f, 18f);
                GameObject killZone = Place(scene, KillZonePrefab, new Vector3(level.center.x, level.yMin - 4f, 0f));
                if (killZone != null)
                    killZone.transform.localScale = new Vector3(Mathf.Max(1f, level.width / 50f), 1f, 1f);
                fixes.Add("Die KillZone unter dem Level war weg und ist wieder da.");
            }

            if (Find<SpawnPoint>() == null)
                warnings.Add("Es gibt keinen SpawnPoint. Zieh welche aus Assets/Kit/Prefabs/Rules ins Level.");

            int missing = all.Count(go => PrefabUtility.IsPrefabAssetMissing(go) && PrefabUtility.IsOutermostPrefabInstanceRoot(go));
            if (missing > 0)
                warnings.Add($"{missing} Baustein(e) zeigen \"Missing Prefab\": Kit-Dateien fehlen. Lehrkraft: Kit aktualisieren.");

            if (all.Count(go => go.GetComponent<ArenaCamera>() != null) > 1)
                warnings.Add("Es gibt mehr als eine Kamera (CameraRig). Bitte eine löschen.");

            if (fixes.Count > 0)
                EditorSceneManager.MarkSceneDirty(scene);
            return fixes;
        }

        static bool ResetTransform(Transform transform)
        {
            if (transform.localPosition == Vector3.zero && transform.localRotation == Quaternion.identity && transform.localScale == Vector3.one)
                return false;
            Undo.RecordObject(transform, UndoName);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            return true;
        }

        static GameObject Place(Scene scene, string prefabPath, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return null;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.position = position;
            Undo.RegisterCreatedObjectUndo(instance, UndoName);
            return instance;
        }
    }
}

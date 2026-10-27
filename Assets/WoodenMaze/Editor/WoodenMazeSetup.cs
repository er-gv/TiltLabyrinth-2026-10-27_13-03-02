using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WoodenMaze.EditorTools
{
    /// <summary>Menu: Wooden Maze > Create Game Scene. Builds and saves a ready-to-play scene.</summary>
    public static class WoodenMazeSetup
    {
        const string Root = "Assets/WoodenMaze";
        const string MaterialsDir = Root + "/Materials";
        const string ScenesDir = Root + "/Scenes";
        const string ScenePath = ScenesDir + "/WoodenMaze.unity";

        [MenuItem("Wooden Maze/Create Game Scene", priority = 0)]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(MaterialsDir);
            EnsureFolder(ScenesDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 13f, -9.5f);
            camGo.transform.LookAt(new Vector3(0f, 0f, -0.4f));

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.96f, 0.9f);
            lightGo.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            var game = new GameObject("Wooden Maze Game").AddComponent<MazeGame>();
            // Material assets in the project make sure the right shaders end up in builds.
            game.woodMaterial = MaterialAsset("Maze Wood", Color.white, 0f, 0f, false);
            game.boxMaterial = MaterialAsset("Box Wood", Color.white, 0f, 0f, false);
            game.floorMaterial = MaterialAsset("Maze Floor", Color.white, 0f, 0f, false);
            game.holeMaterial = MaterialAsset("Hole Inside", new Color(0.04f, 0.035f, 0.03f), 0f, 0f, false);
            game.marbleMaterial = MaterialAsset("Silver Marble", new Color(0.86f, 0.87f, 0.9f), 1f, 0.92f, true);
            game.goalMaterial = MaterialAsset("Goal", new Color(1f, 0.72f, 0.18f), 0f, 0.3f, true, emissive: true);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = game.gameObject;
            Debug.Log("Wooden Maze scene created at " + ScenePath + ". Press Play and use the arrow keys.");
        }

        static Material MaterialAsset(string name, Color color, float metallic, float smoothness, bool specular,
            bool emissive = false)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = MazeMaterials.Create(name);
                AssetDatabase.CreateAsset(mat, path);
            }
            MazeMaterials.Configure(mat, color, metallic, smoothness, null, specular);
            if (emissive) MazeMaterials.SetEmission(mat, color * 0.6f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}

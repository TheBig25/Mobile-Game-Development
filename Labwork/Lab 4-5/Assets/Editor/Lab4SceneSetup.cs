using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CMP5402.Week4.Editor
{
    // Must stay in an Editor folder. It is excluded from device builds.
    public static class Lab4SceneSetup
    {
        [MenuItem("CMP5402/Week4/Create Profiling Sandbox")]
        public static void CreateSandbox()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            bool scriptedPipeline = GraphicsSettings.currentRenderPipeline != null;
            Shader shader = Shader.Find(scriptedPipeline ? "Universal Render Pipeline/Unlit" : "Unlit/Color");
            if (shader == null)
            {
                EditorUtility.DisplayDialog("Lab 4 setup", "Use a Universal 3D (URP) or Built-in 3D project. The required unlit shader was not found.", "OK");
                return;
            }
            EnsureFolder("Assets/CMP5402/Week4/Generated");
            var material = new Material(shader) { name = "Lab4Geometry" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.25f, 0.8f, 0.5f));
            if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(0.25f, 0.8f, 0.5f));
            string materialPath = AssetDatabase.GenerateUniqueAssetPath("Assets/CMP5402/Week4/Generated/Lab4Geometry.mat");
            AssetDatabase.CreateAsset(material, materialPath);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var root = new GameObject("Lab4Sandbox");
            var sandbox = root.AddComponent<Lab4Sandbox>();
            var settings = new SerializedObject(sandbox);
            settings.FindProperty("geometryMaterial").objectReferenceValue = material;
            settings.FindProperty("sandboxCamera").objectReferenceValue = cameraObject.GetComponent<Camera>();
            settings.ApplyModifiedPropertiesWithoutUndo();
            string scenePath = AssetDatabase.GenerateUniqueAssetPath("Assets/CMP5402/Week4/Generated/Lab4Sandbox.unity");
            EditorSceneManager.SaveScene(scene, scenePath);
            Selection.activeGameObject = root;
            AssetDatabase.SaveAssets();
            Debug.Log("Lab4 sandbox created: " + scenePath + ". Add this scene to the active mobile Build Profile.");
        }

        static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(parent, parts[i]);
                parent = next;
            }
        }
    }
}

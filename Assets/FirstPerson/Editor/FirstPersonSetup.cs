using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FirstPersonSetup
{
    [MenuItem("Tools/Dungeon RPG/Create First Person Demo")]
    public static void CreateDemo()
    {
        const string scenePath = "Assets/FirstPerson/Scenes/FirstPersonDemo.unity";
        const string prefabPath = "Assets/FirstPerson/Prefabs/FirstPersonPlayer.prefab";
        if (File.Exists(scenePath) || File.Exists(prefabPath))
        {
            Debug.LogWarning("First-person demo already exists. Existing files were preserved.");
            return;
        }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Assets/FirstPerson/Scenes");
        Directory.CreateDirectory("Assets/FirstPerson/Prefabs");
        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject player = new GameObject("FirstPersonPlayer");
        CharacterController capsule = player.AddComponent<CharacterController>();
        capsule.height = 1.8f;
        capsule.radius = 0.3f;
        capsule.center = new Vector3(0f, 0.9f, 0f);
        capsule.stepOffset = 0.3f;
        player.transform.position = new Vector3(0f, 0.1f, -4f);
        GameObject view = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        view.tag = "MainCamera";
        view.transform.SetParent(player.transform, false);
        view.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        Camera camera = view.GetComponent<Camera>();
        camera.nearClipPlane = 0.01f;
        camera.fieldOfView = 75f;
        GameObject arms = new GameObject("ArmsRoot");
        arms.transform.SetParent(view.transform, false);
        FirstPersonController movement = player.AddComponent<FirstPersonController>();
        SerializedObject serialized = new SerializedObject(movement);
        serialized.FindProperty("playerCamera").objectReferenceValue = view.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAssetAndConnect(player, prefabPath, InteractionMode.AutomatedAction);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Ground";
        floor.transform.position = new Vector3(0f, -0.25f, 0f);
        floor.transform.localScale = new Vector3(24f, 0.5f, 24f);
        for (int i = 0; i < 3; i++)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "Jump Block " + (i + 1);
            obstacle.transform.position = new Vector3(-3f + i * 3f, 0.4f, 2f);
            obstacle.transform.localScale = new Vector3(1.5f, 0.8f, 1.5f);
        }
        GameObject sun = new GameObject("Directional Light", typeof(Light));
        sun.GetComponent<Light>().type = LightType.Directional;
        sun.GetComponent<Light>().intensity = 1.5f;
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.5f);
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("FIRST_PERSON_SETUP_OK: " + scenePath);
    }
}

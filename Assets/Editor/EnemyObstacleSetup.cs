using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EnemyObstacleSetup
{
    [MenuItem("Tools/Dungeon RPG/Add Obstacle Course and Bake")]
    public static void AddCourse()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (Object.FindAnyObjectByType<FirstPersonController>() == null)
        {
            Debug.LogWarning("Open your player scene before adding the obstacle course.");
            return;
        }
        GameObject root = GameObject.Find("Enemy Obstacle Course");
        if (root == null)
        {
            root = new GameObject("Enemy Obstacle Course");
            Undo.RegisterCreatedObjectUndo(root, "Create enemy obstacle course");
            Material material = GetWallMaterial();
            AddWall(root.transform, material, "Centre Divider", new Vector3(1.5f, 1.25f, 1.5f), new Vector3(0.6f, 2.5f, 2f));
            AddWall(root.transform, material, "West Wall", new Vector3(-3f, 1.25f, 2f), new Vector3(3f, 2.5f, 0.6f));
            AddWall(root.transform, material, "South Crate", new Vector3(4f, 0.75f, -3f), new Vector3(2f, 1.5f, 2f));
            AddWall(root.transform, material, "Southwest Wall", new Vector3(-3f, 1.25f, -3f), new Vector3(0.6f, 2.5f, 3f));
        }
        Physics.SyncTransforms();
        EnemySetup.AddEnemy();
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("OBSTACLE_COURSE_OK: Four solid obstacles added and navigation baked. Save the scene, then press Play.");
    }

    private static void AddWall(Transform parent, Material material, string name, Vector3 position, Vector3 size)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        Undo.RegisterCreatedObjectUndo(wall, "Create obstacle");
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = position;
        wall.transform.localScale = size;
        wall.GetComponent<Renderer>().sharedMaterial = material;
        NavMeshModifier modifier = Undo.AddComponent<NavMeshModifier>(wall);
        modifier.overrideArea = true;
        modifier.area = 1; // Not Walkable: wall tops are not destinations.
    }

    private static Material GetWallMaterial()
    {
        const string path = "Assets/Art/Materials/ObstacleWall.mat";
        Directory.CreateDirectory("Assets/Art/Materials");
        AssetDatabase.Refresh();
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        material = new Material(shader) { color = new Color(0.25f, 0.31f, 0.36f) };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}

using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class EnemySetup
{
    [MenuItem("Tools/Dungeon RPG/Add Capsule Enemy")]
    public static void AddEnemy()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
        if (player == null)
        {
            EditorUtility.DisplayDialog("Capsule Enemy", "Add the FirstPersonController to your Player first.", "OK");
            return;
        }
        // Keep the player's body and hands out of the walkable geometry bake.
        NavMeshModifier modifier = player.GetComponent<NavMeshModifier>();
        if (modifier == null) modifier = Undo.AddComponent<NavMeshModifier>(player.gameObject);
        Undo.RecordObject(modifier, "Exclude player from navigation bake");
        modifier.ignoreFromBuild = true;

        NavMeshSurface surface = Object.FindAnyObjectByType<NavMeshSurface>();
        if (surface == null)
        {
            GameObject navigation = new GameObject("Navigation");
            Undo.RegisterCreatedObjectUndo(navigation, "Create navigation");
            surface = Undo.AddComponent<NavMeshSurface>(navigation);
        }
        Undo.RecordObject(surface, "Configure navigation");
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        // Finer bake resolution preserves clearance around corners and narrow passages.
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.08f;

        EnemyChase enemy = Object.FindAnyObjectByType<EnemyChase>();
        if (enemy == null)
        {
            GameObject root = new GameObject("Capsule Enemy");
            Undo.RegisterCreatedObjectUndo(root, "Create capsule enemy");
            root.transform.position = player.transform.position + new Vector3(3f, 0f, 3f);
            NavMeshAgent agent = Undo.AddComponent<NavMeshAgent>(root);
            agent.agentTypeID = surface.agentTypeID;
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = 3f;
            agent.acceleration = 12f;
            agent.angularSpeed = 360f;
            agent.stoppingDistance = 1.2f;
            agent.autoBraking = true;
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Capsule Visual";
            Undo.RegisterCreatedObjectUndo(visual, "Create enemy visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.up;
            // Navigation drives this character; a separate static collider would bake an obstacle.
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            Directory.CreateDirectory("Assets/Art/Materials");
            AssetDatabase.Refresh();
            const string materialPath = "Assets/Art/Materials/EnemyCapsule.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader);
                material.color = new Color(0.8f, 0.12f, 0.1f);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            visual.GetComponent<Renderer>().sharedMaterial = material;
            enemy = Undo.AddComponent<EnemyChase>(root);
        }
        Undo.RecordObject(enemy, "Assign chase target");
        enemy.Target = player.transform;
        NavMeshAgent enemyAgent = enemy.GetComponent<NavMeshAgent>();
        Undo.RecordObject(enemyAgent, "Configure enemy navigation clearance");
        enemyAgent.agentTypeID = surface.agentTypeID;
        enemyAgent.radius = 0.5f;
        enemyAgent.autoRepath = true;
        enemyAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        Bake(surface);
        NavMeshQueryFilter filter = new NavMeshQueryFilter { agentTypeID = enemyAgent.agentTypeID, areaMask = enemyAgent.areaMask };
        if (NavMesh.SamplePosition(enemy.transform.position, out NavMeshHit spawn, 5f, filter))
        {
            Undo.RecordObject(enemy.transform, "Place enemy on NavMesh");
            enemy.transform.position = spawn.position;
        }
        EditorSceneManager.MarkSceneDirty(enemy.gameObject.scene);
        Selection.activeGameObject = enemy.gameObject;
        Debug.Log("ENEMY_SETUP_OK: Capsule enemy targets " + player.name + ". Save the scene, then press Play.");
        ValidatePath();
    }

    [MenuItem("Tools/Dungeon RPG/Rebake Enemy Navigation")]
    public static void Rebake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        NavMeshSurface surface = Object.FindAnyObjectByType<NavMeshSurface>();
        if (surface != null) Bake(surface);
        ValidatePath();
    }

    private static void Bake(NavMeshSurface surface)
    {
        surface.BuildNavMesh();
        if (surface.navMeshData == null) throw new System.InvalidOperationException("Navigation bake failed. Check the floor has a collider.");
        Directory.CreateDirectory("Assets/Navigation");
        AssetDatabase.Refresh();
        if (!AssetDatabase.Contains(surface.navMeshData))
            AssetDatabase.CreateAsset(surface.navMeshData, AssetDatabase.GenerateUniqueAssetPath("Assets/Navigation/EnemyNavMesh.asset"));
        EditorUtility.SetDirty(surface);
        EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Dungeon RPG/Validate Enemy Path")]
    public static void ValidatePath()
    {
        EnemyChase enemy = Object.FindAnyObjectByType<EnemyChase>();
        if (enemy == null || enemy.Target == null) return;
        NavMeshPath path = new NavMeshPath();
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        NavMeshQueryFilter filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (NavMesh.SamplePosition(enemy.Target.position, out NavMeshHit goal, 2f, filter) &&
            NavMesh.CalculatePath(enemy.transform.position, goal.position, filter, path) && path.status == NavMeshPathStatus.PathComplete)
            Debug.Log("ENEMY_PATH_OK: Complete path from enemy to player (" + path.corners.Length + " corners).");
        else Debug.LogWarning("Enemy has no complete path to the player. Check the floor, spawn location, and navigation bake.");
    }
}

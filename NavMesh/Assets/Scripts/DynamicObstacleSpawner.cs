using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.InputSystem;

/// <summary>
/// Spawns dynamic obstacles on mouse click and updates NavMesh in real-time.
/// Task 2
/// </summary>
public class DynamicObstacleSpawner : MonoBehaviour
{
    [Header("References")]
    public NavMeshSurface navMeshSurface;
    public GameObject obstaclePrefab;
    public LayerMask groundLayer;

    [Header("Settings")]
    public float obstacleHeight = 2f;
    public bool autoUpdateNavMesh = true;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (obstaclePrefab == null)
        {
            // Create a simple cube prefab at runtime if none assigned
            obstaclePrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstaclePrefab.name = "DynamicObstacle";
            obstaclePrefab.SetActive(false);
            var renderer = obstaclePrefab.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = Color.red;
        }
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TrySpawnObstacle();
        }
    }

    private void TrySpawnObstacle()
    {
        if (mainCamera == null || navMeshSurface == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            SpawnObstacle(hit.point);
        }
    }

    private void SpawnObstacle(Vector3 position)
    {
        GameObject obstacle = Instantiate(obstaclePrefab, position + Vector3.up * (obstacleHeight * 0.5f), Quaternion.identity);
        obstacle.SetActive(true);
        obstacle.name = "DynamicObstacle_" + Time.frameCount;

        // Add NavMesh Modifier so it acts as obstacle during bake
        var modifier = obstacle.AddComponent<NavMeshModifier>();
        modifier.overrideArea = true;
        modifier.area = 1; // Not Walkable (default area 1 is often Not Walkable)

        // Also add NavMeshObstacle with carving for immediate effect without full re-bake
        var obstacleComp = obstacle.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        obstacleComp.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
        obstacleComp.size = new Vector3(1f, obstacleHeight, 1f);
        obstacleComp.carving = true;
        obstacleComp.carveOnlyStationary = true;

        // Ensure it has a collider
        if (obstacle.GetComponent<Collider>() == null)
            obstacle.AddComponent<BoxCollider>();

        if (autoUpdateNavMesh)
            UpdateNavMesh();
    }

    public void UpdateNavMesh()
    {
        if (navMeshSurface != null)
        {
            // Async update is better for performance
            navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
            // Or synchronous: navMeshSurface.BuildNavMesh();
        }
    }

    // Optional: remove obstacles on right click
    private void LateUpdate()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                if (hit.collider.gameObject.name.Contains("DynamicObstacle"))
                {
                    Destroy(hit.collider.gameObject);
                    if (autoUpdateNavMesh)
                        UpdateNavMesh();
                }
            }
        }
    }
}

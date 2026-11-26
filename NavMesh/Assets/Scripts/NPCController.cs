using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCController : MonoBehaviour
{
    public enum PathStrategy
    {
        ShortestPath,
        MinimalEnemyImpact,
        HiddenMovement
    }
    [Header("Points of Interest")]
    public List<Transform> pointsOfInterest = new List<Transform>();
    public List<Transform> coverPoints = new List<Transform>();
    public List<Transform> enemyObjects = new List<Transform>();
    [Header("Strategy Settings")]
    public PathStrategy currentStrategy = PathStrategy.ShortestPath;
    public float health = 100f;
    public float lowHealthThreshold = 40f;
    public float enemyDetectionRadius = 8f;
    [Header("Agent Settings")]
    public float speed = 3.5f;
    public float angularSpeed = 120f;
    public float acceleration = 8f;
    public float stoppingDistance = 0.5f;
    [Header("Damage Testing")]
    public float damagePerHit = 15f;
    public float enemyDamageRadius = 2.5f;
    public float enemyDamageAmount = 5f;
    public float enemyDamageCooldown = 1.5f;
    private NavMeshAgent agent;
    private int currentPoiIndex = 0;
    private float strategyCheckInterval = 2f;
    private float lastStrategyCheck;
    private float lastEnemyDamageTime;
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;
        agent.radius = 0.5f;
        agent.height = 2f;
    }
    private void Start()
    {
        if (pointsOfInterest.Count > 0)
            MoveToNextPoint();
    }
    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.leftCtrlKey.wasPressedThisFrame)
            {
                TakeDamage(damagePerHit);
                Debug.Log($"NPC took {damagePerHit} damage. Health: {health}");
            }
        }
        if (Time.time - lastEnemyDamageTime > enemyDamageCooldown)
        {
            foreach (var enemy in enemyObjects)
            {
                if (enemy == null) continue;
                if (Vector3.Distance(transform.position, enemy.position) < enemyDamageRadius)
                {
                    TakeDamage(enemyDamageAmount);
                    lastEnemyDamageTime = Time.time;
                    Debug.Log($"Enemy damaged NPC. Health: {health}");
                    break;
                }
            }
        }
        if (Time.time - lastStrategyCheck > strategyCheckInterval)
        {
            UpdateStrategyBasedOnSituation();
            lastStrategyCheck = Time.time;
        }
        if (agent.remainingDistance <= stoppingDistance && !agent.pathPending && pointsOfInterest.Count > 0)
        {
            MoveToNextPoint();
        }
    }
    public void UpdateStrategyBasedOnSituation()
    {
        bool enemiesNearby = AreEnemiesNearby();
        bool lowHealth = health < lowHealthThreshold;
        if (lowHealth || (enemiesNearby && Random.value > 0.5f))
        {
            currentStrategy = PathStrategy.HiddenMovement;
        }
        else if (enemiesNearby)
        {
            currentStrategy = PathStrategy.MinimalEnemyImpact;
        }
        else
        {
            currentStrategy = PathStrategy.ShortestPath;
        }
        Debug.Log($"NPC Strategy changed to: {currentStrategy} | Health: {health:F1} | Enemies nearby: {enemiesNearby}");
    }
    private bool AreEnemiesNearby()
    {
        foreach (var enemy in enemyObjects)
        {
            if (enemy != null && Vector3.Distance(transform.position, enemy.position) < enemyDetectionRadius)
                return true;
        }
        return false;
    }
    public void MoveToNextPoint()
    {
        if (pointsOfInterest.Count == 0) return;
        Transform target = GetTargetBasedOnStrategy();
        if (target != null)
        {
            agent.SetDestination(target.position);
            currentPoiIndex = (currentPoiIndex + 1) % pointsOfInterest.Count;
        }
    }
    private Transform GetTargetBasedOnStrategy()
    {
        switch (currentStrategy)
        {
            case PathStrategy.ShortestPath:
                return pointsOfInterest[currentPoiIndex];
            case PathStrategy.MinimalEnemyImpact:
                return FindSafestPointAwayFromEnemies();
            case PathStrategy.HiddenMovement:
                return FindBestCoverPoint();
            default:
                return pointsOfInterest[currentPoiIndex];
        }
    }
    private Transform FindSafestPointAwayFromEnemies()
    {
        Transform best = pointsOfInterest[currentPoiIndex];
        float bestScore = float.MinValue;
        foreach (var poi in pointsOfInterest)
        {
            float minDistToEnemy = float.MaxValue;
            foreach (var enemy in enemyObjects)
            {
                if (enemy == null) continue;
                float dist = Vector3.Distance(poi.position, enemy.position);
                if (dist < minDistToEnemy) minDistToEnemy = dist;
            }
            float score = minDistToEnemy;
            if (score > bestScore)
            {
                bestScore = score;
                best = poi;
            }
        }
        return best;
    }
    private Transform FindBestCoverPoint()
    {
        if (coverPoints.Count == 0)
            return pointsOfInterest[currentPoiIndex];
        Transform bestCover = coverPoints[0];
        float bestScore = float.MinValue;
        foreach (var cover in coverPoints)
        {
            if (cover == null) continue;
            float score = 0f;
            foreach (var enemy in enemyObjects)
            {
                if (enemy == null) continue;
                float dist = Vector3.Distance(cover.position, enemy.position);
                score += dist;
            }
            score -= Vector3.Distance(transform.position, cover.position) * 0.3f;
            if (score > bestScore)
            {
                bestScore = score;
                bestCover = cover;
            }
        }
        return bestCover;
    }
    public void TakeDamage(float amount)
    {
        health -= amount;
        health = Mathf.Max(0, health);
        if (health <= 0)
            Debug.Log("NPC is dead");
    }
}
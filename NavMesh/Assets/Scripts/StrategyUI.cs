using UnityEngine;
using TMPro;

/// <summary>
/// Simple UI to display current NPC strategy and health using TextMeshPro.
/// </summary>
public class StrategyUI : MonoBehaviour
{
    public NPCController npc;
    public TextMeshProUGUI strategyText;
    public TextMeshProUGUI healthText;

    private void Update()
    {
        if (npc == null || strategyText == null || healthText == null) return;

        strategyText.text = $"Strategy: {npc.currentStrategy}";
        healthText.text = $"Health: {npc.health:F0}";

        // Color coding
        switch (npc.currentStrategy)
        {
            case NPCController.PathStrategy.ShortestPath:
                strategyText.color = Color.green;
                break;
            case NPCController.PathStrategy.MinimalEnemyImpact:
                strategyText.color = Color.yellow;
                break;
            case NPCController.PathStrategy.HiddenMovement:
                strategyText.color = Color.cyan;
                break;
        }
    }
}

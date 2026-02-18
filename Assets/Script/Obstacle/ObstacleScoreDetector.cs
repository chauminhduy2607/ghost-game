using UnityEngine;

/// <summary>
/// Detector được ImprovedObstacleSpawner kiểm soát hoàn toàn.
/// KHÔNG tự init trong OnEnable — spawner gọi Activate() sau khi đã reposition xong.
/// </summary>
public class ObstacleScoreDetector : MonoBehaviour
{
    [Header("=== CONFIG (set by spawner) ===")]
    [SerializeField] private int pointsOnPass = 2;
    [SerializeField] private float detectionOffsetY = 1f;
    [SerializeField] private bool isParentPlaceholder = false;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugLog = true;

    private bool hasPassed = false;
    private Transform player;
    private ScoreCycle scoreCycle;
    private float passThresholdY;
    private bool active = false; // chỉ true khi spawner gọi Activate()

    // ─────────────────────────────────────────────────────────────────────
    // Gọi từ spawner ngay sau AddComponent() — chỉ lưu config, chưa init threshold
    // ─────────────────────────────────────────────────────────────────────
    public void Configure(int points, float offsetY, bool placeholder)
    {
        pointsOnPass = points;
        detectionOffsetY = offsetY;
        isParentPlaceholder = placeholder;
        active = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Gọi từ spawner SAU KHI đã SetActive(true) VÀ reposition xong
    // ─────────────────────────────────────────────────────────────────────
    public void Activate()
    {
        if (isParentPlaceholder)
        {
            active = false;
            enabled = false;
            return;
        }

        hasPassed = false;
        active = false; // tạm false, chờ references

        if (player == null)
        {
            GhostController ghost = FindAnyObjectByType<GhostController>();
            if (ghost != null) player = ghost.transform;
        }
        if (scoreCycle == null)
        {
            scoreCycle = FindAnyObjectByType<ScoreCycle>();
        }

        // Tính threshold dựa trên vị trí HIỆN TẠI (đã được reposition)
        passThresholdY = GetMyTopY() + detectionOffsetY;
        active = true;
        enabled = true;

        if (showDebugLog)
            Debug.Log($"[ScoreDetector] ACTIVATED: {gameObject.name} | thresholdY={passThresholdY:F2} | +{pointsOnPass}đ");
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!active || hasPassed || player == null || scoreCycle == null) return;

        if (player.position.y > passThresholdY)
        {
            hasPassed = true;
            int before = scoreCycle.GetScore();
            scoreCycle.SetScore(before + pointsOnPass);

            if (showDebugLog)
                Debug.Log($"[Score +{pointsOnPass}] {gameObject.name} | {before} → {scoreCycle.GetScore()}");
        }
    }

    float GetMyTopY()
    {
        Renderer r = GetComponent<Renderer>();
        if (r != null) return r.bounds.max.y;
        return transform.position.y;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !active || isParentPlaceholder) return;
        Gizmos.color = hasPassed ? Color.green : Color.yellow;
        Gizmos.DrawLine(
            new Vector3(transform.position.x - 1.5f, passThresholdY, 0),
            new Vector3(transform.position.x + 1.5f, passThresholdY, 0));
    }
}
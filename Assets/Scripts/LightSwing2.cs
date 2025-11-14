using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LightSwing2 : MonoBehaviour
{
    [Header("首振り")]
    public float swingAngle = 30f;    // 左右角
    public float speed = 1f;          // 首振り速度

    [Header("検知（ライトの当たり判定）")]
    public float detectionRadius = 5f;
    public float detectionAngle = 90f;
    public LayerMask obstacleMask;
    [Tooltip("変装中は無視するならON")]
    public bool respectDisguise = false;

    [Header("検知 → プレイヤー側のタイマー延長")]
    [Tooltip("ライト範囲に入った“瞬間”に加算する検知秒数")]
    public float addDetectSeconds = 5f;

    [Header("ゲームオーバー条件")]
    [Tooltip("範囲内に連続でこの秒数以上いるとGameOver")]
    public float stayToGameOverSeconds = 2f;
    public string gameOverSceneName = "GameOver";

    [Header("敵通知（任意）")]
    public EnemyPatrol2[] affectedEnemies;

    [Header("照射方向の微調整")]
    public float directionOffset = 0f;

    private float startAngle;
    private PlayerController playerController;
    private bool wasPlayerInRange = false;
    private float stayTimer = 0f;
    private bool gameOverQueued = false;

    // Start is called before the first frame update
    void Start()
    {
        startAngle = transform.eulerAngles.z;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) playerController = playerObj.GetComponent<PlayerController>();

        if (affectedEnemies == null || affectedEnemies.Length == 0)
            affectedEnemies = FindObjectsOfType<EnemyPatrol2>();
    }

    // Update is called once per frame
    void Update()
    {
        // 首振り
        float angleOffset = Mathf.Sin(Time.time * speed) * swingAngle;
        transform.rotation = Quaternion.Euler(0, 0, startAngle + angleOffset);

        bool inRange = IsPlayerInLightRange();

        // 変装を尊重する設定なら、変装中はヒットしない
        if (respectDisguise && playerController && playerController.IsDisguised)
            inRange = false;

        // 入った瞬間の処理（延長＆敵通知）
        if (inRange && !wasPlayerInRange)
        {
            OnPlayerEnterRange(); // 敵へ通知
            if (playerController && addDetectSeconds > 0f)
                playerController.AddDetectionTime(addDetectSeconds);
        }

        // 連続滞在時間の監視
        if (inRange)
        {
            stayTimer += Time.deltaTime;
            if (!gameOverQueued && stayTimer >= stayToGameOverSeconds)
                StartCoroutine(GameOverSequence());
        }
        else
        {
            stayTimer = 0f; // 離れたらリセット（“連続”条件）
        }

        wasPlayerInRange = inRange;
    }

    private bool IsPlayerInLightRange()
    {
        if (!playerController || !playerController.transform) return false;

        Vector2 lightPos = transform.position;
        Vector2 playerPos = playerController.transform.position;
        Vector2 toPlayer = playerPos - lightPos;
        float distance = toPlayer.magnitude;

        if (distance > detectionRadius) return false;

        // 角度（transform.up を前とし、必要ならオフセット）
        Vector2 forward = Quaternion.Euler(0, 0, directionOffset) * (Vector2)transform.up;
        float angleToPlayer = Vector2.Angle(forward, toPlayer);
        if (angleToPlayer > detectionAngle * 0.5f) return false;

        // 遮蔽（壁）
        RaycastHit2D hit = Physics2D.Raycast(lightPos, toPlayer.normalized, distance, obstacleMask);
        if (hit.collider != null) return false;

        return true;
    }

    private void OnPlayerEnterRange()
    {
        if (affectedEnemies == null) return;
        foreach (var enemy in affectedEnemies)
            if (enemy) enemy.OnPlayerDetectedByLight();
    }

    private IEnumerator GameOverSequence()
    {
        gameOverQueued = true;
        // 必要ならここでBGMフェード/画面フェードを入れる
        if (!string.IsNullOrEmpty(gameOverSceneName))
            SceneManager.LoadScene(gameOverSceneName);
        else
            Debug.LogWarning("[LightSwing] gameOverSceneName が未設定です。");
        yield break;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Vector2 forward = Quaternion.Euler(0, 0, directionOffset) * (Vector2)transform.up;
        Vector2 leftBound = Quaternion.Euler(0, 0, -detectionAngle * 0.5f) * forward;
        Vector2 rightBound = Quaternion.Euler(0, 0, detectionAngle * 0.5f) * forward;
        Gizmos.DrawRay(transform.position, leftBound * detectionRadius);
        Gizmos.DrawRay(transform.position, rightBound * detectionRadius);
    }
}

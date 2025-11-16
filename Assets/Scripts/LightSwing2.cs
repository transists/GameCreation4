using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LightSwing2 : MonoBehaviour
{
    [Header("首振り")]
    public float swingAngle = 30f;    // 左右角（±）
    public float speed = 1f;          // 首振り速度（Sin波）

    [Header("回転ターゲット")]
    [Tooltip("回転させるTransform。未指定なら自身")]
    public Transform pivot;

    [Header("回転に追従する見た目（任意）")]
    [Tooltip("回転ヘッドのTransform（SpriteRenderer を持つ子推奨）")]
    public Transform lampHead;
    [Tooltip("Pivot の up 方向へどれだけ離すか（ローカル距離）")]
    public float headDistance = 0.6f;
    [Tooltip("スプライト絵の向き補正（度）。絵が右向きなら -90 が目安")]
    public float headSpriteAngleOffset = 0f;

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

    // --- 内部 ---
    private float startAngle;
    private PlayerController playerController;
    private bool wasPlayerInRange = false;
    private float stayTimer = 0f;
    private bool gameOverQueued = false;


    // Start is called before the first frame update
    void Start()
    {
        // pivot 未指定なら自分を使う
        if (!pivot) pivot = transform;

        startAngle = transform.eulerAngles.z;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) playerController = playerObj.GetComponent<PlayerController>();

        if (affectedEnemies == null || affectedEnemies.Length == 0)
            affectedEnemies = FindObjectsOfType<EnemyPatrol2>();
    }

    // Update is called once per frame
    void Update()
    {
        // ===== 1) 首振り（pivot を回す） =====
        float angleOffset = Mathf.Sin(Time.time * speed) * swingAngle;
        pivot.rotation = Quaternion.Euler(0, 0, startAngle + angleOffset);

        // ===== 2) 見た目のヘッド追従 =====
        if (lampHead)
        {
            // pivot ローカルの up 方向へオフセット
            lampHead.localPosition = Vector3.up * headDistance;
            // スプライト絵の向き補正（見た目だけローカルで追加）
            lampHead.localRotation = Quaternion.Euler(0, 0, headSpriteAngleOffset);
        }

        // ===== 3) 検知判定（pivot 基準） =====
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

        // 連続滞在時間の監視（“連続”条件）
        if (inRange)
        {
            stayTimer += Time.deltaTime;
            if (!gameOverQueued && stayTimer >= stayToGameOverSeconds)
                StartCoroutine(GameOverSequence());
        }
        else
        {
            stayTimer = 0f;
        }

        wasPlayerInRange = inRange;
    }

    private bool IsPlayerInLightRange()
    {
        if (!playerController || !playerController.transform) return false;

        Vector2 lightPos = pivot.position;
        Vector2 playerPos = playerController.transform.position;
        Vector2 toPlayer = playerPos - lightPos;
        float distance = toPlayer.magnitude;

        if (distance > detectionRadius) return false;

        // 角度（pivot.up を前とし、必要ならオフセット）
        Vector2 forward = Quaternion.Euler(0, 0, directionOffset) * (Vector2)pivot.up;
        float angleToPlayer = Vector2.Angle(forward, toPlayer);
        if (angleToPlayer > detectionAngle * 0.5f) return false;

        // 遮蔽物（壁）
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
        var piv = pivot ? pivot : transform;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Vector2 forward = Quaternion.Euler(0, 0, directionOffset) * (Vector2)transform.up;
        Vector2 leftBound = Quaternion.Euler(0, 0, -detectionAngle * 0.5f) * forward;
        Vector2 rightBound = Quaternion.Euler(0, 0, detectionAngle * 0.5f) * forward;
        Gizmos.DrawRay(transform.position, leftBound * detectionRadius);
        Gizmos.DrawRay(transform.position, rightBound * detectionRadius);

        if (lampHead)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(lampHead.position, 0.06f);
        }
    }
}

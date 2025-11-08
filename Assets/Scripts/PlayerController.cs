using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool IsDisguised { get; private set; } = false;
    [Header("マップ情報")]
    public LayerMask wallLayer; // 壁レイヤーをインスペクターから設定
    private bool canUseDisguise = true; // 変装が一度だけ使えるようにするためのフラグ
    public Color disguisedColor = Color.cyan; // 変装中の色（インスペクターで変更可能）
    public float disguiseDuration = 10f; // 変装している時間
    private Color originalColor; // 元の色を保存する変数
    private SpriteRenderer spriteRenderer;

    // --- ↓ここからグリッド移動用のコード ---
    public float moveSpeed = 2.5f; // 1マスを移動する速さ
    //private bool isMoving = false; // 移動中かどうかのフラグ
    private Vector3 targetPosition; // 目標地点
    private Rigidbody2D rb;
    private Vector2 moveInput;
    // --- ↑ここまでグリッド移動用のコード ---

    [Header("見た目：向き別スプライト")]
    public Sprite frontSprite;        // 正面（デフォルト＆↓）
    public Sprite backSprite;         // 後ろ（↑）
    [Tooltip("向き切替のデッドゾーン（小さすぎる上下入力は無視）")]
    public float facingDeadZone = 0.1f;

    private SpriteRenderer sr;

    [Header("検知時のスピード設定")]
    [Tooltip("敵に見つかっている間の速度倍率")]
    public float detectedSpeedMultiplier = 1.6f;
    [Tooltip("速度の補間係数（大きいほど素早く目標速度へ）")]
    public float speedLerp = 12f;

    [Header("ゴール制限")]
    [Tooltip("発見後ゴール不可の秒数")]
    public float goalLockSeconds = 10f;

    private float goalLockTimer = 0f;
    public bool CanGoal => goalLockTimer <= 0f;

    // 内部状態
    private bool isDetected = false;
    public float currentSpeed;


    // Start is called before the first frame update
    void Start()
    {
        if (sr) originalColor = sr.color; // 開始時の色を記憶
        // 現在位置から一番近いタイルの中心にスナップさせる
        float x = Mathf.Floor(transform.position.x) + 0.5f;
        float y = Mathf.Floor(transform.position.y) + 0.5f;
        transform.position = new Vector3(x, y, 0);

        // 移動の目標地点も初期化しておく
        targetPosition = transform.position;
        currentSpeed = moveSpeed; // 現在速度を基準速度から開始
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        // Eキーを押したら変装する
        if (canUseDisguise && Input.GetKeyDown(KeyCode.E))
        {
            Disguise();
        }

        // 获取输入方向（键盘）
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveY).normalized;
        // 组合为向量并归一化
        UpdateFacingByInput(moveInput);
        // 速度ターゲットを決めて補間（見つかっていれば倍率を掛ける）
        float targetSpeed = isDetected ? moveSpeed * detectedSpeedMultiplier : moveSpeed;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedLerp * Time.deltaTime);

        if (goalLockTimer > 0f)
        {
            goalLockTimer -= Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        // 使用物理方式移动（会检测碰撞）
        rb.MovePosition(rb.position + moveInput * currentSpeed * Time.fixedDeltaTime);
    }

    // 移動の入力と判定を行うメソッド
    /*private void HandleMovement()
    {
        if (isMoving) return;

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 potentialTargetPosition = transform.position;
        bool inputReceived = false;

        if (Mathf.Abs(horizontalInput) > 0.5f)
        {
            potentialTargetPosition += new Vector3(Mathf.Sign(horizontalInput), 0, 0);
            inputReceived = true;
        }
        else if (Mathf.Abs(verticalInput) > 0.5f)
        {
            potentialTargetPosition += new Vector3(0, Mathf.Sign(verticalInput), 0);
            inputReceived = true;
        }

        if (inputReceived && IsValidMove(potentialTargetPosition))
        {
            targetPosition = potentialTargetPosition;
            isMoving = true;
        }
    }*/

    private void UpdateFacingByInput(Vector2 input)
    {
        if (!sr) return;

        // 上下入力でのみ切替。左右や停止時は仕様通り「正面」維持
        if (input.y > facingDeadZone)
        {
            // 上へ：後ろ姿
            if (backSprite && sr.sprite != backSprite) sr.sprite = backSprite;
        }
        else
        {
            // デフォルト＆下/停止：正面
            if (frontSprite && sr.sprite != frontSprite) sr.sprite = frontSprite;
        }
    }

    private bool IsValidMove(Vector3 targetPos)
    {
        // 方法1：タイルマップで判定する場合
        // Vector3Int targetCell = wallTilemap.WorldToCell(targetPos);
        // if (wallTilemap.HasTile(targetCell))
        // {
        //     return false; // 壁があるので移動不可
        // }

        // 方法2：レイヤーで判定する場合
        Collider2D hit = Physics2D.OverlapCircle(targetPos, 0.2f, wallLayer);
        if (hit != null)
        {
            return false; // 壁があるので移動不可
        }

        // どのチェックにも引っかからなければ移動可能
        return true;
    }

    private void Disguise()
    {
        // 変装状態にする
        IsDisguised = true;

        // もう使えないようにフラグをfalseにする
        canUseDisguise = false;

        // 見た目を変える（例：色を変える）
        if (sr) sr.color = disguisedColor;

        StartCoroutine(DisguiseTimerCoroutine());
        Debug.Log("変装した！ 10秒後に解除されます。");
    }

    // 10秒待ってから変装解除を呼び出すコルーチン
    private System.Collections.IEnumerator DisguiseTimerCoroutine()
    {
        // disguiseDurationで指定した秒数だけ待つ
        yield return new WaitForSeconds(disguiseDuration);

        // 時間が来たら変装解除メソッドを呼ぶ
        RemoveDisguise();
    }

    // 変装を解除するメソッド
    private void RemoveDisguise()
    {
        IsDisguised = false;

        // 色を元の色に戻す
        if (sr) sr.color = originalColor;

        Debug.Log("変装が解除された！");
    }

    // 公開API：敵側から発見/見失いを通知
    public void SetDetected(bool detected, float speedMultiplier = -1f)
    {
        // 立ち上がり（false -> true）で10秒ロック開始
        if (!isDetected && detected)
        {
            goalLockTimer = goalLockSeconds;
        }

        // 警备员追逐玩家时的玩家速度，默认0表示玩家速度不变更，超过0的数字表示玩家速度倍率
        if (speedMultiplier >= 0f)
        {
            detectedSpeedMultiplier = speedMultiplier;
        }

        // 状態が変わった時のみ切り替え（不要なら単純代入でOK）
        isDetected = detected;
    }
}

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
    public float moveSpeed = 5f; // 1マスを移動する速さ
    private bool isMoving = false; // 移動中かどうかのフラグ
    private Vector3 targetPosition; // 目標地点
    private Rigidbody2D rb;
    private Vector2 moveInput;
    // --- ↑ここまでグリッド移動用のコード ---

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color; // 開始時の色を記憶
        // 現在位置から一番近いタイルの中心にスナップさせる
        float x = Mathf.Floor(transform.position.x) + 0.5f;
        float y = Mathf.Floor(transform.position.y) + 0.5f;
        transform.position = new Vector3(x, y, 0);

        // 移動の目標地点も初期化しておく
        targetPosition = transform.position;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        // Eキーを押したら変装する
        if (canUseDisguise && Input.GetKeyDown(KeyCode.E))
        {
            Disguise();
        }

        HandleMovement();

        /* --- ↓ここからグリッド移動用のコード ---
        if (!isMoving)
        {
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            float verticalInput = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(horizontalInput) > 0.5f)
            {
                targetPosition = transform.position + new Vector3(Mathf.Sign(horizontalInput), 0, 0);
                isMoving = true;
            }
            else if (Mathf.Abs(verticalInput) > 0.5f)
            {
                targetPosition = transform.position + new Vector3(0, Mathf.Sign(verticalInput), 0);
                isMoving = true;
            }
        }

        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            if (transform.position == targetPosition)
            {
                isMoving = false;
            }
        }
        / --- ↑ここまでグリッド移動用のコード ---*/

        // 获取输入方向（键盘）
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        // 组合为向量并归一化
        moveInput = new Vector2(moveX, moveY).normalized;
    }

    void FixedUpdate()
    {
        // 使用物理方式移动（会检测碰撞）
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    // 移動の入力と判定を行うメソッド
    private void HandleMovement()
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
        if (spriteRenderer != null)
        {
            spriteRenderer.color = disguisedColor;
        }

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
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        Debug.Log("変装が解除された！");
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GoalShieldController : MonoBehaviour
{
    [Header("参照")]
    public PlayerController player;           // シーンのプレイヤーをドラッグ
    public Collider2D shieldCollider;         // 子の CircleCollider2D（IsTrigger=false）
    public SpriteRenderer shieldVisual;       // 任意：見た目のリング

    [Header("押し出し設定（任意）")]
    public float separationPadding = 0.001f;  // 少しだけ余裕を持たせる

    Collider2D playerCol;
    Rigidbody2D playerRb;

    void Reset()
    {
        // 自動取得の補助（エディタ上でAdd時）
        shieldCollider = GetComponent<Collider2D>();
        shieldVisual = GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (player == null) player = FindObjectOfType<PlayerController>();
        if (shieldCollider == null) shieldCollider = GetComponent<Collider2D>();

        if (player != null)
        {
            playerCol = player.GetComponent<Collider2D>();
            playerRb  = player.GetComponent<Rigidbody2D>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null || shieldCollider == null) return;

        // プレイヤーがゴール可能ならシールドOFF、不可ならON
        bool active = !player.CanGoal;

        if (shieldCollider.enabled != active)
        {
            shieldCollider.enabled = active;
            if (shieldVisual) shieldVisual.enabled = active;

            if (active)
            {
                // シールドONになった瞬間、もし中にいたら外へ押し出す
                PushPlayerOutIfOverlapped();
            }
        }
    }

    void PushPlayerOutIfOverlapped()
    {
        if (playerCol == null || playerRb == null) return;

        // player と shield の重なり量を取得
        // 注意：Distance(a,b).normal は a→b 法線方向
        ColliderDistance2D d = Physics2D.Distance(playerCol, shieldCollider);

        if (d.isOverlapped)
        {
            // プレイヤーをシールド外側へ押し出す最小分離ベクトル
            // MTV = -(normal) * (distance + padding)  （distanceは負）
            Vector2 mtv = -d.normal * (d.distance + separationPadding);
            playerRb.position += mtv;
            Physics2D.SyncTransforms();
        }
    }
}

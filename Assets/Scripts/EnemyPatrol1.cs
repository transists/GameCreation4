using UnityEngine;

public class EnemyPatrol1 : MonoBehaviour
{
    public Transform[] patrolPoints; // 巡逻点
    public float moveSpeed = 3f;
    private int currentPointIndex = 0;

    public EnemyFieldOfView fieldOfView;
    public PlayerController playerController; // 玩家对象
    AudioSource se;
    public AudioClip shotSE;

    private SpriteRenderer spriteRenderer;

    public float gameOverTime = 2.0f; // 游戏结束延迟时间
    private float playerVisibleTimer = 0.0f; // 玩家可见时间

    [Header("検知の揺れ抑制")]
    public float loseSightGrace = 0.3f;   // 見失い後の猶予
    private float lastSeenTime = -999f;   // 最後に見た時刻

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
		se = GetComponent<AudioSource>(); //SE再生用
	}

    void Update()
    {
        // まずは参照の健全性チェック
        if (playerController == null || playerController.transform == null || fieldOfView == null) return;

        // 実視認（距離・角度・遮蔽・変装）で判定
        bool canSeeNow = PlayerInSight();
        if (canSeeNow) lastSeenTime = Time.time;

        // 猶予込みの“追跡状態”を決める（チラつき対策）
        bool chasing = canSeeNow || (Time.time - lastSeenTime <= loseSightGrace);

        // ★ プレイヤーへ検知状態を通知（ここが速度アップのトリガ）
        playerController.SetDetected(chasing);

        if (canSeeNow)
        {
            // ゲームオーバー用の“連続可視時間”は猶予なしの実視認で積算
            playerVisibleTimer += Time.deltaTime;
            if (playerVisibleTimer >= gameOverTime)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
                return;
            }
        }
        else
        {
            playerVisibleTimer = 0f;
        }

        // 行動
        if (chasing)
            ChasePlayer();
        else
            Patrol();
    }

    private void Patrol()
    {
        Vector2 direction = (patrolPoints[currentPointIndex].position - transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, patrolPoints[currentPointIndex].position, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, patrolPoints[currentPointIndex].position) < 0.1f)
        {
            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
        }

        AdjustRotation(direction);
    }

    private void ChasePlayer()
    {
        Vector2 direction = (playerController.transform.position - transform.position).normalized;
        AdjustRotation(direction);
    }

    private bool PlayerInSight()
    {
        // 変装中は見えない（完全ステルス仕様）
        if (playerController.IsDisguised) return false;

        Vector2 myPos = transform.position;
        Vector2 playerPos = playerController.transform.position;
        Vector2 toPlayer = (playerPos - myPos);
        float dist = toPlayer.magnitude;

        // 距離
        if (dist > fieldOfView.viewRadius) return false;

        // 角度（FOVと同じく transform.up を正面として扱う）
        float angle = Vector2.Angle(transform.up, toPlayer);
        if (angle > fieldOfView.viewAngle * 0.5f) return false;

        // 遮蔽物（壁）チェック：FOV側の obstacleMask を使う
        RaycastHit2D hitWall = Physics2D.Raycast(myPos, toPlayer.normalized, dist, fieldOfView.obstacleMask);
        if (hitWall.collider != null) return false;

        return true;
    }

    private void AdjustRotation(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        //Debug.Log("Enemy Rotation Angle: " + angle);
        if (fieldOfView != null)
        {
            fieldOfView.transform.rotation = transform.rotation;
        }
    }

}

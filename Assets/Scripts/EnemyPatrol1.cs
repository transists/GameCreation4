using UnityEngine;

public class EnemyPatrol1 : MonoBehaviour
{
    public Transform[] patrolPoints; // 巡逻点
    public float moveSpeed = 3f; // 巡逻时的移动速度
    private int currentPointIndex = 0;

    [Header("追逐设置")]
    [Tooltip("追逐玩家时的移动速度（可手动调节）")]
    public float chaseSpeed = 5f; // 追逐时的移动速度
    [Tooltip("玩家被发现时的移动速度倍率（0表示使用PlayerController的默认值，>0则覆盖）")]
    public float playerSpeedMultiplier = 0f; // 玩家速度倍率（可选，0=使用默认值，>0=覆盖设置）

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

    private bool gameEnded = false; // 防止重复触发游戏结束

    [Header("検知延長（侵入で加算）")]
    public float detectionExtendSeconds = 10f;   // 追加：スポットライト侵入で延長する秒数
    private bool wasSeeing = false;              // 追加：前フレーム視認状態

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
		se = GetComponent<AudioSource>(); //SE再生用
	}

    void Update()
    {
        // まずは参照の健全性チェック
        if (playerController == null || playerController.transform == null || fieldOfView == null) return;
        if (gameEnded) return; // 如果游戏已结束，不再执行后续逻辑

        // 実視認（距離・角度・遮蔽・変装）で判定
        bool canSeeNow = PlayerInSight();
        // ★ 侵入の“立ち上がり”で一度だけ検知延長（＆倍率上書きがあれば同時適用）
        if (canSeeNow && !wasSeeing)
        {
            if (playerSpeedMultiplier > 0f)
                playerController.AddDetectionTimeWithMultiplier(detectionExtendSeconds, playerSpeedMultiplier);
            else
                playerController.AddDetectionTime(detectionExtendSeconds);
        }
        wasSeeing = canSeeNow;

        // （ゲームオーバー用）実視認のみで積算
        if (canSeeNow)
        {
            playerVisibleTimer += Time.deltaTime;
            if (playerVisibleTimer >= gameOverTime)
            {
                GameOver();
                return;
            }
        }
        else
        {
            playerVisibleTimer = 0f;
        }

        // 追跡状態（見失い猶予込み）
        if (canSeeNow) lastSeenTime = Time.time;
        bool chasing = canSeeNow || (Time.time - lastSeenTime <= loseSightGrace);

        if (chasing) ChasePlayer();
        else Patrol();
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
        Vector2 myPos = transform.position;
        Vector2 playerPos = playerController.transform.position;
        Vector2 direction = (playerPos - myPos);
        float distance = direction.magnitude;
        
        // 如果已经非常接近玩家，停止移动
        if (distance < 0.1f)
        {
            AdjustRotation(direction.normalized);
            return;
        }
        
        direction.Normalize();
        
        // 调整朝向（保留原有功能）
        AdjustRotation(direction);
        
        // 追逐移动：朝玩家方向移动
        Vector2 nextPosition = Vector2.MoveTowards(myPos, playerPos, chaseSpeed * Time.deltaTime);
        
        // 碰撞检测：检查移动路径是否有障碍物
        if (CanMoveTo(nextPosition))
        {
            transform.position = nextPosition;
        }
        // 如果有障碍物，尝试沿着障碍物移动（简单的避障）
        else
        {
            // 尝试稍微调整方向绕过障碍物
            Vector2[] directions = new Vector2[] 
            {
                new Vector2(Mathf.Sign(direction.x), 0),  // 只水平移动
                new Vector2(0, Mathf.Sign(direction.y)),  // 只垂直移动
            };
            
            foreach (Vector2 altDirection in directions)
            {
                if (altDirection.magnitude < 0.1f) continue; // 跳过零向量
                
                Vector2 altPosition = myPos + altDirection.normalized * chaseSpeed * Time.deltaTime;
                if (CanMoveTo(altPosition))
                {
                    transform.position = altPosition;
                    break;
                }
            }
        }
    }
    
    // 检查是否可以移动到目标位置
    private bool CanMoveTo(Vector2 targetPos)
    {
        if (fieldOfView == null) return true;
        
        // 使用与视野检测相同的障碍物遮罩
        float checkDistance = Vector2.Distance(transform.position, targetPos);
        if (checkDistance < 0.01f) return true; // 如果距离很小，允许移动
        
        Vector2 direction = (targetPos - (Vector2)transform.position).normalized;
        
        // 射线检测：检查移动路径上是否有障碍物
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, checkDistance + 0.1f, fieldOfView.obstacleMask);
        if (hit.collider != null)
        {
            return false; // 有障碍物，不能移动
        }
        
        // 圆形检测：检查目标位置是否有障碍物（防止穿过薄墙）
        Collider2D overlap = Physics2D.OverlapCircle(targetPos, 0.2f, fieldOfView.obstacleMask);
        if (overlap != null)
        {
            return false; // 目标位置有障碍物
        }
        
        return true; // 可以移动
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

    // 游戏结束方法
    private void GameOver()
    {
        if (gameEnded) return; // 防止重复触发

        gameEnded = true;
        Debug.Log("游戏结束：敌人碰撞到玩家");
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
    }

    // Unity碰撞系统：当敌人碰撞到玩家时触发游戏结束
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (gameEnded) return;

        // 检查碰撞的对象是否是玩家
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<PlayerController>() != null)
        {
            GameOver();
        }
    }

}

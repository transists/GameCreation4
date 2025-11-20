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

    [Header("灯光检测速度设置")]
    [Tooltip("玩家进入LightSwing范围时，敌人移动速度的倍率")]
    public float detectedSpeedMultiplier = 1.5f;
    [Tooltip("灯光检测后的持续时间（秒），可配置")]
    public float lightDetectionDuration = 10f;
    [Tooltip("当玩家再次进入灯光范围时，延长的秒数（可配置）")]
    public float lightDetectionExtendSeconds = 10f;
    
    private float lightDetectionTimer = 0f;      // 灯光检测计时器
    private float originalMoveSpeed;             // 保存原始移动速度
    private float originalChaseSpeed;            // 保存原始追逐速度

    [Header("見た目：向き別スプライト")]
    public Sprite frontSprite;        // 正面（デフォルト＆↓）
    public Sprite backSprite;         // 後ろ（↑）
    public Sprite leftSprite;         // 左（←）
    public Sprite rightSprite;        // 右（→）
    
    private Vector2 lastValidDirection = Vector2.up; // 存储最后一个有效移动方向

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
		se = GetComponent<AudioSource>(); //SE再生用
		
		// 确保transform不旋转，保持UI方向不变
		transform.rotation = Quaternion.identity;
		
		// 初始化Sprite（如果没有设置，使用默认的）
		if (spriteRenderer != null && frontSprite == null)
		{
		    frontSprite = spriteRenderer.sprite;
		}
		
		// 初始化fieldOfView的旋转（如果存在且需要初始方向）
		// 这里可以根据需要设置初始朝向，比如向上
		if (fieldOfView != null)
		{
		    // fieldOfView会在移动时根据方向自动旋转
		    // 初始时可以设置为向上，或者保持当前旋转
		}
		
		// 保存原始速度
		originalMoveSpeed = moveSpeed;
		originalChaseSpeed = chaseSpeed;
	}

    void Update()
    {
        // まずは参照の健全性チェック
        if (playerController == null || playerController.transform == null || fieldOfView == null) return;
        if (gameEnded) return; // 如果游戏已结束，不再执行后续逻辑

        // 更新灯光检测计时器
        UpdateLightDetectionTimer();

        // 実視認（距離・角度・遮蔽・変装）で判定
        bool canSeeNow = PlayerInSight();
        // ★ 侵入の"立ち上がり"で一度だけ検知延長（＆倍率上書きがあれば同時適用）
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
    
    /// <summary>
    /// 更新灯光检测计时器
    /// </summary>
    private void UpdateLightDetectionTimer()
    {
        // 如果计时器还在运行，递减计时器
        if (lightDetectionTimer > 0f)
        {
            lightDetectionTimer -= Time.deltaTime;
            
            // 如果计时器结束，恢复原始速度
            if (lightDetectionTimer <= 0f)
            {
                lightDetectionTimer = 0f;
                RestoreOriginalSpeed();
            }
        }
    }
    
    /// <summary>
    /// 当玩家被灯光检测到时调用（由LightSwing调用）
    /// </summary>
    public void OnPlayerDetectedByLight()
    {
        // 如果计时器已经在运行，延长计时器
        if (lightDetectionTimer > 0f)
        {
            lightDetectionTimer += lightDetectionExtendSeconds;
        }
        else
        {
            // 如果计时器未运行，启动计时器
            lightDetectionTimer = lightDetectionDuration;
        }
        
        // 应用速度倍率
        ApplySpeedMultiplier();
    }
    
    /// <summary>
    /// 应用速度倍率
    /// </summary>
    private void ApplySpeedMultiplier()
    {
        moveSpeed = originalMoveSpeed * detectedSpeedMultiplier;
        chaseSpeed = originalChaseSpeed * detectedSpeedMultiplier;
    }
    
    /// <summary>
    /// 恢复原始速度
    /// </summary>
    private void RestoreOriginalSpeed()
    {
        moveSpeed = originalMoveSpeed;
        chaseSpeed = originalChaseSpeed;
    }

    private void Patrol()
    {
        Vector2 targetPos = patrolPoints[currentPointIndex].position;
        Vector2 direction = (targetPos - (Vector2)transform.position);
        
        // 如果距离很近，切换到下一个巡逻点
        if (direction.magnitude < 0.1f)
        {
            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
            targetPos = patrolPoints[currentPointIndex].position;
            direction = (targetPos - (Vector2)transform.position);
        }
        
        // 移动
        transform.position = Vector2.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
        
        // 始终更新视野朝向（即使距离很近也更新，确保视野跟随移动方向）
        AdjustRotation(direction);
    }

    private void ChasePlayer()
    {
        Vector2 myPos = transform.position;
        Vector2 playerPos = playerController.transform.position;
        Vector2 direction = (playerPos - myPos);
        float distance = direction.magnitude;
        
        // 确保方向向量有效
        if (distance < 0.01f)
        {
            // 如果距离太近，使用最后一个有效方向
            // 这样视野会保持朝向玩家，即使不移动
            direction = lastValidDirection;
        }
        else
        {
            direction.Normalize();
        }
        
        // 始终更新视野朝向（即使不移动也要更新，确保视野跟随玩家）
        AdjustRotation(direction);
        
        // 如果已经非常接近玩家，停止移动但保持视野朝向
        if (distance < 0.1f)
        {
            return;
        }
        
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
                    // 更新视野朝向实际移动的方向
                    AdjustRotation(altDirection.normalized);
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

        // 角度检测：使用fieldOfView的朝向（因为fieldOfView会根据移动方向旋转）
        // 如果fieldOfView不存在，使用世界坐标的上方向作为默认
        Vector2 forwardDirection = fieldOfView != null ? fieldOfView.transform.up : Vector2.up;
        float angle = Vector2.Angle(forwardDirection, toPlayer);
        if (angle > fieldOfView.viewAngle * 0.5f) return false;

        // 遮蔽物（壁）チェック：FOV側の obstacleMask を使う
        RaycastHit2D hitWall = Physics2D.Raycast(myPos, toPlayer.normalized, dist, fieldOfView.obstacleMask);
        if (hitWall.collider != null) return false;

        return true;
    }

    private void AdjustRotation(Vector2 direction)
    {
        if (direction.magnitude < 0.01f) 
        {
            // 如果方向向量太小，使用最后一个有效方向
            direction = lastValidDirection;
        }
        else
        {
            // 归一化方向向量并保存
            direction.Normalize();
            lastValidDirection = direction;
        }
        
        // 确保transform不旋转，保持UI方向不变
        transform.rotation = Quaternion.identity;
        
        // 根据移动方向更新Sprite（不旋转transform，保持UI方向）
        UpdateSpriteByDirection(direction);
        
        // 只旋转视野（fieldOfView），用于视野检测
        // transform保持不旋转，这样UI/Sprite就不会旋转
        if (fieldOfView != null)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            fieldOfView.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    // 根据移动方向更新Sprite
    private void UpdateSpriteByDirection(Vector2 direction)
    {
        if (spriteRenderer == null) return;
        
        // 获取方向的绝对值，判断主要移动方向
        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);
        
        // 判断主要移动方向（水平或垂直）
        // 使用一个小的阈值来处理对角线移动，优先选择绝对值更大的方向
        if (absX > absY + 0.1f)
        {
            // 水平移动为主（右或左）
            if (direction.x > 0)
            {
                // 向右移动
                if (rightSprite != null)
                    spriteRenderer.sprite = rightSprite;
            }
            else if (direction.x < 0)
            {
                // 向左移动
                if (leftSprite != null)
                    spriteRenderer.sprite = leftSprite;
            }
        }
        else if (absY > absX + 0.1f)
        {
            // 垂直移动为主（上或下）
            if (direction.y > 0)
            {
                // 向上移动
                if (backSprite != null)
                    spriteRenderer.sprite = backSprite;
            }
            else if (direction.y < 0)
            {
                // 向下移动
                if (frontSprite != null)
                    spriteRenderer.sprite = frontSprite;
            }
        }
        // 如果absX和absY非常接近（对角线移动），根据Y方向优先判断上下
        else if (absY > 0.01f)
        {
            if (direction.y > 0)
            {
                if (backSprite != null)
                    spriteRenderer.sprite = backSprite;
            }
            else
            {
                if (frontSprite != null)
                    spriteRenderer.sprite = frontSprite;
            }
        }
        // 如果只有X方向有值
        else if (absX > 0.01f)
        {
            if (direction.x > 0)
            {
                if (rightSprite != null)
                    spriteRenderer.sprite = rightSprite;
            }
            else
            {
                if (leftSprite != null)
                    spriteRenderer.sprite = leftSprite;
            }
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

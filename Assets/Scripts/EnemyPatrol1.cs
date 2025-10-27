using UnityEngine;

public class EnemyPatrol1 : MonoBehaviour
{
    public Transform[] patrolPoints; // 巡逻点
    public float moveSpeed = 2f;
    private int currentPointIndex = 0;

    public EnemyFieldOfView fieldOfView;
    public PlayerController playerController; // 玩家对象
    public float shootingCooldown = 1.5f; // 射击间隔
    private float lastShootTime = 0;
    public GameObject bulletPrefab; // 子弹预制体
    public Transform firePoint; // 发射子弹的位置
    public Sprite deadSprite; // 敌人死亡后的图片
    AudioSource se;
    public AudioClip shotSE;

    private bool chasingPlayer = false; // 是否在追击玩家
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;

    public float gameOverTime = 2.0f; // 游戏结束延迟时间
    private float playerVisibleTimer = 0.0f; // 玩家可见时间

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
		se = GetComponent<AudioSource>(); //SE再生用
	}

    void Update()
    {
        if (playerController.transform == null) return;

        if (PlayerInSight())
        {
            chasingPlayer = true;
            ChasePlayer();

            playerVisibleTimer += Time.deltaTime;
            //Debug.Log("Player visible for: " + playerVisibleTimer + " seconds");
            if (playerVisibleTimer >= gameOverTime)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
            }
        }
        else
        {
            playerVisibleTimer = 0.0f;
            chasingPlayer = false;
            Patrol();
        }
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

        /*if (Time.time > lastShootTime + shootingCooldown)
        {
            Shoot();
            lastShootTime = Time.time;
        }*/
    }

    private bool PlayerInSight()
    {
        // プレイヤーが変装中なら、即座に「見えていない」ことにして処理を終了する
        if (playerController.IsDisguised)
        {
            return false;
        }

        // player変数をplayerController.transformに置き換えるのを忘れないように
        if (playerController == null || fieldOfView == null) return false;

        Vector2 directionToPlayer = (playerController.transform.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, playerController.transform.position);

        if (distanceToPlayer > fieldOfView.viewRadius) return false;
        float angleToPlayer = Vector2.Angle(transform.up, directionToPlayer);
        if (angleToPlayer > fieldOfView.viewAngle / 2) return false;

        return true;
    }

    private void AdjustRotation(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        Debug.Log("Enemy Rotation Angle: " + angle);
        if (fieldOfView != null)
        {
            fieldOfView.transform.rotation = transform.rotation;
        }
    }

    /*private void Shoot()
    {
        se.PlayOneShot(shotSE);
        //GameObject bullet = Instantiate(Resources.Load<GameObject>("Bullet"), transform.position, Quaternion.identity);
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        bullet.GetComponent<Bullet>().SetDirection((player.position - transform.position).normalized);
    }*/

    public void Die()
    {
        if (isDead) return; // 避免重复调用

        isDead = true;

        // 替换死亡图片
        if (spriteRenderer != null && deadSprite != null)
        {
            spriteRenderer.sprite = deadSprite;
        }

        // 禁用巡逻 & 视野
        this.enabled = false; // 停止 EnemyPatrol 逻辑
        if (fieldOfView != null)
        {
            fieldOfView.gameObject.SetActive(false);
        }

        // 关闭刺杀提示（确保 GameObject 存在）
        Transform assassinationPrompt = transform.Find("AssassinationPrompt");
        if (assassinationPrompt != null)
        {
            Destroy(assassinationPrompt.gameObject);
        }

        // 关闭碰撞（修正错误）
        Collider2D enemyCollider = GetComponent<Collider2D>();
        Collider2D playerCollider = playerController.GetComponent<Collider2D>();

        if (enemyCollider != null) enemyCollider.enabled = false; // 直接禁用敌人碰撞
        if (playerCollider != null && enemyCollider != null)
        {
            Physics2D.IgnoreCollision(enemyCollider, playerCollider, true); // 忽略碰撞
        }
    }

}

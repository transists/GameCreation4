using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightSwing : MonoBehaviour
{
    [Header("摆动角度范围（例如左右各30度）")]
    public float swingAngle = 30f;

    [Header("摆动速度（越大越快）")]
    public float speed = 1f;

    [Header("玩家检测设置")]
    [Tooltip("检测范围（半径）")]
    public float detectionRadius = 5f;
    [Tooltip("检测角度（灯光覆盖的角度）")]
    public float detectionAngle = 90f;
    [Tooltip("障碍物遮罩（用于检测是否有墙壁阻挡）")]
    public LayerMask obstacleMask;

    [Header("敌人引用")]
    [Tooltip("受此灯光影响的敌人（可拖拽多个）")]
    public EnemyPatrol1[] affectedEnemies;

    private float startAngle;
    private PlayerController playerController;
    private bool wasPlayerInRange = false;

    [Header("角度偏移（用于修正照射方向）")]
    public float directionOffset = 0f;

    void Start()
    {
        // 记录初始角度
        startAngle = transform.eulerAngles.z;
        
        // 查找玩家
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerController = playerObj.GetComponent<PlayerController>();
        }
        
        // 如果没有手动指定敌人，尝试自动查找
        if (affectedEnemies == null || affectedEnemies.Length == 0)
        {
            affectedEnemies = FindObjectsOfType<EnemyPatrol1>();
        }
    }

    void Update()
    {
        // 使用正弦函数在[-1, 1]间周期变化
        float angleOffset = Mathf.Sin(Time.time * speed) * swingAngle;

        // 应用旋转（仅Z轴）
        transform.rotation = Quaternion.Euler(0, 0, startAngle + angleOffset);
        
        // 检测玩家是否在灯光范围内
        bool isPlayerInRange = IsPlayerInLightRange();
        
        // 检测玩家进入的瞬间（从不在范围内变为在范围内）
        if (isPlayerInRange && !wasPlayerInRange)
        {
            OnPlayerEnterRange();
        }
        
        wasPlayerInRange = isPlayerInRange;
    }
    
    /// <summary>
    /// 检测玩家是否在灯光的检测范围内
    /// </summary>
    private bool IsPlayerInLightRange()
    {
        if (playerController == null || playerController.transform == null) return false;
        
        Vector2 lightPos = transform.position;
        Vector2 playerPos = playerController.transform.position;
        Vector2 toPlayer = playerPos - lightPos;
        float distance = toPlayer.magnitude;
        
        // 距离检测
        if (distance > detectionRadius) return false;
        
        // 角度检测：检查玩家是否在灯光的当前朝向角度范围内
        // Vector2 lightForward = transform.up; // 灯光的前方方向
        // float angleToPlayer = Vector2.Angle(lightForward, toPlayer);
        Vector2 forward = Quaternion.Euler(0, 0, directionOffset) * transform.up;
        float angleToPlayer = Vector2.Angle(forward, toPlayer);
        if (angleToPlayer > detectionAngle * 0.5f) return false;
        
        // 障碍物检测：检查是否有墙壁阻挡
        RaycastHit2D hit = Physics2D.Raycast(lightPos, toPlayer.normalized, distance, obstacleMask);
        if (hit.collider != null) return false;
        
        return true;
    }
    
    /// <summary>
    /// 当玩家进入灯光范围时调用
    /// </summary>
    private void OnPlayerEnterRange()
    {
        if (affectedEnemies == null) return;
        
        // 通知所有受影响的敌人
        foreach (EnemyPatrol1 enemy in affectedEnemies)
        {
            if (enemy != null)
            {
                enemy.OnPlayerDetectedByLight();
            }
        }
    }
    
    /// <summary>
    /// 在Scene视图中绘制检测范围（用于调试）
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 绘制检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // 绘制检测角度
        Gizmos.color = Color.red;
        Vector2 forward = Quaternion.Euler(0, 0, directionOffset) * transform.up;
        Vector2 leftBound = Quaternion.Euler(0, 0, -detectionAngle * 0.5f) * forward;
        Vector2 rightBound = Quaternion.Euler(0, 0, detectionAngle * 0.5f) * forward;
        
        Gizmos.DrawRay(transform.position, leftBound * detectionRadius);
        Gizmos.DrawRay(transform.position, rightBound * detectionRadius);
    }
}

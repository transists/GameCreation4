using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightSwing : MonoBehaviour
{
    [Header("摆动角度范围（例如左右各30度）")]
    public float swingAngle = 30f;

    [Header("摆动速度（越大越快）")]
    public float speed = 1f;

    private float startAngle;

    void Start()
    {
        // 记录初始角度
        startAngle = transform.eulerAngles.z;
    }

    void Update()
    {
        // 使用正弦函数在[-1, 1]间周期变化
        float angleOffset = Mathf.Sin(Time.time * speed) * swingAngle;

        // 应用旋转（仅Z轴）
        transform.rotation = Quaternion.Euler(0, 0, startAngle + angleOffset);
    }
}

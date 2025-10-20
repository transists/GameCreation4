using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // --- ↓ここからグリッド移動用のコード ---
    public float moveSpeed = 5f; // 1マスを移動する速さ
    private bool isMoving = false; // 移動中かどうかのフラグ
    private Vector3 targetPosition; // 目標地点
    // --- ↑ここまでグリッド移動用のコード ---

    // Start is called before the first frame update
    void Start()
    {
        //rb = GetComponent<Rigidbody2D>(); // Rigidbody 2Dコンポーネントを取得
        // 現在位置から一番近いタイルの中心にスナップさせる
        float x = Mathf.Floor(transform.position.x) + 0.5f;
        float y = Mathf.Floor(transform.position.y) + 0.5f;
        transform.position = new Vector3(x, y, 0);

        // 移動の目標地点も初期化しておく
        targetPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        // --- ↓ここからグリッド移動用のコード ---
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
        // --- ↑ここまでグリッド移動用のコード ---
    }

   
}

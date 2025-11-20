using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WallAlgorism : MonoBehaviour
{
    [Header("押し戻し設定")]
    [Tooltip("少し手前で止めるための余白")]
    public float skin = 0.01f;
    [Tooltip("1フレーム内に何度まで重なり解消を繰り返すか")]
    [Range(1, 6)] public int maxIterations = 3;

    [Header("検出設定")]
    public LayerMask wallsMask;     // 壁のレイヤー（IsTrigger）
    public bool drawNormals = false;

    Collider2D myCol;
    readonly HashSet<Collider2D> overlaps = new HashSet<Collider2D>();
    readonly List<Collider2D> tmpList = new List<Collider2D>(16);


    // Start is called before the first frame update
    void Start()
    {
        myCol = GetComponent<Collider2D>();
        var rb = GetComponent<Rigidbody2D>();
        if (rb) { rb.isKinematic = true; rb.gravityScale = 0; }
    }

    // 壁トリガーに入退場した相手を記録
    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsWall(other)) overlaps.Add(other);
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (overlaps.Contains(other)) overlaps.Remove(other);
    }

    // Update is called once per frame
    void Update()
    {
        if (overlaps.Count == 0) return;

        // 反復して全ての重なりを解消
        for (int iter = 0; iter < maxIterations; iter++)
        {
            bool anyResolved = false;
            tmpList.Clear();
            tmpList.AddRange(overlaps);

            foreach (var other in tmpList)
            {
                if (other == null) { overlaps.Remove(other); continue; }
                if (!IsWall(other)) { overlaps.Remove(other); continue; }

                // 最小分離ベクトル（MTV）を計算
                ColliderDistance2D d = Physics2D.Distance(myCol, other);
                if (!d.isOverlapped) { overlaps.Remove(other); continue; }

                // d.normal は myCol → other の法線（外向き）
                // 距離 d.distance は重なり時は負。押し戻し量 = normal * (-distance + skin)
                Vector2 mtv = d.normal * (-d.distance + skin);

                // 一気に大きく動くと別壁にめりこむので、Transformで加算＆次の反復で解消
                transform.position += (Vector3)mtv;
                anyResolved = true;

                if (drawNormals)
                {
                    Debug.DrawLine(d.pointA, d.pointA + d.normal * 0.5f, Color.cyan, 0f, false);
                }
            }

            if (!anyResolved) break; // もう重なっていない
            // 物理とTransformのズレを同期（安全策）
            Physics2D.SyncTransforms();
        }
    }

    bool IsWall(Collider2D col)
    {
        return ((1 << col.gameObject.layer) & wallsMask) != 0 && col.isTrigger;
    }
}

using UnityEngine;

/// <summary>
/// 矩形＋前方突起だけで探知するFOV。見た目メッシュは作らず、Gizmosで可視化。
/// 敵の前＝transform.up（このTransformの回転で方向が決まる）
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EnemyFieldOfView2 : MonoBehaviour
{
    [Header("矩形FOV（タイル基準）")]
    public float tileSize = 1f;
    public int forwardTiles = 6;   // 前方に6マス
    public int sideTiles = 2;      // 左右に2マス（幅は 2*side+1 マス相当）
    [Header("遮蔽（壁）")]
    public LayerMask obstacleMask;

    private Mesh mesh;

    /// <summary>ワールド座標の点が探知範囲に入っているか？</summary>

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        RebuildMesh();
    }

    void LateUpdate()
    {
        
    }

    /// <summary>敵本体から毎フレ呼ぶ：位置と前方を合わせる</summary>
    public void SetPose(Vector2 pos, Vector2 forward)
    {
        transform.SetPositionAndRotation(
            pos,
            Quaternion.FromToRotation(Vector2.up, forward.normalized)
        );
    }

    /// <summary>矩形FOV内判定（原点origin、前方forward、ターゲットtarget）</summary>
    public bool Contains(Vector2 origin, Vector2 forward, Vector2 target, float padding = 0f)
    {
        Vector2 f = forward.normalized;
        Vector2 r = new Vector2(f.y, -f.x); // 右向き（up→right）

        Vector2 to = target - origin;
        float z = Vector2.Dot(to, f);   // 前後
        float x = Vector2.Dot(to, r);   // 左右

        float depth = forwardTiles * tileSize + padding;
        float halfWidth = (sideTiles + 0.5f) * tileSize + padding; // 中央列も含めて少し余裕

        if (z < 0f || z > depth) return false;          // 後ろ/遠すぎ
        if (Mathf.Abs(x) > halfWidth) return false;     // 横にはみ出し

        // 遮蔽チェック（壁に遮られていないか）
        RaycastHit2D hit = Physics2D.Raycast(origin, to.normalized, to.magnitude, obstacleMask);
        if (hit.collider != null) return false;

        return true;
    }

    // 矩形の簡易メッシュ（視覚化用）
    public void RebuildMesh()
    {
        float depth = forwardTiles * tileSize;
        float halfWidth = (sideTiles + 0.5f) * tileSize;

        Vector3 bl = new Vector3(-halfWidth, 0f, 0f);
        Vector3 br = new Vector3(+halfWidth, 0f, 0f);
        Vector3 tl = new Vector3(-halfWidth, depth, 0f);
        Vector3 tr = new Vector3(+halfWidth, depth, 0f);

        mesh.Clear();
        mesh.vertices = new Vector3[] { bl, br, tr, tl };
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
    }

    void OnValidate()
    {
        if (mesh != null) RebuildMesh();
    }

    void OnDrawGizmosSelected()
    {
        float depth = forwardTiles * tileSize;
        float halfWidth = (sideTiles + 0.5f) * tileSize;

        var rot = transform.rotation;
        var pos = transform.position;

        Vector3 f = rot * Vector3.up;
        Vector3 r = rot * Vector3.right;

        Vector3 bl = pos + (-halfWidth) * r + 0f * f;
        Vector3 br = pos + (+halfWidth) * r + 0f * f;
        Vector3 tl = pos + (-halfWidth) * r + depth * f;
        Vector3 tr = pos + (+halfWidth) * r + depth * f;

        Gizmos.color = new Color(0f, 1f, 0.2f, 0.5f);
        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }
}
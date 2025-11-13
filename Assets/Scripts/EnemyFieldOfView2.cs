using UnityEngine;

/// <summary>
/// 矩形＋前方突起だけで探知するFOV。見た目メッシュは作らず、Gizmosで可視化。
/// 敵の前＝transform.up（このTransformの回転で方向が決まる）
/// </summary>
public class EnemyFieldOfView2 : MonoBehaviour
{
    [Header("タイル基準サイズ")]
    [Tooltip("1タイルのワールドサイズ（通常は1）")]
    public float tileSize = 1f;

    [Header("探知矩形サイズ（タイル数）")]
    [Tooltip("本体の矩形（幅,高さ）")]
    public Vector2Int mainTiles = new Vector2Int(8, 8);
    [Tooltip("前方の突起（幅,奥行） 例: (1,1) = 前1マスの正方形")]
    public Vector2Int forwardTiles = new Vector2Int(3, 1);

    [Header("遮蔽物オプション")]
    [Tooltip("壁で視線が遮られたら不可視にする")]
    public bool clipByObstacles = true;
    public LayerMask obstacleMask;

    /// <summary>ワールド座標の点が探知範囲に入っているか？</summary>

    void Start()
    {
        
    }

    void LateUpdate()
    {
        
    }

    /// <summary>ワールド座標の点が探知範囲に入っているか？</summary>
    public bool ContainsPoint(Vector2 worldPos)
    {
        // ローカル座標へ（このTransformのupが“前”）
        Vector2 p = transform.InverseTransformPoint(worldPos);

        // タイル → ワールド寸法
        Vector2 mainSize = new Vector2(mainTiles.x * tileSize, mainTiles.y * tileSize);
        Vector2 forwardSize = new Vector2(forwardTiles.x * tileSize, forwardTiles.y * tileSize);

        // 本体矩形：中心原点
        Vector2 halfMain = mainSize * 0.5f;
        bool inMain =
            Mathf.Abs(p.x) <= halfMain.x &&
            Mathf.Abs(p.y) <= halfMain.y;

        // 前方突起：中心を+Y（前）へオフセット
        Vector2 halfFwd = forwardSize * 0.5f;
        float fwdCenterY = halfMain.y + halfFwd.y;
        bool inForward =
            Mathf.Abs(p.x) <= halfFwd.x &&
            p.y >= (fwdCenterY - halfFwd.y) && p.y <= (fwdCenterY + halfFwd.y);

        if (!(inMain || inForward)) return false;

        // 壁で遮る
        if (clipByObstacles)
        {
            Vector2 origin = transform.position;
            Vector2 toP = worldPos - origin;
            if (Physics2D.Raycast(origin, toP.normalized, toP.magnitude, obstacleMask))
                return false;
        }
        return true;
    }

#if UNITY_EDITOR
    // Sceneビューでの可視化（選択時）
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.35f);

        Vector2 mainSize = new Vector2(mainTiles.x * tileSize, mainTiles.y * tileSize);
        Vector2 forwardSize = new Vector2(forwardTiles.x * tileSize, forwardTiles.y * tileSize);

        // このTransformの回転・位置を反映
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        // 本体矩形（中心は原点）
        Gizmos.DrawCube(Vector3.zero, new Vector3(mainSize.x, mainSize.y, 0.01f));

        // 前方突起（+Yへ）
        float fwdCenterY = (mainSize.y * 0.5f) + (forwardSize.y * 0.5f);
        Gizmos.DrawCube(new Vector3(0f, fwdCenterY, 0f), new Vector3(forwardSize.x, forwardSize.y, 0.01f));

        Gizmos.matrix = Matrix4x4.identity;
    }
#endif

    // 便利ユーティリティ（任意）
    public void SetTiles(Vector2Int main, Vector2Int forward)
    {
        mainTiles = new Vector2Int(Mathf.Max(1, main.x), Mathf.Max(1, main.y));
        forwardTiles = new Vector2Int(Mathf.Max(0, forward.x), Mathf.Max(0, forward.y));
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Key : MonoBehaviour
{
    [Tooltip("対応させたい壁ID。未指定なら targetWall を使う")]
    public string targetWallId = "A";

    [Tooltip("個別に1枚の壁を直接指定したい時に使う")]
    public LockingWall targetWall;

    public AudioClip pickSE;
    public ParticleSystem pickFX;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Player判定はタグでもコンポーネントでもOK
        if (!other.CompareTag("Player") && !other.GetComponent<PlayerController>()) return;

        // 演出
        if (pickFX) Instantiate(pickFX, transform.position, Quaternion.identity);
        if (pickSE) AudioSource.PlayClipAtPoint(pickSE, transform.position);

        // 壁解除
        if (targetWall)
        {
            targetWall.Unlock();
        }
        else
        {
            // 同じIDの壁を全部解除（複数枚対応）
            var walls = FindObjectsOfType<LockingWall>(true)
                        .Where(w => w.wallId == targetWallId);
            foreach (var w in walls) w.Unlock();
        }

        Destroy(gameObject); // 鍵を消す
    }
}

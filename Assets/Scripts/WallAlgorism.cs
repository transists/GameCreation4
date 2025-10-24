using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WallAlgorism : MonoBehaviour
{
    [Header("�����߂��ݒ�")]
    [Tooltip("������O�Ŏ~�߂邽�߂̗]��")]
    public float skin = 0.01f;
    [Tooltip("1�t���[�����ɉ��x�܂ŏd�Ȃ�������J��Ԃ���")]
    [Range(1, 6)] public int maxIterations = 3;

    [Header("���o�ݒ�")]
    public LayerMask wallsMask;     // �ǂ̃��C���[�iIsTrigger�j
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

    // �ǃg���K�[�ɓ��ޏꂵ��������L�^
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

        // �������đS�Ă̏d�Ȃ������
        for (int iter = 0; iter < maxIterations; iter++)
        {
            bool anyResolved = false;
            tmpList.Clear();
            tmpList.AddRange(overlaps);

            foreach (var other in tmpList)
            {
                if (other == null) { overlaps.Remove(other); continue; }
                if (!IsWall(other)) { overlaps.Remove(other); continue; }

                // �ŏ������x�N�g���iMTV�j���v�Z
                ColliderDistance2D d = Physics2D.Distance(myCol, other);
                if (!d.isOverlapped) { overlaps.Remove(other); continue; }

                // d.normal �� myCol �� other �̖@���i�O�����j
                // ���� d.distance �͏d�Ȃ莞�͕��B�����߂��� = normal * (-distance + skin)
                Vector2 mtv = d.normal * (-d.distance + skin);

                // ��C�ɑ傫�������ƕʕǂɂ߂肱�ނ̂ŁATransform�ŉ��Z�����̔����ŉ���
                transform.position += (Vector3)mtv;
                anyResolved = true;

                if (drawNormals)
                {
                    Debug.DrawLine(d.pointA, d.pointA + d.normal * 0.5f, Color.cyan, 0f, false);
                }
            }

            if (!anyResolved) break; // �����d�Ȃ��Ă��Ȃ�
            // ������Transform�̃Y���𓯊��i���S��j
            Physics2D.SyncTransforms();
        }
    }

    bool IsWall(Collider2D col)
    {
        return ((1 << col.gameObject.layer) & wallsMask) != 0 && col.isTrigger;
    }
}

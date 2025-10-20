using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // --- ����������O���b�h�ړ��p�̃R�[�h ---
    public float moveSpeed = 5f; // 1�}�X���ړ����鑬��
    private bool isMoving = false; // �ړ������ǂ����̃t���O
    private Vector3 targetPosition; // �ڕW�n�_
    // --- �������܂ŃO���b�h�ړ��p�̃R�[�h ---

    // Start is called before the first frame update
    void Start()
    {
        //rb = GetComponent<Rigidbody2D>(); // Rigidbody 2D�R���|�[�l���g���擾
        // ���݈ʒu�����ԋ߂��^�C���̒��S�ɃX�i�b�v������
        float x = Mathf.Floor(transform.position.x) + 0.5f;
        float y = Mathf.Floor(transform.position.y) + 0.5f;
        transform.position = new Vector3(x, y, 0);

        // �ړ��̖ڕW�n�_�����������Ă���
        targetPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        // --- ����������O���b�h�ړ��p�̃R�[�h ---
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
        // --- �������܂ŃO���b�h�ړ��p�̃R�[�h ---
    }

   
}

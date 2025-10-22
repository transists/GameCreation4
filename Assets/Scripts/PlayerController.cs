using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool IsDisguised { get; private set; } = false;
    [Header("�}�b�v���")]
    public LayerMask wallLayer; // �ǃ��C���[���C���X�y�N�^�[����ݒ�
    private bool canUseDisguise = true; // �ϑ�����x�����g����悤�ɂ��邽�߂̃t���O
    public Color disguisedColor = Color.cyan; // �ϑ����̐F�i�C���X�y�N�^�[�ŕύX�\�j
    public float disguiseDuration = 10f; // �ϑ����Ă��鎞��
    private Color originalColor; // ���̐F��ۑ�����ϐ�
    private SpriteRenderer spriteRenderer;

    // --- ����������O���b�h�ړ��p�̃R�[�h ---
    public float moveSpeed = 5f; // 1�}�X���ړ����鑬��
    private bool isMoving = false; // �ړ������ǂ����̃t���O
    private Vector3 targetPosition; // �ڕW�n�_
    // --- �������܂ŃO���b�h�ړ��p�̃R�[�h ---

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color; // �J�n���̐F���L��
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
        // E�L�[����������ϑ�����
        if (canUseDisguise && Input.GetKeyDown(KeyCode.E))
        {
            Disguise();
        }

        HandleMovement();

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

    // �ړ��̓��͂Ɣ�����s�����\�b�h
    private void HandleMovement()
    {
        if (isMoving) return;

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 potentialTargetPosition = transform.position;
        bool inputReceived = false;

        if (Mathf.Abs(horizontalInput) > 0.5f)
        {
            potentialTargetPosition += new Vector3(Mathf.Sign(horizontalInput), 0, 0);
            inputReceived = true;
        }
        else if (Mathf.Abs(verticalInput) > 0.5f)
        {
            potentialTargetPosition += new Vector3(0, Mathf.Sign(verticalInput), 0);
            inputReceived = true;
        }

        if (inputReceived && IsValidMove(potentialTargetPosition))
        {
            targetPosition = potentialTargetPosition;
            isMoving = true;
        }
    }

    private bool IsValidMove(Vector3 targetPos)
    {
        // ���@1�F�^�C���}�b�v�Ŕ��肷��ꍇ
        // Vector3Int targetCell = wallTilemap.WorldToCell(targetPos);
        // if (wallTilemap.HasTile(targetCell))
        // {
        //     return false; // �ǂ�����̂ňړ��s��
        // }

        // ���@2�F���C���[�Ŕ��肷��ꍇ
        Collider2D hit = Physics2D.OverlapCircle(targetPos, 0.2f, wallLayer);
        if (hit != null)
        {
            return false; // �ǂ�����̂ňړ��s��
        }

        // �ǂ̃`�F�b�N�ɂ�����������Ȃ���Έړ��\
        return true;
    }

    private void Disguise()
    {
        // �ϑ���Ԃɂ���
        IsDisguised = true;

        // �����g���Ȃ��悤�Ƀt���O��false�ɂ���
        canUseDisguise = false;

        // �����ڂ�ς���i��F�F��ς���j
        if (spriteRenderer != null)
        {
            spriteRenderer.color = disguisedColor;
        }

        StartCoroutine(DisguiseTimerCoroutine());
        Debug.Log("�ϑ������I 10�b��ɉ�������܂��B");
    }

    // 10�b�҂��Ă���ϑ��������Ăяo���R���[�`��
    private System.Collections.IEnumerator DisguiseTimerCoroutine()
    {
        // disguiseDuration�Ŏw�肵���b�������҂�
        yield return new WaitForSeconds(disguiseDuration);

        // ���Ԃ�������ϑ��������\�b�h���Ă�
        RemoveDisguise();
    }

    // �ϑ����������郁�\�b�h
    private void RemoveDisguise()
    {
        IsDisguised = false;

        // �F�����̐F�ɖ߂�
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        Debug.Log("�ϑ����������ꂽ�I");
    }
}

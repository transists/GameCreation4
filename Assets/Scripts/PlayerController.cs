using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool IsDisguised { get; private set; } = false;
    [Header("マップ情報")]
    public LayerMask wallLayer; // 壁レイヤーをインスペクターから設定
    private bool canUseDisguise = true; // 変装が一度だけ使えるようにするためのフラグ
    public Color disguisedColor = Color.cyan; // 変装中の色（インスペクターで変更可能）
    public float disguiseDuration = 10f; // 変装している時間
    private Color[] originalColors; // 複数SR分
    private SpriteRenderer spriteRenderer;

    // --- ↓ここからグリッド移動用のコード ---
    public float moveSpeed = 2.5f; // 1マスを移動する速さ
    //private bool isMoving = false; // 移動中かどうかのフラグ
    //private Vector3 targetPosition; // 目標地点
    private Rigidbody2D rb;
    private Vector2 moveInput;
    // --- ↑ここまでグリッド移動用のコード ---
    [Header("見た目の出力ターゲット")]
    [SerializeField] private SpriteRenderer srBase;    // Player/SR_Base をドラッグ
    [SerializeField] private SpriteRenderer srLitOnly; // Player/SR_LitOnly をドラッグ
    [SerializeField] private bool forceApplyInLateUpdate = true;
    [Header("見た目：向き別スプライト")]
    public Sprite frontSprite;        // 正面（デフォルト＆↓）
    public Sprite backSprite;         // 後ろ（↑）
    public Sprite leftSprite;         // 左（←）
    public Sprite rightSprite;        // 右（→）
    [Header("見た目：向き別スプライト（変装）")]
    public Sprite disguiseFrontSprite;
    public Sprite disguiseBackSprite;
    public Sprite disguiseLeftSprite;
    public Sprite disguiseRightSprite;
    [Tooltip("向き切替のデッドゾーン（小さすぎる上下入力は無視）")]
    public float facingDeadZone = 0.1f;
    private enum Facing { Front, Back, Left, Right }
    private Facing lastFacing = Facing.Front;   // 初期は正面

    // === 足音/SE ===
    [Header("サウンド：足音・SE")]
    public AudioClip footstepLoopNormal;      // 通常移動の足音（継ぎ目のないループ素材）
    public AudioClip footstepLoopDetected;    // 検知中の足音（テンポ強め/違う質感）
    [Range(0f, 1f)] public float footstepVolume = 0.6f;
    [Tooltip("移動とみなす速度しきい値（これ未満で足音停止）")]
    public float moveSfxThreshold = 0.05f;

    public AudioClip disguiseSE;              // 変装時のワンショットSE
    [Range(0f, 1f)] public float disguiseVolume = 1.0f;

    // 内部用：足音再生用のAudioSource（BGM等と混ざらない独立チャンネル）
    private AudioSource footstepSource;
    private bool footIsPlaying = false;
    private bool lastFootIsDetected = false;  // 直前が検知足音かどうか

    [Header("検知タイマー")]
    [Tooltip("スポットライトに入るたび延長される検知時間（秒）")]
    public float detectionExtendSeconds = 10f;

    [SerializeField] private float detectionTimeRemaining = 0f; // 検知残り時間（秒）
    public bool IsDetectedNow => detectionTimeRemaining > 0f;   // 外部から読みやすく
    // 検知中の速度倍率：通常は detectedSpeedMultiplier を使うが、
    // 敵側が倍率を提示してきた場合は“期限付きオーバーライド”にできる
    private float detectedMultiplierOverride = 0f;
    private float overrideTimeRemaining = 0f;   // オーバーライド有効残り秒

    [Header("検知時のスピード設定")]
    [Tooltip("敵に見つかっている間の速度倍率")]
    public float detectedSpeedMultiplier = 1.6f;
    [Tooltip("速度の補間係数（大きいほど素早く目標速度へ）")]
    public float speedLerp = 12f;

    [Header("サウンド：検知状態ループ")]
    public AudioClip detectedStateLoop;          // 検知中に鳴らし続けるループSE
    [Range(0f, 1f)] public float detectedStateVolume = 0.7f;
    public float detectedFadeSeconds = 0.15f;    // フェードIN/OUT時間

    private AudioSource detectedStateSource;
    private bool detectedLoopPlaying = false;

    [Header("ゴール制限")]
    [Tooltip("発見後ゴール不可の秒数")]
    public float goalLockSeconds = 10f;

    private float goalLockTimer = 0f;
    public bool CanGoal => goalLockTimer <= 0f;
    public float GoalLockRemaining => Mathf.Max(0f, goalLockTimer);
    // 内部状態
    private bool isDetected = false;
    public float currentSpeed;

    // 追加: デバッグ補助
    [SerializeField] private bool debugLogFacing = false;
    [SerializeField] private bool forceApplyFacingInLateUpdate = true;

    // Start is called before the first frame update
    void Start()
    {
        // 現在位置から一番近いタイルの中心にスナップさせる
        float x = Mathf.Floor(transform.position.x) + 0.5f;
        float y = Mathf.Floor(transform.position.y) + 0.5f;
        transform.position = new Vector3(x, y, 0);

        currentSpeed = moveSpeed; // 現在速度を基準速度から開始
        ApplyFacingSprite(lastFacing); // 初期表示
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // 自動補助：未割り当てなら名前で探索
        if (!srBase || !srLitOnly)
        {
            var srs = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                var n = sr.name.ToLower();
                if (!srBase && n.Contains("base")) srBase = sr;
                if (!srLitOnly && n.Contains("lit")) srLitOnly = sr;
            }
        }

        // 子に余計なSpriteRendererが残っていないか警告
        {
            var srs = GetComponentsInChildren<SpriteRenderer>(true);
            int extra = 0;
            foreach (var sr in srs)
                if (sr != srBase && sr != srLitOnly) extra++;
            if (extra > 0)
                Debug.LogWarning($"[Player] 子に余計なSpriteRendererが {extra} 個あります。二重描画の原因になります。", this);
        }

        if (!footstepSource)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.loop = true;
            footstepSource.playOnAwake = false;
            footstepSource.spatialBlend = 0f; // 2D
            footstepSource.volume = footstepVolume;
        }

        // 検知ループ用の専用AudioSourceを用意
        if (!detectedStateSource)
        {
            detectedStateSource = gameObject.AddComponent<AudioSource>();
            detectedStateSource.loop = true;
            detectedStateSource.playOnAwake = false;
            detectedStateSource.spatialBlend = 0f; // 2D
            detectedStateSource.volume = 0f;       // フェードIN前提
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Eキーを押したら変装する
        if (canUseDisguise && Input.GetKeyDown(KeyCode.E))
        {
            Disguise();
        }

        // 获取输入方向（键盘）
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveY).normalized;
        // 组合为向量并归一化
        UpdateFacingByInput(moveInput);
        // 検知タイマー減算
        if (detectionTimeRemaining > 0f)
        {
            detectionTimeRemaining -= Time.deltaTime;
            if (detectionTimeRemaining < 0f) detectionTimeRemaining = 0f;
        }
        if (overrideTimeRemaining > 0f)
        {
            overrideTimeRemaining -= Time.deltaTime;
            if (overrideTimeRemaining <= 0f)
            {
                overrideTimeRemaining = 0f;
                detectedMultiplierOverride = 0f; // 期限切れで無効化
            }
        }

        // 検知時の倍率（オーバーライドがあればそれを優先）
        float detectedMul = (overrideTimeRemaining > 0f && detectedMultiplierOverride > 0f)
                            ? detectedMultiplierOverride
                            : detectedSpeedMultiplier;

        // 速度ターゲット（← detectedMul を使うよう修正）
        float targetSpeed = IsDetectedNow ? moveSpeed * detectedMul : moveSpeed;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedLerp * Time.deltaTime);

        // ゴールロックもクランプ
        if (goalLockTimer > 0f)
        {
            goalLockTimer -= Time.deltaTime;
            if (goalLockTimer < 0f) goalLockTimer = 0f;
        }

        // 入力に応じて lastFacing を更新
        UpdateFacingByInput(moveInput);

        

        // ※ この時点で一度適用
        ApplyFacingSprite(lastFacing);

        UpdateFootstepAudio();  // ← これを最後の方で呼ぶだけ
        UpdateDetectedLoop();   // ← 毎フレーム呼ぶ
    }

    void FixedUpdate()
    {
        // 使用物理方式移动（会检测碰撞）
        rb.MovePosition(rb.position + moveInput * currentSpeed * Time.fixedDeltaTime);
    }

    void LateUpdate()
    {
        // 他のスクリプトに上書きされた場合の保険
        if (forceApplyFacingInLateUpdate)
            ApplyFacingSprite(lastFacing);
    }

    private void UpdateFootstepAudio()
    {
        // 実効移動速度（入力×currentSpeed）で判定
        float movingSpeed = (moveInput * currentSpeed).magnitude;

        bool isMoving = movingSpeed > moveSfxThreshold;
        bool wantDetectedFoot = false;

        // あなたのスクリプトに IsDetectedNow がある前提（なければ isDetected 等に置き換えてOK）
        wantDetectedFoot = IsDetectedNow;

        if (!isMoving)
        {
            // 停止：足音OFF
            if (footIsPlaying)
            {
                footstepSource.Stop();
                footIsPlaying = false;
            }
            return;
        }

        // ここまで来たら「動いている」
        // 使うべきループクリップを選択
        AudioClip desired = wantDetectedFoot ? footstepLoopDetected : footstepLoopNormal;

        // クリップ未設定なら何もしない
        if (!desired) return;

        // クリップ変更が必要か？
        bool needSwitch = (!footIsPlaying) || (footstepSource.clip != desired) || (lastFootIsDetected != wantDetectedFoot);

        if (needSwitch)
        {
            // スムーズ切替：一瞬止めて差し替え → 再生
            footstepSource.Stop();
            footstepSource.clip = desired;
            footstepSource.volume = footstepVolume;
            footstepSource.Play();

            footIsPlaying = true;
            lastFootIsDetected = wantDetectedFoot;
        }
        else
        {
            // 再生中なら音量を追従（Inspectorで変更したとき反映されるように）
            footstepSource.volume = footstepVolume;
        }
    }


    private void UpdateFacingByInput(Vector2 input)
    {
        float ax = Mathf.Abs(input.x);
        float ay = Mathf.Abs(input.y);
        if (ax < facingDeadZone && ay < facingDeadZone) return;

        if (ay >= ax) lastFacing = (input.y > 0f) ? Facing.Back : Facing.Front;
        else lastFacing = (input.x > 0f) ? Facing.Right : Facing.Left;

        if (debugLogFacing) Debug.Log($"[PC] Facing={lastFacing}");
    }

    private void ApplyFacingSprite(Facing f)
    {
        Sprite s;
        if (IsDisguised)
        {
            switch (f)
            {
                case Facing.Back: s = disguiseBackSprite ? disguiseBackSprite : backSprite; break;
                case Facing.Left: s = disguiseLeftSprite ? disguiseLeftSprite : leftSprite; break;
                case Facing.Right: s = disguiseRightSprite ? disguiseRightSprite : rightSprite; break;
                default: s = disguiseFrontSprite ? disguiseFrontSprite : frontSprite; break;
            }
        }
        else
        {
            switch (f)
            {
                case Facing.Back: s = backSprite; break;
                case Facing.Left: s = leftSprite; break;
                case Facing.Right: s = rightSprite; break;
                default: s = frontSprite; break;
            }
        }
        SetSpriteBoth(s);
    }

    // 2枚のSRに同じSpriteを必ず適用
    private void SetSpriteBoth(Sprite s)
    {
        if (!s) return;
        if (srBase) srBase.sprite = s;
        if (srLitOnly) srLitOnly.sprite = s;
    }

    

    private bool IsValidMove(Vector3 targetPos)
    {
        // 方法1：タイルマップで判定する場合
        // Vector3Int targetCell = wallTilemap.WorldToCell(targetPos);
        // if (wallTilemap.HasTile(targetCell))
        // {
        //     return false; // 壁があるので移動不可
        // }

        // 方法2：レイヤーで判定する場合
        Collider2D hit = Physics2D.OverlapCircle(targetPos, 0.2f, wallLayer);
        if (hit != null)
        {
            return false; // 壁があるので移動不可
        }

        // どのチェックにも引っかからなければ移動可能
        return true;
    }

    private void Disguise()
    {
        // 変装状態にする
        IsDisguised = true;

        // もう使えないようにフラグをfalseにする
        canUseDisguise = false;

        // 見た目を変える（例：色を変える）
        ApplyFacingSprite(lastFacing); // 即反映
        if (disguiseSE) AudioSource.PlayClipAtPoint(disguiseSE, transform.position, disguiseVolume);
        StartCoroutine(DisguiseTimerCoroutine());
        Debug.Log("変装した！ 10秒後に解除されます。");
    }

    // 10秒待ってから変装解除を呼び出すコルーチン
    private System.Collections.IEnumerator DisguiseTimerCoroutine()
    {
        // disguiseDurationで指定した秒数だけ待つ
        yield return new WaitForSeconds(disguiseDuration);

        // 時間が来たら変装解除メソッドを呼ぶ
        RemoveDisguise();
    }

    // 変装を解除するメソッド
    private void RemoveDisguise()
    {
        IsDisguised = false;

        ApplyFacingSprite(lastFacing); // 即反映
        Debug.Log("変装が解除された！");
    }

    // 旧仕様互換のための状態保持（立ち上がり検出用）
    private bool _prevDetectedSignal = false;

    /// <summary>
    /// 旧API互換：毎フレームの detected 信号を受け取るが、
    /// 「false→true の立ち上がり」の瞬間にだけ 10秒延長する。
    /// speedMultiplier は >0 のときだけ“一時上書き”として seconds の間だけ適用。
    /// </summary>
    public void SetDetected(bool detected, float speedMultiplier = -1f)
    {
        // 立ち上がり（false→true）の瞬間だけ延長
        if (detected && !_prevDetectedSignal)
        {
            if (speedMultiplier > 0f)
                AddDetectionTimeWithMultiplier(detectionExtendSeconds, speedMultiplier);
            else
                AddDetectionTime(detectionExtendSeconds);
        }

        // 次回比較用に保存（※速度ON/OFFの判定には IsDetectedNow を使います）
        _prevDetectedSignal = detected;
    }

    /// <summary>
    /// スポットライトに「入った瞬間」に敵側から呼ぶ。seconds 秒ぶん検知を延長。
    /// </summary>
    public void AddDetectionTime(float seconds)
    {
        bool wasDetected = IsDetectedNow;

        // 仕様：延長は「加算」。累積していく
        detectionTimeRemaining += Mathf.Max(0f, seconds);

        // 立ち上がり（未検知→検知）でゴールロックを開始（従来仕様維持）
        if (!wasDetected && IsDetectedNow)
        {
            goalLockTimer = goalLockSeconds;
        }
    }

    // 侵入瞬間で延長＋倍率一時上書き
    public void AddDetectionTimeWithMultiplier(float seconds, float multiplier)
    {
        AddDetectionTime(seconds);
        if (multiplier > 0f)
        {
            detectedMultiplierOverride = Mathf.Max(detectedMultiplierOverride, multiplier);
            overrideTimeRemaining += Mathf.Max(0f, seconds);
        }
    }

    private Coroutine detectedFadeCo;

    private void UpdateDetectedLoop()
    {
        bool wantPlay = IsDetectedNow;

        if (wantPlay && !detectedLoopPlaying)
        {
            StartDetectedLoop();
        }
        else if (!wantPlay && detectedLoopPlaying)
        {
            StopDetectedLoop();
        }

        // 追加の安全策：検知が切れているのに鳴っていたら確実に止める
        if (!wantPlay && detectedStateSource)
        {
            if (detectedStateSource.isPlaying && detectedStateSource.volume <= 0.001f)
                detectedStateSource.Stop();
        }
    }

    private void StartDetectedLoop()
    {
        if (!detectedStateLoop) return;

        detectedStateSource.clip = detectedStateLoop;
        detectedStateSource.volume = 0f;
        detectedStateSource.Play();
        detectedLoopPlaying = true;

        if (detectedFadeCo != null) StopCoroutine(detectedFadeCo);
        detectedFadeCo = StartCoroutine(FadeVolume(detectedStateSource, 0f, detectedStateVolume, detectedFadeSeconds));
    }

    private void StopDetectedLoop()
    {
        if (!detectedLoopPlaying) return;

        if (detectedFadeCo != null) StopCoroutine(detectedFadeCo);
        detectedFadeCo = StartCoroutine(FadeOutAndStop(detectedStateSource, detectedFadeSeconds));
        detectedLoopPlaying = false;
    }

    private System.Collections.IEnumerator FadeVolume(AudioSource src, float from, float to, float sec)
    {
        float t = 0f;
        src.volume = from;
        while (t < sec)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(from, to, t / sec);
            yield return null;
        }
        src.volume = to;
    }

    private System.Collections.IEnumerator FadeOutAndStop(AudioSource src, float sec)
    {
        float from = src.volume;
        float t = 0f;
        while (t < sec)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(from, 0f, t / sec);
            yield return null;
        }
        src.volume = 0f;
        src.Stop();
    }

    void OnDisable()
    {
        if (footstepSource) { footstepSource.Stop(); }
        if (detectedStateSource) { detectedStateSource.Stop(); }
        detectedLoopPlaying = false;
    }

    
}

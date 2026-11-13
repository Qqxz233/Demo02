using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("移动与惯性设置")]
    public float moveSpeed = 12f;
    [Tooltip("加速度：数值越大，起步越快 (推荐 15~30)")]
    public float acceleration = 20f;
    [Tooltip("减速度(水阻)：数值越小，松开按键后滑行越远 (推荐 2~5)")]
    public float deceleration = 3f;
    [HideInInspector] public float speedMultiplier = 1f;

    [Header("转向设置")]
    public Transform modelTransform;
    public float turnSpeed = 5f;
    public float modelRotationOffset = 0f;

    [Header("视觉与特效引用")]
    public GameObject waterWakeVFX;
    private ParticleSystem wakeParticleSystem;

    private PlayerInput playerInput;
    private Rigidbody rb;
    private Vector3 currentVelocity;

    // 击退硬直控制
    private float knockbackTimer = 0f;
    private float currentKnockbackDrag = 2f;

    // 视觉表现
    private TrailRenderer speedTrail;
    private Renderer[] allRenderers;
    private MaterialPropertyBlock mpb;
    private Color originalColor;
    private bool isCurrentlySlowed = false;

    // 【移动音效】
    private AudioSource moveAudioSource;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.drag = 0f;
            rb.isKinematic = false;
        }

        allRenderers = GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();

        if (allRenderers.Length > 0 && allRenderers[0].sharedMaterial != null)
        {
            originalColor = allRenderers[0].sharedMaterial.color;
        }
    }

    private void Start()
    {
        if (waterWakeVFX != null)
        {
            wakeParticleSystem = waterWakeVFX.GetComponent<ParticleSystem>();
            if (wakeParticleSystem != null) wakeParticleSystem.Stop();
        }

        // 初始化加速拖尾
        speedTrail = gameObject.AddComponent<TrailRenderer>();
        speedTrail.time = 0.2f;
        speedTrail.startWidth = 0.5f;
        speedTrail.endWidth = 0f;
        speedTrail.material = new Material(Shader.Find("Sprites/Default"));
        speedTrail.startColor = new Color(0.2f, 1f, 0.2f, 0.8f);
        speedTrail.endColor = new Color(0.2f, 1f, 0.2f, 0f);
        speedTrail.emitting = false;

        // ==========================================================
        // 【核心新增】：初始化移动音效 (极致性能优化方案)
        // ==========================================================
        moveAudioSource = gameObject.AddComponent<AudioSource>();
        moveAudioSource.playOnAwake = false;
        moveAudioSource.loop = true;         // 开启循环
        moveAudioSource.spatialBlend = 0f;   // 2D音效，防止3D距离计算消耗性能

        if (AudioManager.Instance != null)
        {
            AudioClip moveClip = AudioManager.Instance.GetSoundClip(SoundType.PlayerMove);
            if (moveClip != null)
            {
                moveAudioSource.clip = moveClip;
                moveAudioSource.volume = 0.6f; // 音量根据你的素材微调

                // 关键：先在静默状态下播放一帧，然后立刻 Pause
                // 这样音频缓冲通道就已经打通了，后续 UnPause 是零延迟的！
                moveAudioSource.Play();
                moveAudioSource.Pause();
            }
        }
    }

    public void ApplyKnockback(Vector3 force, float duration = 0.4f)
    {
        if (rb == null) return;

        knockbackTimer = duration;
        currentVelocity = Vector3.zero;

        rb.velocity = Vector3.zero;
        rb.AddForce(force, ForceMode.Impulse);
    }

    private void FixedUpdate()
    {
        // 1. 如果游戏不在游玩状态（例如暂停、选卡、死亡），强制停止
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            if (rb != null) rb.velocity = Vector3.zero;
            currentVelocity = Vector3.zero;
            UpdateWaterVFX(Vector2.zero);
            HandleMoveSound(false); // 停止声音
            return;
        }

        if (playerInput == null || rb == null) return;

        // 2. 击退硬直期间，不受控制且停止移动音效
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, Time.fixedDeltaTime * currentKnockbackDrag);
            UpdateWaterVFX(Vector2.zero);
            HandleMoveSound(false); // 击退时停止声音
            return;
        }

        // 3. 正常移动逻辑
        Vector2 input = playerInput.MoveDirection;
        bool hasInput = input.sqrMagnitude > 0.01f;

        Vector3 targetVelocity = new Vector3(input.x, 0, input.y) * moveSpeed * speedMultiplier;

        if (hasInput)
            currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        else
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, deceleration * Time.fixedDeltaTime);

        rb.velocity = new Vector3(currentVelocity.x, rb.velocity.y, currentVelocity.z);

        UpdateWaterVFX(input);

        // 船头平滑转向
        if (currentVelocity.sqrMagnitude > 0.5f && modelTransform != null)
        {
            Vector3 moveDir = currentVelocity.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(moveDir) * Quaternion.Euler(0, modelRotationOffset, 0);
            modelTransform.rotation = Quaternion.Slerp(modelTransform.rotation, targetRotation, Time.fixedDeltaTime * turnSpeed);
        }

        // 4. 控制移动音效
        HandleMoveSound(hasInput);
    }

    // ==========================================================
    // 【核心新增】：极速音频暂停/恢复逻辑
    // ==========================================================
    private void HandleMoveSound(bool isMovingNow)
    {
        if (moveAudioSource == null || moveAudioSource.clip == null) return;

        if (isMovingNow)
        {
            // 如果正在移动，且声音没在播，直接 UnPause（开销极小，绝不卡顿）
            if (!moveAudioSource.isPlaying)
                moveAudioSource.UnPause();
        }
        else
        {
            // 如果停下了，或者被击退了，直接 Pause（保留音频缓冲进度）
            if (moveAudioSource.isPlaying)
                moveAudioSource.Pause();
        }
    }

    private void UpdateWaterVFX(Vector2 input)
    {
        if (waterWakeVFX == null || wakeParticleSystem == null) return;
        waterWakeVFX.transform.position = transform.position;
        bool hasInput = input.sqrMagnitude > 0.01f;

        if (hasInput)
        {
            if (!waterWakeVFX.activeSelf) waterWakeVFX.SetActive(true);
            if (!wakeParticleSystem.isPlaying) wakeParticleSystem.Play();
            Vector3 moveDir = new Vector3(input.x, 0, input.y).normalized;
            Quaternion vfxRotation = Quaternion.LookRotation(moveDir);
            waterWakeVFX.transform.rotation = Quaternion.Slerp(waterWakeVFX.transform.rotation, vfxRotation, Time.deltaTime * 10f);
        }
        else
        {
            if (wakeParticleSystem.isPlaying) wakeParticleSystem.Stop();
        }
    }

    private void Update()
    {
        UpdateSpeedVisuals();
    }

    private void UpdateSpeedVisuals()
    {
        if (speedMultiplier > 1.01f)
        {
            speedTrail.emitting = true;
            if (isCurrentlySlowed) { ResetToOriginalColor(); isCurrentlySlowed = false; }
        }
        else if (speedMultiplier < 0.99f)
        {
            speedTrail.emitting = false;
            if (!isCurrentlySlowed)
            {
                ApplyColorToAllRenderers(new Color(0.5f, 0.8f, 1f));
                isCurrentlySlowed = true;
            }
        }
        else
        {
            speedTrail.emitting = false;
            if (isCurrentlySlowed)
            {
                ResetToOriginalColor();
                isCurrentlySlowed = false;
            }
        }
    }

    private void ApplyColorToAllRenderers(Color color)
    {
        foreach (var rend in allRenderers)
        {
            if (rend == null) continue;
            rend.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_Color", color);
            rend.SetPropertyBlock(mpb);
        }
    }

    private void ResetToOriginalColor()
    {
        ApplyColorToAllRenderers(originalColor);
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using Mathcalibur.Battle;
using UnityEngine;

public enum BattleAttackMotionType
{
    Light,
    Medium,
    Heavy
}

public enum AnimationPlaybackMode
{
    Trigger,
    PlayState
}

public enum EnemyHitPlaybackModeOverride
{
    UseManagerDefault,
    Trigger,
    PlayState
}

[System.Serializable]
public class AttackVfxEvent
{
    [Header("VFX")]
    public string eventName = "Slash";
    public GameObject prefab;

    [Tooltip("공격 애니메이션 시작 후 몇 초 뒤에 VFX를 생성할지")]
    [Min(0f)] public float delay = 0.2f;

    [Tooltip("비워두면 BattleAnimationManager의 기본 Player VFX Spawn Point를 사용")]
    public Transform spawnPointOverride;

    [Tooltip("0보다 크면 생성 후 해당 시간 뒤 자동 삭제. 0 이하이면 자동 삭제하지 않음")]
    public float lifeTime = 2f;

    [Header("Optional Transform Offset")]
    [Tooltip("스폰 포인트 기준 로컬 위치 보정값")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("스폰 포인트 회전 기준 추가 회전값")]
    public Vector3 rotationOffset = Vector3.zero;

    [Tooltip("생성된 VFX 크기 배율")]
    public Vector3 scaleMultiplier = Vector3.one;

    [Tooltip("true면 생성된 VFX를 스폰 포인트의 자식으로 붙임. 검기처럼 순간 생성되는 이펙트는 보통 false 권장")]
    public bool parentToSpawnPoint = false;
}

[System.Serializable]
public class HitReactionEvent
{
    public string eventName = "Hit";
    [Min(0f)] public float delay = 0.35f;
    public string enemyHitStateNameOverride;
    public string enemyHitTriggerNameOverride;
    public EnemyHitPlaybackModeOverride enemyHitPlaybackModeOverride = EnemyHitPlaybackModeOverride.UseManagerDefault;
    public GameObject hitVfxPrefab;
    public Transform hitVfxSpawnPointOverride;
    public float hitVfxLifeTime = 2f;
    public AudioClip hitSfx;
}

[System.Serializable]
public class AttackAnimationProfile
{
    [Header("Player Animation")]
    public string playerTriggerName = "LightAttack";

    [Header("Enemy Hit Timing")]
    public float[] enemyHitDelays = new float[] { 0.35f };

    [Header("Enemy Hit Reaction Events")]
    [Tooltip("비어 있으면 기존 Enemy Hit Delays + Hit VFX/SFX를 사용합니다.")]
    public HitReactionEvent[] hitReactionEvents;

    [Header("Attack VFX Events")]
    [Tooltip("공격 중 나올 VFX 목록. 약/중/강마다 다른 프리팹, 타이밍, 스폰 위치를 지정할 수 있음")]
    public AttackVfxEvent[] attackVfxEvents;

    [Header("Legacy Single Attack VFX")]
    [Tooltip("예전 방식 호환용. Attack VFX Events가 비어있을 때만 사용됨")]
    public GameObject attackVfxPrefab;
    public float attackVfxDelay = 0.2f;
    public float attackVfxLifeTime = 2f;

    [Header("Hit VFX")]
    public GameObject hitVfxPrefab;
    public float hitVfxLifeTime = 2f;

    [Header("SFX")]
    public AudioClip attackSfx;
    public AudioClip hitSfx;

    public AttackAnimationProfile()
    {
    }

    public AttackAnimationProfile(string triggerName, float[] hitDelays)
    {
        playerTriggerName = triggerName;
        enemyHitDelays = hitDelays;
    }
}

public class BattleAnimationManager : MonoBehaviour
{
    private enum TimelineEventType
    {
        AttackVfx,
        EnemyHit
    }

    private sealed class TimelineEvent
    {
        public float Time;
        public int Order;
        public TimelineEventType Type;
        public AttackVfxEvent AttackVfxEvent;
        public HitReactionEvent HitReactionEvent;

        public static TimelineEvent CreateAttackVfx(float time, AttackVfxEvent attackVfxEvent)
        {
            return new TimelineEvent
            {
                Time = time,
                Order = 0,
                Type = TimelineEventType.AttackVfx,
                AttackVfxEvent = attackVfxEvent
            };
        }

        public static TimelineEvent CreateEnemyHit(float time, HitReactionEvent hitReactionEvent = null)
        {
            return new TimelineEvent
            {
                Time = time,
                Order = 1,
                Type = TimelineEventType.EnemyHit,
                HitReactionEvent = hitReactionEvent
            };
        }
    }

    [Header("Animators")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Animator enemyAnimator;

    [Header("Default VFX Spawn Points")]
    [Tooltip("AttackVfxEvent의 Spawn Point Override가 비어있을 때 사용하는 기본 플레이어 VFX 위치")]
    [SerializeField] private Transform playerVfxSpawnPoint;

    [Tooltip("적 피격 VFX가 나올 위치")]
    [SerializeField] private Transform enemyHitVfxPoint;

    [Header("Optional Audio")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Attack Profiles")]
    [SerializeField]
    private AttackAnimationProfile lightAttack =
        new AttackAnimationProfile("LightAttack", new float[] { 0.35f });

    [SerializeField]
    private AttackAnimationProfile mediumAttack =
        new AttackAnimationProfile("MediumAttack", new float[] { 0.30f, 0.65f });

    [SerializeField]
    private AttackAnimationProfile heavyAttack =
        new AttackAnimationProfile("HeavyAttack", new float[] { 0.55f });

    [Header("Enemy Hit Animation")]
    [SerializeField] private string enemyHitStateName = "Hit";

    [Tooltip("true면 SetTrigger(\"Hit\") 사용, false면 Animator.Play(\"Hit\", 0, 0f) 사용")]
    [SerializeField] private bool useEnemyHitTrigger = false;

    [SerializeField] private string enemyHitTriggerName = "Hit";

    [Header("Enemy Attack Animation")]
    [SerializeField] private string enemyAttackTriggerName;
    [SerializeField] private string enemyAttackStateName;
    [SerializeField] private AnimationPlaybackMode enemyAttackPlaybackMode = AnimationPlaybackMode.Trigger;
    [SerializeField] private AudioClip enemyAttackStartSfx;
    [SerializeField] private GameObject enemyAttackVfxPrefab;
    [SerializeField] private Transform enemyAttackVfxSpawnPoint;
    [SerializeField, Min(0f)] private float playerHitDelay = 0.35f;
    [SerializeField, Min(0f)] private float enemyAttackFinishDelay = 0.35f;
    [SerializeField] private GameObject playerHitVfxPrefab;
    [SerializeField] private Transform playerHitVfxSpawnPoint;
    [SerializeField] private AudioClip playerHitSfx;

    [Header("Player Combat Mode Animation")]
    [SerializeField] private string playerDefenseModeBoolName;
    [SerializeField] private string playerCombatModeIntName;

    [Header("Player Hit Animation")]
    [SerializeField] private string playerAttackModeHitTriggerName;
    [SerializeField] private string playerAttackModeHitStateName;
    [SerializeField] private string playerDefenseModeHitTriggerName;
    [SerializeField] private string playerDefenseModeHitStateName;
    [SerializeField] private AnimationPlaybackMode playerHitPlaybackMode = AnimationPlaybackMode.Trigger;

    [Header("Enemy Death Animation")]
    [SerializeField] private string enemyDeathTriggerName;
    [SerializeField] private string enemyDeathStateName;
    [SerializeField] private AnimationPlaybackMode enemyDeathPlaybackMode = AnimationPlaybackMode.Trigger;
    [SerializeField, Min(0f)] private float enemyDeathDuration = 1f;
    [SerializeField] private AudioClip enemyDeathSfx;
    [SerializeField] private GameObject enemyDeathVfxPrefab;
    [SerializeField] private Transform enemyDeathVfxSpawnPoint;

    [Header("Damage Threshold")]
    [Tooltip("이 값 이하이면 약공격")]
    [SerializeField] private int lightMaxDamage = 50;

    [Tooltip("Light Max Damage 초과, 이 값 이하이면 중공격. 이 값 초과이면 강공격")]
    [SerializeField] private int mediumMaxDamage = 99;

    [Header("Temporary Test Damage")]
    [SerializeField] private bool useTestDamageOverride = false;
    [SerializeField] private int testDamage = 30;

    [Header("Debug Test")]
    [SerializeField] private bool enableKeyboardDebug = true;

    [Header("Playback Rule")]
    [SerializeField] private bool interruptCurrentAttack = true;

    private Coroutine currentAttackRoutine;
    private CombatMode currentCombatMode = CombatMode.Attack;
    private Animator defaultEnemyAnimator;
    private Transform defaultEnemyHitVfxPoint;
    private string defaultEnemyAttackTriggerName;
    private string defaultEnemyHitTriggerName;
    private string defaultEnemyDeathTriggerName;
    private bool defaultUseEnemyHitTrigger;
    private AnimationPlaybackMode defaultEnemyAttackPlaybackMode;
    private AnimationPlaybackMode defaultEnemyDeathPlaybackMode;
    private bool enemyInspectorBindingsCaptured;

    public bool IsPlayingAttack => currentAttackRoutine != null;

    private void Awake()
    {
        CaptureEnemyInspectorBindings();

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }
    }

    private void CaptureEnemyInspectorBindings()
    {
        if (enemyInspectorBindingsCaptured)
        {
            return;
        }

        defaultEnemyAnimator = enemyAnimator;
        defaultEnemyHitVfxPoint = enemyHitVfxPoint;
        defaultEnemyAttackTriggerName = enemyAttackTriggerName;
        defaultEnemyHitTriggerName = enemyHitTriggerName;
        defaultEnemyDeathTriggerName = enemyDeathTriggerName;
        defaultUseEnemyHitTrigger = useEnemyHitTrigger;
        defaultEnemyAttackPlaybackMode = enemyAttackPlaybackMode;
        defaultEnemyDeathPlaybackMode = enemyDeathPlaybackMode;
        enemyInspectorBindingsCaptured = true;
    }

    private void Update()
    {
        if (!enableKeyboardDebug)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayAttack(BattleAttackMotionType.Light);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayAttack(BattleAttackMotionType.Medium);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            PlayAttack(BattleAttackMotionType.Heavy);
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            PlayAttackByDamage(testDamage);
        }
    }

    public void PlayAttackByDamage(int calculatedDamage)
    {
        int damage = useTestDamageOverride ? testDamage : calculatedDamage;

        BattleAttackMotionType attackType = GetAttackTypeByDamage(damage);
        PlayAttack(attackType);
    }

    public IEnumerator PlayAttackByDamageRoutine(int calculatedDamage)
    {
        int damage = useTestDamageOverride ? testDamage : calculatedDamage;
        yield return PlayAttackRoutine(GetProfile(GetAttackTypeByDamage(damage)), true);
    }

    public void PlayAttack(BattleAttackMotionType attackType)
    {
        AttackAnimationProfile profile = GetProfile(attackType);

        if (profile == null)
        {
            Debug.LogWarning("Attack profile is null.");
            return;
        }

        if (playerAnimator == null)
        {
            Debug.LogWarning("Player Animator is not assigned.");
            return;
        }

        if (currentAttackRoutine != null)
        {
            if (!interruptCurrentAttack)
            {
                return;
            }

            StopCoroutine(currentAttackRoutine);
            currentAttackRoutine = null;
        }

        currentAttackRoutine = StartCoroutine(PlayAttackRoutine(profile, false));
    }

    public void StopCurrentAttack()
    {
        if (currentAttackRoutine == null)
            return;

        StopCoroutine(currentAttackRoutine);
        currentAttackRoutine = null;
    }

    public BattleAttackMotionType GetAttackTypeByDamage(int damage)
    {
        if (damage <= lightMaxDamage)
        {
            return BattleAttackMotionType.Light;
        }

        if (damage <= mediumMaxDamage)
        {
            return BattleAttackMotionType.Medium;
        }

        return BattleAttackMotionType.Heavy;
    }

    private IEnumerator PlayAttackRoutine(AttackAnimationProfile profile, bool waitOnly)
    {
        if (profile == null || playerAnimator == null)
        {
            yield break;
        }

        if (waitOnly && currentAttackRoutine != null)
        {
            if (interruptCurrentAttack)
            {
                StopCoroutine(currentAttackRoutine);
                currentAttackRoutine = null;
            }
            else
            {
                yield break;
            }
        }
        ResetPlayerAttackTriggers();

        if (!string.IsNullOrEmpty(profile.playerTriggerName))
        {
            playerAnimator.SetTrigger(profile.playerTriggerName);
        }
        PlaySfx(profile.attackSfx);

        List<TimelineEvent> timeline = BuildTimeline(profile);
        timeline.Sort(CompareTimelineEvents);

        float elapsed = 0f;

        for (int i = 0; i < timeline.Count; i++)
        {
            TimelineEvent timelineEvent = timeline[i];

            float targetTime = Mathf.Max(0f, timelineEvent.Time);
            float waitTime = Mathf.Max(0f, targetTime - elapsed);

            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
            }

            elapsed = targetTime;
            ExecuteTimelineEvent(profile, timelineEvent);
        }

        currentAttackRoutine = null;
    }

    private List<TimelineEvent> BuildTimeline(AttackAnimationProfile profile)
    {
        List<TimelineEvent> timeline = new List<TimelineEvent>();

        AddAttackVfxEvents(profile, timeline);
        AddEnemyHitEvents(profile, timeline);

        return timeline;
    }

    private void AddAttackVfxEvents(AttackAnimationProfile profile, List<TimelineEvent> timeline)
    {
        bool hasNewVfxEvents = false;

        if (profile.attackVfxEvents != null)
        {
            for (int i = 0; i < profile.attackVfxEvents.Length; i++)
            {
                AttackVfxEvent vfxEvent = profile.attackVfxEvents[i];

                if (vfxEvent == null || vfxEvent.prefab == null)
                    continue;

                hasNewVfxEvents = true;
                timeline.Add(TimelineEvent.CreateAttackVfx(vfxEvent.delay, vfxEvent));
            }
        }

        if (hasNewVfxEvents || profile.attackVfxPrefab == null)
            return;

        AttackVfxEvent legacyEvent = new AttackVfxEvent
        {
            eventName = "Legacy Attack VFX",
            prefab = profile.attackVfxPrefab,
            delay = profile.attackVfxDelay,
            spawnPointOverride = null,
            lifeTime = profile.attackVfxLifeTime,
            positionOffset = Vector3.zero,
            rotationOffset = Vector3.zero,
            scaleMultiplier = Vector3.one,
            parentToSpawnPoint = false
        };

        timeline.Add(TimelineEvent.CreateAttackVfx(legacyEvent.delay, legacyEvent));
    }

    private void AddEnemyHitEvents(AttackAnimationProfile profile, List<TimelineEvent> timeline)
    {
        if (profile.hitReactionEvents != null && profile.hitReactionEvents.Length > 0)
        {
            for (int i = 0; i < profile.hitReactionEvents.Length; i++)
            {
                HitReactionEvent hitEvent = profile.hitReactionEvents[i];
                if (hitEvent != null)
                {
                    timeline.Add(TimelineEvent.CreateEnemyHit(hitEvent.delay, hitEvent));
                }
            }
            return;
        }

        if (profile.enemyHitDelays == null)
            return;

        for (int i = 0; i < profile.enemyHitDelays.Length; i++)
        {
            timeline.Add(TimelineEvent.CreateEnemyHit(profile.enemyHitDelays[i]));
        }
    }

    private static int CompareTimelineEvents(TimelineEvent a, TimelineEvent b)
    {
        int timeCompare = a.Time.CompareTo(b.Time);
        if (timeCompare != 0)
        {
            return timeCompare;
        }

        return a.Order.CompareTo(b.Order);
    }

    private void ExecuteTimelineEvent(AttackAnimationProfile profile, TimelineEvent timelineEvent)
    {
        switch (timelineEvent.Type)
        {
            case TimelineEventType.AttackVfx:
                SpawnAttackVfx(timelineEvent.AttackVfxEvent);
                break;

            case TimelineEventType.EnemyHit:
                PlayEnemyHit(timelineEvent.HitReactionEvent);
                PlaySfx(timelineEvent.HitReactionEvent != null ? timelineEvent.HitReactionEvent.hitSfx : profile.hitSfx);
                SpawnVfx(
                    timelineEvent.HitReactionEvent != null ? timelineEvent.HitReactionEvent.hitVfxPrefab : profile.hitVfxPrefab,
                    timelineEvent.HitReactionEvent != null && timelineEvent.HitReactionEvent.hitVfxSpawnPointOverride != null ? timelineEvent.HitReactionEvent.hitVfxSpawnPointOverride : enemyHitVfxPoint,
                    timelineEvent.HitReactionEvent != null ? timelineEvent.HitReactionEvent.hitVfxLifeTime : profile.hitVfxLifeTime);
                break;
        }
    }

    private AttackAnimationProfile GetProfile(BattleAttackMotionType attackType)
    {
        switch (attackType)
        {
            case BattleAttackMotionType.Light:
                return lightAttack;

            case BattleAttackMotionType.Medium:
                return mediumAttack;

            case BattleAttackMotionType.Heavy:
                return heavyAttack;

            default:
                return lightAttack;
        }
    }

    private void PlayEnemyHit(HitReactionEvent hitEvent = null)
    {
        if (enemyAnimator == null)
        {
            Debug.LogWarning("Enemy Animator is not assigned.");
            return;
        }

        bool useTrigger = useEnemyHitTrigger;
        string triggerName = enemyHitTriggerName;
        string stateName = enemyHitStateName;

        if (hitEvent != null)
        {
            if (!string.IsNullOrEmpty(hitEvent.enemyHitTriggerNameOverride))
            {
                triggerName = hitEvent.enemyHitTriggerNameOverride;
            }

            if (!string.IsNullOrEmpty(hitEvent.enemyHitStateNameOverride))
            {
                stateName = hitEvent.enemyHitStateNameOverride;
            }

            if (hitEvent.enemyHitPlaybackModeOverride == EnemyHitPlaybackModeOverride.Trigger)
            {
                useTrigger = true;
            }
            else if (hitEvent.enemyHitPlaybackModeOverride == EnemyHitPlaybackModeOverride.PlayState)
            {
                useTrigger = false;
            }
        }

        if (useTrigger)
        {
            if (string.IsNullOrEmpty(triggerName)) return;
            enemyAnimator.ResetTrigger(triggerName);
            enemyAnimator.SetTrigger(triggerName);
        }
        else
        {
            if (string.IsNullOrEmpty(stateName)) return;
            enemyAnimator.Play(stateName, 0, 0f);
        }
    }


    public IEnumerator PlayEnemyAttackRoutine(Action onDamageTiming)
    {
        PlayAnimator(enemyAnimator, enemyAttackPlaybackMode, enemyAttackTriggerName, enemyAttackStateName);
        PlaySfx(enemyAttackStartSfx);
        SpawnVfx(enemyAttackVfxPrefab, enemyAttackVfxSpawnPoint);

        if (playerHitDelay > 0f)
        {
            yield return new WaitForSeconds(playerHitDelay);
        }

        onDamageTiming?.Invoke();
        PlayPlayerHit();
        PlaySfx(playerHitSfx);
        SpawnVfx(playerHitVfxPrefab, playerHitVfxSpawnPoint);

        if (enemyAttackFinishDelay > 0f)
        {
            yield return new WaitForSeconds(enemyAttackFinishDelay);
        }
    }

    public IEnumerator PlayEnemyDeathRoutine()
    {
        PlayAnimator(enemyAnimator, enemyDeathPlaybackMode, enemyDeathTriggerName, enemyDeathStateName);
        PlaySfx(enemyDeathSfx);
        SpawnVfx(enemyDeathVfxPrefab, enemyDeathVfxSpawnPoint);

        if (enemyDeathDuration > 0f)
        {
            yield return new WaitForSeconds(enemyDeathDuration);
        }
    }

    public void SetEnemyRuntimeBindings(
        Animator animator,
        Transform hitVfxPoint,
        string attackTriggerName,
        string hitTriggerName,
        string deathTriggerName)
    {
        CaptureEnemyInspectorBindings();

        var restoreInspectorBindings = animator == null
            && hitVfxPoint == null
            && attackTriggerName == null
            && hitTriggerName == null
            && deathTriggerName == null;

        enemyAnimator = animator != null ? animator : defaultEnemyAnimator;
        enemyHitVfxPoint = hitVfxPoint != null ? hitVfxPoint : defaultEnemyHitVfxPoint;

        if (restoreInspectorBindings)
        {
            enemyAttackTriggerName = defaultEnemyAttackTriggerName;
            enemyHitTriggerName = defaultEnemyHitTriggerName;
            enemyDeathTriggerName = defaultEnemyDeathTriggerName;
            useEnemyHitTrigger = defaultUseEnemyHitTrigger;
            enemyAttackPlaybackMode = defaultEnemyAttackPlaybackMode;
            enemyDeathPlaybackMode = defaultEnemyDeathPlaybackMode;
            return;
        }

        enemyAttackTriggerName = attackTriggerName ?? string.Empty;
        enemyHitTriggerName = hitTriggerName ?? string.Empty;
        enemyDeathTriggerName = deathTriggerName ?? string.Empty;
        useEnemyHitTrigger = true;
        enemyAttackPlaybackMode = AnimationPlaybackMode.Trigger;
        enemyDeathPlaybackMode = AnimationPlaybackMode.Trigger;
    }

    public void SetPlayerCombatMode(CombatMode mode)
    {
        currentCombatMode = mode;
        if (playerAnimator == null) return;
        if (!string.IsNullOrEmpty(playerDefenseModeBoolName))
        {
            playerAnimator.SetBool(playerDefenseModeBoolName, mode == CombatMode.Defense);
        }
        if (!string.IsNullOrEmpty(playerCombatModeIntName))
        {
            playerAnimator.SetInteger(playerCombatModeIntName, mode == CombatMode.Defense ? 1 : 0);
        }
    }

    private void PlayPlayerHit()
    {
        string triggerName = currentCombatMode == CombatMode.Defense ? playerDefenseModeHitTriggerName : playerAttackModeHitTriggerName;
        string stateName = currentCombatMode == CombatMode.Defense ? playerDefenseModeHitStateName : playerAttackModeHitStateName;
        PlayAnimator(playerAnimator, playerHitPlaybackMode, triggerName, stateName);
    }

    private static void PlayAnimator(Animator animator, AnimationPlaybackMode mode, string triggerName, string stateName)
    {
        if (animator == null) return;
        if (mode == AnimationPlaybackMode.Trigger)
        {
            if (string.IsNullOrEmpty(triggerName)) return;
            animator.ResetTrigger(triggerName);
            animator.SetTrigger(triggerName);
            return;
        }
        if (string.IsNullOrEmpty(stateName)) return;
        animator.Play(stateName, 0, 0f);
    }

    private void ResetPlayerAttackTriggers()
    {
        if (playerAnimator == null)
            return;

        ResetPlayerAttackTrigger(lightAttack);
        ResetPlayerAttackTrigger(mediumAttack);
        ResetPlayerAttackTrigger(heavyAttack);
    }

    private void ResetPlayerAttackTrigger(AttackAnimationProfile profile)
    {
        if (profile == null || string.IsNullOrEmpty(profile.playerTriggerName))
            return;

        playerAnimator.ResetTrigger(profile.playerTriggerName);
    }

    private void SpawnAttackVfx(AttackVfxEvent vfxEvent)
    {
        if (vfxEvent == null || vfxEvent.prefab == null)
            return;

        Transform spawnPoint = vfxEvent.spawnPointOverride != null
            ? vfxEvent.spawnPointOverride
            : playerVfxSpawnPoint;

        if (spawnPoint == null)
        {
            Debug.LogWarning($"Attack VFX spawn point is missing. VFX: {vfxEvent.prefab.name}");
            return;
        }

        Vector3 position = spawnPoint.position + spawnPoint.TransformDirection(vfxEvent.positionOffset);
        Quaternion rotation = spawnPoint.rotation * Quaternion.Euler(vfxEvent.rotationOffset);

        GameObject instance = Instantiate(vfxEvent.prefab, position, rotation);

        if (vfxEvent.parentToSpawnPoint)
        {
            instance.transform.SetParent(spawnPoint, true);
        }

        instance.transform.localScale = Vector3.Scale(instance.transform.localScale, vfxEvent.scaleMultiplier);
        DestroyVfxAfterLifeTime(instance, vfxEvent.lifeTime);
    }

    private void SpawnVfx(GameObject prefab, Transform spawnPoint, float lifeTime = 2f)
    {
        if (prefab == null || spawnPoint == null)
            return;

        GameObject instance = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        DestroyVfxAfterLifeTime(instance, lifeTime);
    }

    private void DestroyVfxAfterLifeTime(GameObject instance, float lifeTime)
    {
        if (instance == null || lifeTime <= 0f)
            return;

        Destroy(instance, lifeTime);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    [ContextMenu("Test/Play By Test Damage")]
    private void TestPlayByDamage()
    {
        PlayAttackByDamage(testDamage);
    }

    [ContextMenu("Test/Play Light Attack")]
    private void TestLightAttack()
    {
        PlayAttack(BattleAttackMotionType.Light);
    }

    [ContextMenu("Test/Play Medium Attack")]
    private void TestMediumAttack()
    {
        PlayAttack(BattleAttackMotionType.Medium);
    }

    [ContextMenu("Test/Play Heavy Attack")]
    private void TestHeavyAttack()
    {
        PlayAttack(BattleAttackMotionType.Heavy);
    }

    [ContextMenu("Test/Play Enemy Hit")]
    private void TestEnemyHit()
    {
        PlayEnemyHit();
    }
}

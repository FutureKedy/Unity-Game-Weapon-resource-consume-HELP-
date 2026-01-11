using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static GameEnums;

public class WeaponLogic : MonoBehaviour
{
    public enum AttackType { Swing, Thrust, HeavyThrust, ContinuousThrust, AuraStrike }


    [System.Serializable]
    public class AttackAnimationSettings
    {
        public GameObject animationPrefab;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
        public float duration = 0.5f;
    }

    [Header("Swordsmanship Style")]
    private SwordsmanshipStyle currentStyle;
    private CurrentStyle currentStyleScript;

    private int currentStrikeIndex = 0;

    [Header("Attack Mode (Manual Override)")]
    public AttackType clickAttackType = AttackType.Swing;   // used when activated normally
    public AttackType holdAttackType = AttackType.Thrust;   // used when activated with hold

    [Header("Pivot Settings")]
    public Transform weaponPivot;

    [Header("Offsets")]
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    [Header("Swing Settings (Defaults)")]
    public float swingStartOffset = 90f;
    public float swingDegrees = 180f;
    public float swingDuration = 0.3f;

    [Header("Thrust Settings")]
    public float thrustDistance = 1f;
    public float thrustDuration = 0.2f;

    [Header("Heavy Thrust Settings")]
    public float heavyThrustPullback = 2f;
    public float heavyThrustDistance = 15f;
    public float heavyThrustDuration = 0.6f;

    [Header("Heavy Thrust Particles")]
    [Range(0f, 1f)] public float particleDensity = 0.5f;
    public List<GameObject> particlePrefabs = new List<GameObject>();
    public float particleLifetime = 0.5f;
    public float particleSpawnRadius = 0.5f;     // random radius offset
    public float particleSpawnJitter = 0.3f;     // random timing offset

    [Header("Continuous Thrust Settings")]
public float continuousThrustRange = 13f;
public int continuousThrustLimit = 33;
public float continuousThrustSpeed = 25f; // units per second
public GameObject continuousThrustShortPrefab;
public GameObject continuousThrustMiddlePrefab;
public GameObject continuousThrustLongPrefab;

    [Header("Damage Settings")]
    public float baseDamage = 10f;
    public DamageType damageType = DamageType.None;
    public ElementType elementType = ElementType.None;
    public PolygonCollider2D swingCollider;

    [Header("Attack Animations")]
    public AttackAnimationSettings swingAnimation;
    public AttackAnimationSettings thrustAnimation;
    public AttackAnimationSettings heavyThrustAnimation;

    private SpriteRenderer spriteRenderer;
    private bool isSwinging = false;
    private bool isThrusting = false;
    private bool isHeavyThrusting = false;

    private bool isContinuousThrusting = false;

private float continuousThrustTimer = 0f;
private Transform continuousThrustTarget;
private Vector3 continuousThrustStartPos;
private Vector3 continuousThrustEndPos;
    private Quaternion continuousThrustRotation;

private Queue<Transform> continuousThrustQueue = new Queue<Transform>();
private int continuousThrustCount = 0;



    private float swingTimer = 0f;
    private float swingStartAngle;
    private float currentSwingDegrees;
    private float currentSwingDuration;
    private bool swingFacingLeft = false;

    private float thrustTimer = 0f;
    private Vector3 thrustLocalStart;
    private Vector3 thrustLocalTarget;
    private Quaternion thrustRotation;

    private float heavyThrustTimer = 0f;
    private Vector3 heavyThrustDirection;
    private Vector3 heavyThrustStartPos;
    private Vector3 heavyThrustEndPos;
    private Quaternion heavyThrustRotation;
    private Transform playerTransform;
    private float particleSpawnAccumulator = 0f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (swingCollider != null)
            swingCollider.enabled = false;

        playerTransform = transform.root;
    }

    void LateUpdate()
    {
        if (weaponPivot == null) return;
        
if (isSwinging)
            UpdateSwing();
        else if (isThrusting)
            UpdateThrust();
        else if (isHeavyThrusting)
            UpdateHeavyThrust();
        else if (isContinuousThrusting)
            UpdateContinuousThrust();
        else
            IdleFollowMouse();

       
    }

    private void IdleFollowMouse()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        mousePos.z = 0f;

        Vector3 direction = mousePos - weaponPivot.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle) * Quaternion.Euler(rotationOffset);
        transform.position = weaponPivot.position + (Quaternion.Euler(0, 0, angle) * positionOffset);

        if (spriteRenderer != null)
            spriteRenderer.flipY = mousePos.x < weaponPivot.position.x;
    }

    private void OnEnable()
{
    currentStyleScript = FindObjectOfType<CurrentStyle>();
    if (currentStyleScript != null)
    {
        SetCurrentStyle(currentStyleScript.GetCurrentStyle());
        currentStyleScript.OnStyleChanged += HandleCurrentStyleChanged;
    }

    // 🔹 Register as Hold Weapon 1
    var useItem = FindObjectOfType<UseItem>();
    if (useItem != null)
    {
        useItem.RegisterSpawnedWeapon(gameObject, true, true); 
        Debug.Log("[WeaponLogic] Registered as Hold Weapon 1.");
    }
}

private void OnDisable()
{
    if (currentStyleScript != null)
        currentStyleScript.OnStyleChanged -= HandleCurrentStyleChanged;

    // 🔹 Unregister on disable
    var useItem = FindObjectOfType<UseItem>();
    if (useItem != null)
    {
        useItem.UnregisterSpawnedWeapon(gameObject, true, true);
        Debug.Log("[WeaponLogic] Unregistered as Hold Weapon 1.");
    }
}


    private void HandleCurrentStyleChanged(SwordsmanshipStyle newStyle)
    {
        SetCurrentStyle(newStyle);
    }

    public void SetCurrentStyle(SwordsmanshipStyle newStyle)
    {
        currentStyle = newStyle;
        currentStrikeIndex = 0;
    }

    public void Activate(bool isPrimary, bool isHold = false)
    {
        AttackType chosenType = isHold ? holdAttackType : clickAttackType;
        SwordsmanshipStyle.Strike strikeToUse = null;

        if (currentStyle != null && currentStyle.strikes.Count > 0)
        {
            strikeToUse = currentStyle.strikes[currentStrikeIndex];

            // If the style defines strikes, it overrides inspector setting
            switch (strikeToUse.strikeType)
            {
                case SwordsmanshipStyle.StrikeType.Swing:
                    chosenType = AttackType.Swing;
                    break;
                case SwordsmanshipStyle.StrikeType.Thrust:
                    chosenType = isHold ? holdAttackType : clickAttackType;
                    break;
                case SwordsmanshipStyle.StrikeType.Special:
                    Debug.Log("Special strike triggered (not implemented).");
                    break;
            }
        }

        if (chosenType == AttackType.Swing)
    StartSwing(strikeToUse);
else if (chosenType == AttackType.Thrust)
    StartThrust();
else if (chosenType == AttackType.HeavyThrust)
    StartHeavyThrust();
else if (chosenType == AttackType.ContinuousThrust)
    StartContinuousThrust();


        if (currentStyle != null && currentStyle.strikes.Count > 0)
            currentStrikeIndex = (currentStrikeIndex + 1) % currentStyle.strikes.Count;
    }

    private void StartSwing(SwordsmanshipStyle.Strike strike)
    {
        if (isSwinging || weaponPivot == null) return;

        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        mousePos.z = 0f;

        Vector3 direction = mousePos - weaponPivot.position;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        swingFacingLeft = mousePos.x < weaponPivot.position.x;
        if (spriteRenderer != null)
            spriteRenderer.flipY = swingFacingLeft;

        if (strike != null && strike.overrideSwingSettings)
        {
            currentSwingDegrees = strike.swingDegrees;
            currentSwingDuration = strike.swingDuration;
            swingStartOffset = strike.swingStartOffset;
        }
        else
        {
            currentSwingDegrees = swingDegrees;
            currentSwingDuration = swingDuration;
        }

        swingStartAngle = baseAngle + (swingFacingLeft ? -swingStartOffset : swingStartOffset);
        swingTimer = 0f;
        isSwinging = true;

        if (swingCollider != null)
            swingCollider.enabled = true;

        transform.rotation = Quaternion.Euler(0, 0, swingStartAngle) * Quaternion.Euler(rotationOffset);
        transform.position = weaponPivot.position + (Quaternion.Euler(0, 0, swingStartAngle) * positionOffset);

        PlayAttackAnimation(swingAnimation, baseAngle, swingFacingLeft);
    }

    private void UpdateSwing()
    {
        swingTimer += Time.deltaTime;
        float t = Mathf.Clamp01(swingTimer / currentSwingDuration);

        float directionMultiplier = swingFacingLeft ? 1f : -1f;
        float currentAngle = swingStartAngle + (currentSwingDegrees * t * directionMultiplier);

        transform.rotation = Quaternion.Euler(0, 0, currentAngle) * Quaternion.Euler(rotationOffset);
        transform.position = weaponPivot.position + (Quaternion.Euler(0, 0, currentAngle) * positionOffset);

        if (swingTimer >= currentSwingDuration)
        {
            isSwinging = false;
            if (swingCollider != null)
                swingCollider.enabled = false;
        }
    }

    private void StartThrust()
    {
        if (isThrusting || weaponPivot == null) return;

        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        mousePos.z = 0f;

        Vector3 direction = (mousePos - weaponPivot.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        thrustRotation = Quaternion.Euler(0, 0, angle);
        thrustLocalStart = thrustRotation * positionOffset;
        thrustLocalTarget = thrustLocalStart + direction * thrustDistance;

        thrustTimer = 0f;
        isThrusting = true;

        if (swingCollider != null)
            swingCollider.enabled = true;

        PlayAttackAnimation(thrustAnimation, angle, mousePos.x < weaponPivot.position.x);
    }

    private void UpdateThrust()
    {
        thrustTimer += Time.deltaTime;
        float halfDuration = thrustDuration / 2f;
        Vector3 localPos;

        if (thrustTimer <= halfDuration)
        {
            float t = thrustTimer / halfDuration;
            localPos = Vector3.Lerp(thrustLocalStart, thrustLocalTarget, t);
        }
        else
        {
            float t = (thrustTimer - halfDuration) / halfDuration;
            localPos = Vector3.Lerp(thrustLocalTarget, thrustLocalStart, t);
        }

        transform.position = weaponPivot.position + localPos;
        transform.rotation = thrustRotation * Quaternion.Euler(rotationOffset);

        if (thrustTimer >= thrustDuration)
        {
            isThrusting = false;
            if (swingCollider != null)
                swingCollider.enabled = false;
        }
    }

    private void StartHeavyThrust()
    {
        if (isHeavyThrusting || weaponPivot == null) return;

        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        mousePos.z = 0f;

        heavyThrustDirection = (mousePos - weaponPivot.position).normalized;
        heavyThrustRotation = Quaternion.Euler(0, 0, Mathf.Atan2(heavyThrustDirection.y, heavyThrustDirection.x) * Mathf.Rad2Deg);

        heavyThrustStartPos = playerTransform.position;
        heavyThrustEndPos = playerTransform.position + (heavyThrustDirection * heavyThrustDistance);

        heavyThrustTimer = 0f;
        isHeavyThrusting = true;
        particleSpawnAccumulator = 0f;

        if (swingCollider != null)
            swingCollider.enabled = true;
    }

    private void UpdateHeavyThrust()
    {
        heavyThrustTimer += Time.deltaTime;
        float halfDuration = heavyThrustDuration / 2f;

        if (heavyThrustTimer <= halfDuration)
        {
            float t = heavyThrustTimer / halfDuration;
            Vector3 localPullback = positionOffset + new Vector3(-heavyThrustPullback * t, 0f, 0f);

            transform.rotation = heavyThrustRotation * Quaternion.Euler(rotationOffset);
            transform.position = weaponPivot.position + (heavyThrustRotation * localPullback);
        }
        else
        {
            float t = (heavyThrustTimer - halfDuration) / halfDuration;
            Vector3 newPlayerPos = Vector3.Lerp(heavyThrustStartPos, heavyThrustEndPos, t);

            // Spawn particles
            if (particleDensity > 0f && particlePrefabs.Count > 0)
            {
                float distanceMoved = Vector3.Distance(playerTransform.position, newPlayerPos);
                float spacing = Mathf.Lerp(3f, 0.2f, particleDensity);
                particleSpawnAccumulator += distanceMoved;

                while (particleSpawnAccumulator >= spacing)
                {
                    particleSpawnAccumulator -= spacing;

                    int index = Random.Range(0, particlePrefabs.Count);
                    GameObject chosenPrefab = particlePrefabs[index];

                    // Random offset within circle radius
                    Vector2 offset = Random.insideUnitCircle * particleSpawnRadius;

                    // Random jitter forward/backward along path
                    Vector3 jitteredPos = Vector3.Lerp(playerTransform.position, newPlayerPos, Random.Range(0f, 1f));
                    Vector3 spawnPos = jitteredPos + (Vector3)offset;

                    GameObject particle = Instantiate(chosenPrefab, spawnPos, Quaternion.identity);
                    Destroy(particle, particleLifetime);
                }
            }

            playerTransform.position = newPlayerPos;

            transform.rotation = heavyThrustRotation * Quaternion.Euler(rotationOffset);
            transform.position = weaponPivot.position + (heavyThrustRotation * positionOffset);
        }

        if (heavyThrustTimer >= heavyThrustDuration)
        {
            isHeavyThrusting = false;
            if (swingCollider != null)
                swingCollider.enabled = false;
        }
    }

    private void StartContinuousThrust()
{
    if (isContinuousThrusting || weaponPivot == null) return;

    // find enemies in range
    GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
    List<Transform> sortedEnemies = new List<Transform>();

    foreach (var enemy in enemies)
    {
        float dist = Vector3.Distance(playerTransform.position, enemy.transform.position);
        if (dist <= continuousThrustRange)
            sortedEnemies.Add(enemy.transform);
    }

    // sort by distance (closest first)
    sortedEnemies.Sort((a, b) =>
        Vector3.Distance(playerTransform.position, a.position)
        .CompareTo(Vector3.Distance(playerTransform.position, b.position)));

    // fill queue up to limit
    continuousThrustQueue.Clear();
    for (int i = 0; i < Mathf.Min(continuousThrustLimit, sortedEnemies.Count); i++)
        continuousThrustQueue.Enqueue(sortedEnemies[i]);

    if (continuousThrustQueue.Count == 0) return; // no valid target

    // set first target
    continuousThrustTarget = continuousThrustQueue.Dequeue();
    continuousThrustStartPos = playerTransform.position; // ✅ FIX: initialize start pos
    continuousThrustEndPos = continuousThrustTarget.position;
    continuousThrustCount = 1;
    isContinuousThrusting = true;

    // calculate rotation
    continuousThrustRotation = Quaternion.Euler(0, 0,
        Mathf.Atan2((continuousThrustEndPos - continuousThrustStartPos).y,
                    (continuousThrustEndPos - continuousThrustStartPos).x) * Mathf.Rad2Deg);

    continuousThrustTimer = 0f;

    // pick prefab based on distance
    float closestDist = Vector3.Distance(continuousThrustStartPos, continuousThrustEndPos); // ✅ FIX
    GameObject chosenPrefab = null;
    if (closestDist <= 4f && continuousThrustShortPrefab != null)
        chosenPrefab = continuousThrustShortPrefab;
    else if (closestDist <= 9f && continuousThrustMiddlePrefab != null)
        chosenPrefab = continuousThrustMiddlePrefab;
    else if (continuousThrustLongPrefab != null)
        chosenPrefab = continuousThrustLongPrefab;

    if (chosenPrefab != null)
    {
        GameObject fx = Instantiate(chosenPrefab, playerTransform.position, continuousThrustRotation);
        Destroy(fx, 0.5f); // ✅ FIX: was using continuousThrustDuration (deleted var)
    }

    if (swingCollider != null)
        swingCollider.enabled = true;
}


private void UpdateContinuousThrust()
{
    continuousThrustTimer += Time.deltaTime;

    // move player toward current target
    Vector3 direction = (continuousThrustEndPos - playerTransform.position).normalized;
    float step = continuousThrustSpeed * Time.deltaTime;
    playerTransform.position = Vector3.MoveTowards(playerTransform.position, continuousThrustEndPos, step);

    transform.rotation = continuousThrustRotation * Quaternion.Euler(rotationOffset);
    transform.position = weaponPivot.position + (continuousThrustRotation * positionOffset);

    // reached target?
    if (Vector3.Distance(playerTransform.position, continuousThrustEndPos) <= 0.05f)
    {
        // damage
        if (continuousThrustTarget != null)
        {
            Health targetHealth = continuousThrustTarget.GetComponent<Health>();
            if (targetHealth != null)
                DealDamage(targetHealth);
        }

        // next enemy in chain
        if (continuousThrustQueue.Count > 0 && continuousThrustCount < continuousThrustLimit)
        {
            continuousThrustTarget = continuousThrustQueue.Dequeue();

            // ✅ Reset start/end positions for the new jump
            continuousThrustStartPos = playerTransform.position;
            continuousThrustEndPos = continuousThrustTarget.position;

            // ✅ Recalculate rotation
            continuousThrustRotation = Quaternion.Euler(0, 0,
                Mathf.Atan2((continuousThrustEndPos - continuousThrustStartPos).y,
                            (continuousThrustEndPos - continuousThrustStartPos).x) * Mathf.Rad2Deg);

            // ✅ Spawn prefab based on new distance
            float dist = Vector3.Distance(continuousThrustStartPos, continuousThrustEndPos);
            GameObject chosenPrefab = null;
            if (dist <= 4f && continuousThrustShortPrefab != null)
                chosenPrefab = continuousThrustShortPrefab;
            else if (dist <= 9f && continuousThrustMiddlePrefab != null)
                chosenPrefab = continuousThrustMiddlePrefab;
            else if (continuousThrustLongPrefab != null)
                chosenPrefab = continuousThrustLongPrefab;

            if (chosenPrefab != null)
            {
                GameObject fx = Instantiate(chosenPrefab, playerTransform.position, continuousThrustRotation);
                Destroy(fx, 0.5f);
            }

            continuousThrustCount++;
        }
        else
        {
            // stop chaining
            isContinuousThrusting = false;
            if (swingCollider != null)
                swingCollider.enabled = false;
        }
    }
}



    private void PlayAttackAnimation(AttackAnimationSettings settings, float baseAngle, bool facingLeft)
    {
        if (settings.animationPrefab == null) return;

        Quaternion rotation = Quaternion.Euler(0, 0, baseAngle) * Quaternion.Euler(settings.rotationOffset);

        GameObject anim = Instantiate(
            settings.animationPrefab,
            weaponPivot.position + (rotation * settings.positionOffset),
            rotation
        );

        if (facingLeft)
        {
            Vector3 scale = anim.transform.localScale;
            scale.y *= -1;
            anim.transform.localScale = scale;
        }

        Destroy(anim, settings.duration);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isSwinging && !isThrusting && !isHeavyThrusting) return;

        Health target = other.GetComponent<Health>();
        if (target != null)
            DealDamage(target);
    }

    private void DealDamage(Health target)
    {
        if (target == null) return;

        target.TakeDamage(
            Mathf.RoundToInt(baseDamage),
            Vector2.zero,
            transform,
            damageType,
            false,
            elementType
        );
    }
}

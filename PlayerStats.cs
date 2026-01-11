using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;
using static GameEnums;

[System.Serializable]
public class Stat
{
    public float baseValue;
    public float current;
    public float max;
    private List<float> modifiers = new List<float>();

    public Stat(float baseValue)
    {
        this.baseValue = baseValue;
        this.max = baseValue;
        this.current = baseValue;
    }

    public float GetValue()
    {
        float finalValue = baseValue;
        modifiers.ForEach(x => finalValue += x);
        return finalValue;
    }

    public void AddModifier(float modifier)
    {
        if (modifier != 0)
            modifiers.Add(modifier);
    }

    public void RemoveModifier(float modifier)
    {
        if (modifier != 0)
            modifiers.Remove(modifier);
    }
}

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    public int baseHealth = 10;
    public int baseMana = 10;
    public int baseStamina = 10;

    [Header("Damage Resistances")]
    [Range(0, 100)] public int physicalResistance = 0;
    [Range(0, 100)] public int magicalResistance = 0;
    [Range(0, 100)] public int holyResistance = 0;
    [Range(0, 100)] public int demonicResistance = 0;

    [Header("Elemental Resistances")]
    [Range(0, 100)] public int fireResistance = 0;
    [Range(0, 100)] public int waterResistance = 0;
    [Range(0, 100)] public int earthResistance = 0;
    [Range(0, 100)] public int airResistance = 0;
    [Range(0, 100)] public int iceResistance = 0;
    [Range(0, 100)] public int lightningResistance = 0;
    [Range(0, 100)] public int spaceResistance = 0;
    [Range(0, 100)] public int timeResistance = 0;

    [Header("Damage Weaknesses")]
    [Range(0, 100)] public int physicalWeakness = 0;
    [Range(0, 100)] public int magicalWeakness = 0;
    [Range(0, 100)] public int holyWeakness = 0;
    [Range(0, 100)] public int demonicWeakness = 0;
    [Header("Elemental Weaknesses")]
    [Range(0, 100)] public int fireWeakness = 0;
    [Range(0, 100)] public int waterWeakness = 0;
    [Range(0, 100)] public int earthWeakness = 0;
    [Range(0, 100)] public int airWeakness = 0;
    [Range(0, 100)] public int iceWeakness = 0;
    [Range(0, 100)] public int lightningWeakness = 0;
    [Range(0, 100)] public int spaceWeakness = 0;
    [Range(0, 100)] public int timeWeakness = 0;


    [Header("Purity/Corruption")]
    [Range(0f, 100f)] public float purity = 0f;
    [Range(0f, 100f)] public float corruption = 0f;
    public float GetDamageMultiplier()
    {
        return 1f - (purity / 100f); // 100% purity = 0% damage (adjust formula if needed)
    }

    [Header("Core Stats")]
    public Stat health;
    public Stat mana;
    public Stat stamina;
    public int strength = 0;
    public int agility = 0;
    public int dexterity = 0;
    public int thaumir = 0;
    public int charisma = 0;
    public int statPoints = 15;

    [Header("Movement")]
    public float baseSpeed = 3f;
    public float currentSpeed;
    public bool isRunning;

    [Header("External Movement Force (Train, Wind, etc.)")]
    public Vector2 externalForce = Vector2.zero;
    public bool hasExternalForce => externalForce.sqrMagnitude > 0.01f;


    [Header("Combat")]
    public float physicalDamageMultiplier = 1f;
    public float magicalDamageMultiplier = 1f;
    public float healthRegen = 0f;
    public float staminaRegen = 0f;
    public float artifactCritDamageBonus = 0f;
    private float currentCritChance;

    [Header("Leveling")]
    public int currentExp = 0;
    public int expToNextLevel = 100;
    public int playerLevel = 1;
    private int baseExp = 100;

    [Header("Regeneration Rates")]
    public float manaRegenRate = 2f;
    public float richManaRegenRate = 7f;
    public float staminaRegenRate = 2f;
    public float staminaDrainRate = 5f;

    [Header("UI References")]
    public Image healthBarFill;
    public Image manaBarFill;
    public Image staminaBarFill;
    public Image expBarFill;
    public TMP_Text healthText;
    public TMP_Text manaText;
    public TMP_Text staminaText;
    public TMP_Text thaumirText;
    public TMP_Text strengthText;
    public TMP_Text agilityText;
    public TMP_Text dexterityText;
    public TMP_Text charismaText;
    public TMP_Text statPointsText;
    public TMP_Text levelText;

    public event System.Action<float> OnHealthChanged;

    [Header("Effects")]
    public GameObject damageParticleEffect;
    public GameObject levelUpEffect;

    [Header("UI Buttons")]
    public Button increaseHealthButton;
    public Button increaseManaButton;
    public Button increaseStaminaButton;
    public Button increaseStrengthButton;
    public Button increaseAgilityButton;
    public Button increaseDexterityButton;
    public Button increaseThaumirButton;
    public Button increaseCharismaButton;

    private Rigidbody2D rb;
    private Vector2 movement;
    private bool isStaminaExhausted = false;
    private bool isManaExhausted = false;
    private float staminaExhaustionTimer = 0f;
    private float manaExhaustionTimer = 0f;
    private const float exhaustionDuration = 7f;
    private bool isInRichManaArea = false;
    public bool isDead = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        health = new Stat(baseHealth);
        mana = new Stat(baseMana);
        stamina = new Stat(baseStamina);

        if (increaseHealthButton != null) increaseHealthButton.onClick.AddListener(() => AddStatPoint("Health"));
        if (increaseManaButton != null) increaseManaButton.onClick.AddListener(() => AddStatPoint("Mana"));
        if (increaseStaminaButton != null) increaseStaminaButton.onClick.AddListener(() => AddStatPoint("Stamina"));
        if (increaseStrengthButton != null) increaseStrengthButton.onClick.AddListener(() => AddStatPoint("Strength"));
        if (increaseAgilityButton != null) increaseAgilityButton.onClick.AddListener(() => AddStatPoint("Agility"));
        if (increaseDexterityButton != null) increaseDexterityButton.onClick.AddListener(() => AddStatPoint("Dexterity"));
        if (increaseThaumirButton != null) increaseThaumirButton.onClick.AddListener(() => AddStatPoint("Thaumir"));
        if (increaseCharismaButton != null) increaseCharismaButton.onClick.AddListener(() => AddStatPoint("Charisma"));

        UpdateStats();
        UpdateStatDisplays();
    }

    public void InterruptSkills()
    {
        UsingSkill[] skills = GetComponents<UsingSkill>();
        foreach (var skill in skills)
        {
            skill.Interrupt();
        }
    }

    public void AddPurity(float amount)
    {
        if (corruption > 0)
        {
            float reduction = Mathf.Min(corruption, amount);
            corruption -= reduction;
            amount -= reduction;
            UpdateMaxHealth();
            if (corruption > 0) return;
        }

        if (amount > 0)
        {
            purity = Mathf.Clamp(purity + amount, 0, 100f);
        }

        UpdateStats();
        UpdateStatDisplays();
    }

    public void AddCorruption(float amount)
    {
        if (purity > 0)
        {
            float reduction = Mathf.Min(purity, amount);
            purity -= reduction;
            amount -= reduction;
            if (purity > 0) return;
        }

        if (amount > 0)
        {
            corruption = Mathf.Clamp(corruption + amount, 0, 100f);
            UpdateMaxHealth();
        }

        UpdateStats();
        UpdateStatDisplays();
    }

    private void UpdateMaxHealth()
    {
        float corruptionReduction = corruption / 100f;
        health.max = Mathf.RoundToInt(baseHealth * (1f - corruptionReduction));
        health.current = Mathf.Clamp(health.current, 0, health.max);
    }

    public void TakeDamage(
        int damage,
        Vector2 knockbackDirection,
        Transform attacker,
        GameEnums.DamageType damageType = GameEnums.DamageType.None,
        bool isCritical = false,
        GameEnums.ElementType elementType = GameEnums.ElementType.None)
    {
        InterruptSkills();
        if (isDead) return;

        int finalDamage = CalculateFinalDamage(damage, damageType, elementType);
        ApplyDamage(finalDamage, knockbackDirection);
    }

    private int CalculateFinalDamage(int damage, GameEnums.DamageType damageType, GameEnums.ElementType elementType)
{
    float damageModifier = 1f;

    // Damage Type Modifiers (Resistance - Weakness)
    switch (damageType)
    {
        case GameEnums.DamageType.Physical:
            damageModifier *= (1f - (physicalResistance / 100f)) * (1f + (physicalWeakness / 100f));
            break;
        case GameEnums.DamageType.Magical:
            damageModifier *= (1f - (magicalResistance / 100f)) * (1f + (magicalWeakness / 100f));
            break;
        case GameEnums.DamageType.Holy:
            damageModifier *= (1f - (holyResistance / 100f)) * (1f + (holyWeakness / 100f));
            if (purity > 0) damageModifier *= (1f - (purity / 100f)); // Purity reduces holy damage
            break;
        case GameEnums.DamageType.Demonic:
            damageModifier *= (1f - (demonicResistance / 100f)) * (1f + (demonicWeakness / 100f));
            break;
    }

    // Elemental Modifiers (Resistance - Weakness)
    switch (elementType)
    {
        case GameEnums.ElementType.Fire:
            damageModifier *= (1f - (fireResistance / 100f)) * (1f + (fireWeakness / 100f));
            break;
        case GameEnums.ElementType.Water:
            damageModifier *= (1f - (waterResistance / 100f)) * (1f + (waterWeakness / 100f));
            break;
        case GameEnums.ElementType.Earth:
            damageModifier *= (1f - (earthResistance / 100f)) * (1f + (earthWeakness / 100f));
            break;
        case GameEnums.ElementType.Air:
            damageModifier *= (1f - (airResistance / 100f)) * (1f + (airWeakness / 100f));
            break;
        case GameEnums.ElementType.Ice:
            damageModifier *= (1f - (iceResistance / 100f)) * (1f + (iceWeakness / 100f));
            break;
        case GameEnums.ElementType.Lightning:
            damageModifier *= (1f - (lightningResistance / 100f)) * (1f + (lightningWeakness / 100f));
            break;
        case GameEnums.ElementType.Space:
            damageModifier *= (1f - (spaceResistance / 100f)) * (1f + (spaceWeakness / 100f));
            break;
        case GameEnums.ElementType.Time:
            damageModifier *= (1f - (timeResistance / 100f)) * (1f + (timeWeakness / 100f));
            break;
    }

    return Mathf.RoundToInt(damage * damageModifier);
}

    private void ApplyDamage(int finalDamage, Vector2 knockbackDirection)
{
    float previousHealth = health.current; // Store before changing
    health.current -= finalDamage;
    health.current = Mathf.Clamp(health.current, 0, health.max);
    
    // Add this line to notify listeners:
    if (OnHealthChanged != null && health.current < previousHealth) 
        OnHealthChanged(health.current);

        if (damageParticleEffect != null)
        {
            Instantiate(damageParticleEffect, transform.position, Quaternion.identity);
        }

        if (rb != null && knockbackDirection != Vector2.zero)
        {
            rb.AddForce(knockbackDirection * 10f, ForceMode2D.Impulse);
        }

        UpdateHealthBar();

        if (health.current <= 0)
        {
            Die();
        }
    }

    private void Update()
{
    if (isDead) return;

    Vector2 moveInput = Vector2.zero;
    if (Keyboard.current != null)
    {
        // Horizontal input
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            moveInput.x = -1f;
        else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            moveInput.x = 1f;
        else
            moveInput.x = 0f;

        // Vertical input
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            moveInput.y = 1f;
        else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            moveInput.y = -1f;
        else
            moveInput.y = 0f;
    }

    movement = moveInput.normalized;

    // Only treat player input as movement for running purposes
bool hasInput = movement.sqrMagnitude > 0.01f;

// Running check (LeftShift)
isRunning = hasInput && Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;


        if (isRunning && movement.magnitude > 0 && !isStaminaExhausted && stamina.current > 0)
        {
            stamina.current -= staminaDrainRate * Time.deltaTime;
            stamina.current = Mathf.Clamp(stamina.current, 0, stamina.max);

            if (stamina.current <= 0)
            {
                stamina.current = 0;
                isRunning = false;
                isStaminaExhausted = true;
                staminaExhaustionTimer = exhaustionDuration;
            }
        }
        else
        {
            isRunning = false;
        }

        if (!isRunning)
        {
            RegenerateStats();
        }

        UpdateHealthBar();
        UpdateManaBar();
        UpdateStaminaBar();
        UpdateExpBar();

        if (currentExp >= expToNextLevel)
        {
            LevelUp();
        }

        UpdateExhaustionTimers();
    }

   





    public void UpdateStats()
    {
        if (agility <= 10)
        {
            currentSpeed = baseSpeed + agility * 1f;
        }
        else
        {
            currentSpeed = baseSpeed + (5 * 1f) + (agility - 5) * 0.5f;
        }

        health.current = Mathf.Clamp(health.current, 0, health.max);
        mana.current = Mathf.Clamp(mana.current, 0, mana.max);
        stamina.current = Mathf.Clamp(stamina.current, 0, stamina.max);
        currentCritChance = dexterity * 0.5f;
    }

   private void RegenerateStats()
{
    if (!isDead && health.current < health.max && healthRegen > 0)
    {
        float previousHealth = health.current;
        float healthToAdd = healthRegen * Time.deltaTime;
        health.current = Mathf.Min(health.current + healthToAdd, health.max);
        
        // Add this line:
        if (OnHealthChanged != null && health.current > previousHealth) 
            OnHealthChanged(health.current);
            
        UpdateHealthBar();
    }

    // Existing mana regeneration
    if (!isManaExhausted && mana.current < mana.max)
    {
        float regenRate = isInRichManaArea ? richManaRegenRate : manaRegenRate;
        float manaToAdd = regenRate * Time.deltaTime;
        mana.current = Mathf.Min(mana.current + manaToAdd, mana.max);
    }

    // Existing stamina regeneration
    if (!isRunning && !isStaminaExhausted && stamina.current < stamina.max)
    {
        float staminaToAdd = staminaRegenRate * Time.deltaTime;
        stamina.current = Mathf.Min(stamina.current + staminaToAdd, stamina.max);
    }
}

    private void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = (float)health.current / (float)health.max;
        }
    }

    private void UpdateManaBar()
    {
        if (manaBarFill != null)
        {
            manaBarFill.fillAmount = (float)mana.current / (float)mana.max;
        }
    }

    private void UpdateStaminaBar()
    {
        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount = (float)stamina.current / (float)stamina.max;
        }
    }

    private void UpdateExpBar()
    {
        if (expBarFill != null)
        {
            expBarFill.fillAmount = (float)currentExp / (float)expToNextLevel;
        }
    }

    private void Die()
    {
        isDead = true;
        gameObject.SetActive(false);
    }

    public void GainExp(int exp)
    {
        currentExp += exp;
        if (currentExp >= expToNextLevel)
        {
            LevelUp();
        }
        UpdateExpBar();
    }

    private void LevelUp()
    {
        playerLevel++;
        currentExp -= expToNextLevel;
        expToNextLevel = (int)(baseExp * Mathf.Pow(1.1f, playerLevel - 1));
        statPoints += 3;

        if (levelUpEffect != null)
        {
            Instantiate(levelUpEffect, transform.position, Quaternion.identity);
        }

        UpdateStatDisplays();
    }

    public void AddStatPoint(string stat)
    {
        if (statPoints <= 0) return;

        switch (stat)
        {
            case "Health":
                health.max += 10;
                break;
            case "Mana":
                mana.max += 5;
                break;
            case "Stamina":
                stamina.max += 5;
                break;
            case "Strength":
                strength++;
                break;
            case "Agility":
                agility++;
                break;
            case "Dexterity":
                dexterity++;
                break;
            case "Thaumir":
                thaumir++;
                break;
            case "Charisma":
                charisma++;
                break;
            default:
                Debug.LogWarning("Invalid stat name.");
                return;
        }

        statPoints--;
        UpdateStats();
        UpdateStatDisplays();
    }

    private void UpdateStatDisplays()
    {
        if (healthText != null) healthText.text = ((int)health.max).ToString();
        if (manaText != null) manaText.text = ((int)mana.max).ToString();
        if (staminaText != null) staminaText.text = ((int)stamina.max).ToString();
        if (thaumirText != null) thaumirText.text = thaumir.ToString();
        if (strengthText != null) strengthText.text = strength.ToString();
        if (agilityText != null) agilityText.text = agility.ToString();
        if (dexterityText != null) dexterityText.text = dexterity.ToString();
        if (charismaText != null) charismaText.text = charisma.ToString();
        if (statPointsText != null) statPointsText.text = statPoints.ToString();
        if (levelText != null) levelText.text = playerLevel.ToString();
    }

    public void ForceUpdateUI()
{
    UpdateStats();
    UpdateStatDisplays();
}

    private void UpdateExhaustionTimers()
    {
        if (isStaminaExhausted)
        {
            staminaExhaustionTimer -= Time.deltaTime;
            if (staminaExhaustionTimer <= 0)
            {
                isStaminaExhausted = false;
            }
        }

        if (isManaExhausted)
        {
            manaExhaustionTimer -= Time.deltaTime;
            if (manaExhaustionTimer <= 0)
            {
                isManaExhausted = false;
            }
        }
    }

    public void UseMana(int amount)
    {
        if (mana.current >= amount && !isManaExhausted)
        {
            mana.current -= amount;
            UpdateManaBar();

            if (mana.current <= 0)
            {
                mana.current = 0;
                isManaExhausted = true;
                manaExhaustionTimer = exhaustionDuration;
            }
        }
    }

    public int CalculateMagicalDamage(int baseDamage)
    {
        return baseDamage + thaumir;
    }

    public int CalculateDamage()
    {
        int baseDamage = 10;
        float purityReduction = purity / 100f;
        float damageMultiplier = 1f - purityReduction;
        baseDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);

        bool isCritical = Random.value <= currentCritChance;

        if (isCritical)
        {
            float totalCritDamage = 2f + artifactCritDamageBonus;
            return Mathf.RoundToInt(baseDamage * totalCritDamage);
        }
        return baseDamage;
    }

    public void SetInRichManaArea(bool value)
    {
        isInRichManaArea = value;
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static GameEnums;

public class UseItem : MonoBehaviour
{
    [Header("Item Type")]
    public ItemType itemType;

    public enum UseBehavior { SpawnPrefab, CallPrefab }

    [Header("Item 1 Settings")]
    public GameObject useItem1Prefab;
    public UseBehavior useItem1Behavior = UseBehavior.CallPrefab;
    public float item1CooldownDuration = 0.3f;

    [Header("Hold Item 1 Settings")]
    public GameObject holdItem1Prefab;
    public UseBehavior holdItem1Behavior = UseBehavior.CallPrefab;

    [Header("Item 2 Settings")]
    public GameObject useItem2Prefab;
    public UseBehavior useItem2Behavior = UseBehavior.CallPrefab;
    public float item2CooldownDuration = 0.3f;

    [Header("Hold Item 2 Settings")]
    public GameObject holdItem2Prefab;
    public UseBehavior holdItem2Behavior = UseBehavior.CallPrefab;

    [Header("Timing Settings")]
    public float holdThreshold = 0.3f;

    [Header("Item 1 Resource Settings")]
    public ResourceType item1ResourceType = ResourceType.Mana;
    public float item1Cost = 10f;
    public bool item1UsePercentage = false;

    [Header("Hold Item 1 Resource Settings")]
    public ResourceType holdItem1ResourceType = ResourceType.Mana;
    public float holdItem1Cost = 5f;
    public bool holdItem1UsePercentage = false;

    [Header("Item 2 Resource Settings")]
    public ResourceType item2ResourceType = ResourceType.Mana;
    public float item2Cost = 10f;
    public bool item2UsePercentage = false;

    [Header("Hold Item 2 Resource Settings")]
    public ResourceType holdItem2ResourceType = ResourceType.Mana;
    public float holdItem2Cost = 5f;
    public bool holdItem2UsePercentage = false;

    private float item1CooldownTimer = 0f;
    private float item2CooldownTimer = 0f;

    private WeaponLogic spawnedWeapon1;
    private WeaponLogic spawnedWeapon2;
    private WeaponLogic spawnedHoldWeapon1;
    private WeaponLogic spawnedHoldWeapon2;

    private bool isHoldingItem1 = false;
    private float holdTimerItem1 = 0f;
    private bool holdActivatedItem1 = false;

    private bool isHoldingItem2 = false;
    private float holdTimerItem2 = 0f;
    private bool holdActivatedItem2 = false;

    public enum ResourceType { Health, Mana, Stamina }

    void Update()
    {
        if (item1CooldownTimer > 0f) item1CooldownTimer -= Time.deltaTime;
        if (item2CooldownTimer > 0f) item2CooldownTimer -= Time.deltaTime;

        if (isHoldingItem1)
        {
            holdTimerItem1 += Time.deltaTime;
            if (!holdActivatedItem1 && holdTimerItem1 >= holdThreshold)
            {
                ActivateHold(true);
                holdActivatedItem1 = true;
            }
        }

        if (isHoldingItem2)
        {
            holdTimerItem2 += Time.deltaTime;
            if (!holdActivatedItem2 && holdTimerItem2 >= holdThreshold)
            {
                ActivateHold(false);
                holdActivatedItem2 = true;
            }
        }
    }

    #region Input Handlers
    public void BeginUseItem1()
    {
        if (item1CooldownTimer > 0f) return;
        isHoldingItem1 = true;
        holdTimerItem1 = 0f;
        holdActivatedItem1 = false;
    }

    public void ReleaseUseItem1()
    {
        if (!isHoldingItem1) return;
        isHoldingItem1 = false;
        item1CooldownTimer = item1CooldownDuration;

        if (!holdActivatedItem1)
        {
            if (holdTimerItem1 >= holdThreshold)
            {
                ActivateHold(true);
                holdActivatedItem1 = true;
            }
            else
            {
                ActivateClick(true);
            }
        }
    }

    public void BeginUseItem2()
    {
        if (item2CooldownTimer > 0f) return;
        isHoldingItem2 = true;
        holdTimerItem2 = 0f;
        holdActivatedItem2 = false;
    }

    public void ReleaseUseItem2()
    {
        if (!isHoldingItem2) return;
        isHoldingItem2 = false;
        item2CooldownTimer = item2CooldownDuration;

        if (!holdActivatedItem2)
        {
            if (holdTimerItem2 >= holdThreshold)
            {
                ActivateHold(false);
                holdActivatedItem2 = true;
            }
            else
            {
                ActivateClick(false);
            }
        }
    }
    #endregion

    #region Resource Helpers
    private Stat GetStatFromType(ResourceType type)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats == null) return null;

        return type switch
        {
            ResourceType.Health => stats.health,
            ResourceType.Mana => stats.mana,
            ResourceType.Stamina => stats.stamina,
            _ => null
        };
    }
    #endregion

    #region Activation Logic
    private void ActivateClick(bool isPrimary)
    {
        if (isPrimary)
        {
            Stat stat = GetStatFromType(item1ResourceType);
            if (stat != null)
            {
                ResourceLibrary.DeductResources(GetComponent<PlayerStats>(), stat, item1Cost, item1UsePercentage);
            }

            if (useItem1Behavior == UseBehavior.SpawnPrefab && useItem1Prefab != null)
            {
                GameObject obj = Instantiate(useItem1Prefab, transform.position, Quaternion.identity);
                var logic = obj.GetComponent<WeaponLogic>();
                if (logic != null) logic.Activate(true, false);
            }
            else if (spawnedWeapon1 != null)
            {
                spawnedWeapon1.Activate(true, false);
            }
        }
        else
        {
            Stat stat = GetStatFromType(item2ResourceType);
            if (stat != null)
            {
                ResourceLibrary.DeductResources(GetComponent<PlayerStats>(), stat, item2Cost, item2UsePercentage);
            }

            if (useItem2Behavior == UseBehavior.SpawnPrefab && useItem2Prefab != null)
            {
                GameObject obj = Instantiate(useItem2Prefab, transform.position, Quaternion.identity);
                var logic = obj.GetComponent<WeaponLogic>();
                if (logic != null) logic.Activate(false, false);
            }
            else if (spawnedWeapon2 != null)
            {
                spawnedWeapon2.Activate(false, false);
            }
        }
    }

    private void ActivateHold(bool isPrimary)
    {
        if (isPrimary)
        {
            Stat stat = GetStatFromType(holdItem1ResourceType);
            if (stat != null)
            {
                ResourceLibrary.DeductResources(GetComponent<PlayerStats>(), stat, holdItem1Cost, holdItem1UsePercentage);
            }

            if (holdItem1Behavior == UseBehavior.SpawnPrefab && holdItem1Prefab != null)
            {
                GameObject obj = Instantiate(holdItem1Prefab, transform.position, Quaternion.identity);
                var logic = obj.GetComponent<WeaponLogic>();
                if (logic != null) logic.Activate(true, true);
            }
            else if (spawnedHoldWeapon1 != null)
            {
                spawnedHoldWeapon1.Activate(true, true);
            }
        }
        else
        {
            Stat stat = GetStatFromType(holdItem2ResourceType);
            if (stat != null)
            {
                ResourceLibrary.DeductResources(GetComponent<PlayerStats>(), stat, holdItem2Cost, holdItem2UsePercentage);
            }

            if (holdItem2Behavior == UseBehavior.SpawnPrefab && holdItem2Prefab != null)
            {
                GameObject obj = Instantiate(holdItem2Prefab, transform.position, Quaternion.identity);
                var logic = obj.GetComponent<WeaponLogic>();
                if (logic != null) logic.Activate(false, true);
            }
            else if (spawnedHoldWeapon2 != null)
            {
                spawnedHoldWeapon2.Activate(false, true);
            }
        }
    }
    #endregion

    #region Spawned Weapon Registration
    public void RegisterSpawnedWeapon(GameObject weapon, bool isPrimary, bool isHold = false)
    {
        WeaponLogic logic = weapon.GetComponent<WeaponLogic>();
        if (logic != null)
        {
            if (isHold)
            {
                if (isPrimary) spawnedHoldWeapon1 = logic;
                else spawnedHoldWeapon2 = logic;
            }
            else
            {
                if (isPrimary) spawnedWeapon1 = logic;
                else spawnedWeapon2 = logic;
            }
        }
    }

    public void UnregisterSpawnedWeapon(GameObject weapon, bool isPrimary, bool isHold = false)
    {
        WeaponLogic logic = weapon.GetComponent<WeaponLogic>();
        if (logic != null)
        {
            if (isPrimary)
            {
                if (isHold && spawnedHoldWeapon1 == logic) spawnedHoldWeapon1 = null;
                else if (!isHold && spawnedWeapon1 == logic) spawnedWeapon1 = null;
            }
            else
            {
                if (isHold && spawnedHoldWeapon2 == logic) spawnedHoldWeapon2 = null;
                else if (!isHold && spawnedWeapon2 == logic) spawnedWeapon2 = null;
            }
        }
    }
    #endregion
}

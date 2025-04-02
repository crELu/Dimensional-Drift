using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.VFX;
using Collider = UnityEngine.Collider;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager main;
    public static float3 Position => burstPos.Data.Position;
    private class IntFieldKey {}
    public static readonly SharedStatic<PlayerDataBurst> burstPos = SharedStatic<PlayerDataBurst>.GetOrCreate<PlayerManager, IntFieldKey>();

    public struct PlayerDataBurst
    {
        public float3 Position;
        public float Damage;
    }
    
    public static float waveTimer, maxWaveTimer;
    public static int waveCount;
    private static int T;

    public PlayerMovement movement;
    public PlayerInventory inventory;
    
    private InputAction _fireAction;
    private InputAction _weaponUpAction;
    private InputAction _weaponDownAction;
    private InputAction _scanAction;
    [Header("Stats Settings")] 
    public List<CharacterAugment> augments;
    [SerializeField] private CharacterStats stats;
    private AllStats _extraStats;
    [SerializeField] private CharacterStats baseStats;
    public float MaxHealth => baseStats.flatHealth;
    public float MaxShield => baseStats.flatShield;
    private float ShieldRegen => baseStats.shieldRegen;
    private float MaxAmmo => baseStats.flatAmmo;
    private float AmmoRegen => baseStats.ammoRegen;
    public float DashCd => baseStats.dashCd;
    public float PickupRadius => baseStats.pickupRadius;
    public float BoostRegen => baseStats.boostRegen;
    public bool FullHealth => Mathf.Approximately(health, MaxHealth);
    public bool FullShield => Mathf.Approximately(shield, MaxShield);
    public float health, shield;
    [field:SerializeField] public float Ammo { get; private set; }
    public RawImage ammoText, waveImage;
    [SerializeField] private TextMeshProUGUI velocityText;
    public float velocity;
    [Header("Weapon Settings")] 
    public PlayerWeapon CurrentWeapon => weapons[currentWeapon];
    public int currentWeapon;
    public List<PlayerWeapon> weapons;
    public List<Image> weaponSlots;
    public Sprite weaponSelected, weaponUnselected;
    public GameObject infiniteAmmo;
    
    public static List<Attack> Bullets => main.CurrentWeapon.Bullets;
    public RectTransform hp, sd;
    public TextMeshProUGUI waveCounter, newWaveText;
    public RawImage newWaveImage;
    public VisualEffect minimap, overlay;
    public RectTransform minimapIcon;
    public GraphicsBuffer MinimapPos, OverlayPos;
    [HideInInspector] public CursorLockMode targetCursorMode;

    [Header("Sound Settings")]
    [SerializeField] private AudioSource PlayerDamageTrack;
    [SerializeField] private AudioClip DamageSFX;
    private bool _isScanning;
    public float ScanRadius { get; private set; }
    public int ScanState { get; private set; }

    void Start()
    {
        _fireAction = InputSystem.actions.FindAction("Fire 1");
        _weaponUpAction  = InputSystem.actions.FindAction("Weapon Up");
        _weaponDownAction  = InputSystem.actions.FindAction("Weapon Down");
        _scanAction  = InputSystem.actions.FindAction("Scan");
        main = this;
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        Application.targetFrameRate = 60;
        CalcStats();
        T = Shader.PropertyToID("_t");
    }
    
    public void AddAugment(Augment augment)
    {
        if (augment is CharacterAugment charAug)
            augments.Add(charAug);
        else if (augment is StatsCharAugment statsAug)
            _extraStats += statsAug.GetStats(new AllStats{characterStats = baseStats}).characterStats;
        else
        {
            Debug.Log($"Wrong augment type {augment.Target} for character.");
        }

        CalcStats();
    }
    
    public void AddStats(AllStats s)
    {
        _extraStats += s;
    }

    void Update()
    {
        CheckHealth();
        DoAttack();
        CalcStats();
        velocityText.text = $"{velocity:F0}";
        waveCounter.text = $"{waveCount}";
        ammoText.material.SetFloat(T, Ammo/MaxAmmo);
        waveImage.material.SetFloat(T, waveTimer/maxWaveTimer);
        AddAmmo(AmmoRegen * Time.deltaTime);
        if (_scanAction.triggered && !_isScanning)
        {
            StartCoroutine(Scan());
        }
        if (_weaponDownAction.triggered)
        {
            currentWeapon++;
            currentWeapon %= weapons.Count;
        } else if (_weaponUpAction.triggered)
        {
            currentWeapon--;
            if (currentWeapon < 0) currentWeapon = weapons.Count - 1;
        }

        for (int i = 0; i < weaponSlots.Count; i++)
        {
            if (currentWeapon == i) weaponSlots[i].sprite = weaponSelected;
            else weaponSlots[i].sprite = weaponUnselected;
        }
        infiniteAmmo.SetActive(currentWeapon == 0);

        var v = transform.forward;
        v.y = 0;
        minimapIcon.transform.rotation = Quaternion.Euler(0, 0, -Quaternion.LookRotation(v).eulerAngles.y);
        transform.position = Vector3.Lerp(transform.position, burstPos.Data.Position, 20f * Time.deltaTime);
    }

    public void StartNewWave(int num)
    {
        newWaveText.text = $"- Wave {num} -";
        StartCoroutine(Wave());
    }
    
    private IEnumerator Wave()
    {
        float t = 0;
        var a = newWaveImage.color;
        var b = newWaveText.color;
        newWaveImage.gameObject.SetActive(true);
        while (t < 3)
        {
            t += Time.deltaTime;
            if (t > 2)
            {
                newWaveImage.color = new Color(a.r, a.g, a.b, a.a*(3-t));
                newWaveText.color = new Color(b.r, b.g, b.b, b.a*(3-t));
            }
            yield return new WaitForSecondsRealtime(0);
        }
        newWaveImage.color = a;
        newWaveText.color = b;
        newWaveImage.gameObject.SetActive(false);
    }

    private IEnumerator Scan()
    {
        _isScanning = true;
        float t = 0;
        while (t < 12)
        {
            t += Time.deltaTime;
            if (t < 10)
            {
                ScanState = 1;
                ScanRadius = t * 400;
            }
            else 
            {
                ScanState = -1;
            }
            
            yield return null;
        }

        ScanState = 0;
        _isScanning = false;
    }

    private void CalcStats()
    {
        CharacterStats s = _extraStats.characterStats;
        foreach (var augment in augments)
        {
            s += augment.GetStats(new AllStats{characterStats = baseStats}).characterStats;
        }
        baseStats = stats * s;
        burstPos.Data.Damage = baseStats.damageMultiplier;
    }

    public void DoDamage(float damage)
    {
        var sDamage = Mathf.Min(damage, shield);
        shield -= sDamage;
        damage -= sDamage;
        
        health -= damage;

        // Andrew TODO
        // PlayerDamageTrack.PlayOneShot(DamageSFX);
    }

    private void CheckHealth()
    {
        health = Mathf.Min(MaxHealth, health);
        shield += ShieldRegen * Time.deltaTime;
        shield = Mathf.Min(MaxShield, shield);
        sd.sizeDelta = new Vector2(shield / MaxShield * 1024, 32);
        hp.sizeDelta = new Vector2(health / MaxHealth * 1024, 64);
        if (health <= 0)
        {
            GameObject deathMessageObject = GameObject.Find("Death Message");
            if (deathMessageObject != null)
            {
                TextMeshProUGUI deathMessage =
                    deathMessageObject.GetComponent<TextMeshProUGUI>();

                deathMessage.SetText("You Died!");
            }
        }
    }

    private void DoAttack()
    {
        CurrentWeapon.Fire(this, _fireAction.IsPressed());
    }

    public void UseAmmo(float a)
    {
        if (a == 0) return;
        if (a<0) {Debug.Log("don t do that (use negative ammo)");}
        if (a>Ammo) {Debug.Log("dont do that (use more ammo than exists)");}
        Ammo -= a;
        Ammo = Mathf.Max(0, Ammo);
    }
    
    public void AddAmmo(float a)
    {
        if (a<0) Debug.Log("don t do that (add negative ammo)");
        Ammo += a;
        Ammo = Mathf.Min(MaxAmmo, Ammo);
    }
    
}
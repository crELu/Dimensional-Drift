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
    private float _ammoTime;
    public RawImage ammoText, waveImage;
    public RawImage hitEffect;
    [ColorUsage(true, true)] public Color shieldColor, healthColor;
    public AnimationCurve hitCurve;
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
    [SerializeField] private GameObject scan, noScan;
    public float ScanRadius { get; private set; }
    public int ScanState { get; private set; }
    private float _startTime;
    void Awake() {
        main = this;
        _startTime = Time.time;
    }
    
    void Start()
    {
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        Application.targetFrameRate = 60;
        CalcStats();
        T = Shader.PropertyToID("_t");
        hitEffect.material.SetFloat("_Alpha", 0);
    }
    
    public void AddAugment(Augment augment, int tier)
    {
        if (augment.Target != AugmentType.Character)
        {
            Debug.LogError($"Wrong augment type {augment.Target} for player.");
            return;
        }
        if (tier < 0 || tier > 2)
        {
            Debug.LogError($"Invalid augment tier {tier}.");
            return;
        }

        if (augment is CharacterAugment coreAug)
        {
            Predicate<CharacterAugment> nameChecker = e => e.Id == coreAug.Id;
            if (tier == 0)
            {
                if (augments.Exists(nameChecker))
                {
                    Debug.LogError($"Tried to add T1 augment {augment.Id} for player, but it already exists.");
                    return;
                }
                if (augments.Count >= 3)
                {
                    Debug.LogError($"Tried to add T1 augment {augment.Id} for player, but there are already 3.");
                    
                }
                    
                var clone = Instantiate(coreAug);
                clone.Stacks = 1;
                augments.Add(clone);
            }
            else if (tier == 1)
            {
                if (!augments.Exists(e => nameChecker(e) && e.Stacks == 1))
                {
                    Debug.LogError($"Tried to add T2 augment {augment.Id} for player, but no T1 exists.");
                    return;
                }
                augments.Find(nameChecker).Stacks++;
            }
            else if (tier == 2)
            {
                if (!augments.Exists(e => nameChecker(e) && e.Stacks == 2))
                {
                    Debug.LogError($"Tried to add T3 augment {augment.Id} for player, but no T2 exists.");
                    return;
                }
                augments.Find(nameChecker).Stacks++;
            }
        }
    }
    
    public void AddStats(AllStats s)
    {
        _extraStats += s;
    }

    private Coroutine _scanner;
    
    void Update()
    {
        CheckHealth();
        DoAttack();
        CalcStats();
        velocityText.text = $"{velocity:F0}";
        waveCounter.text = $"{waveCount}";
        ammoText.material.SetFloat(T, Ammo/MaxAmmo);
        waveImage.material.SetFloat(T, waveTimer/maxWaveTimer);
        _ammoTime -= Time.deltaTime;
        if (_ammoTime < 0) AddAmmo(AmmoRegen * Time.deltaTime);
        
        if (PlayerInputs.main.Scan)
        {
            if (_scanner == null) _scanner = StartCoroutine(Scan());
            else _isScanning = false;
        }

        if (PlayerInputs.main.Weapon != -1)
        {
            currentWeapon = PlayerInputs.main.Weapon;
        } else if (PlayerInputs.main.WeaponDown)
        {
            currentWeapon++;
            currentWeapon %= weapons.Count;
        } else if (PlayerInputs.main.WeaponUp)
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
    
    public (bool, int) ValidAugment(Augment augment)
    {
        if (augment.Target != AugmentType.Character) return (false, 0);
        if (!augments.Exists(e => e.name == augment.name))
        {
            if (augments.Count >= 3) return (false, 0);
            return (true, 0);
        }
        var aug = augments.Find(e => e.name == augment.name);
        if (aug.Stacks >= 3) return (false, 0);
        return (true, aug.Stacks);
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
        while (_isScanning)
        {
            scan.SetActive(true);
            noScan.SetActive(false);
            t += Time.deltaTime;
            if (t < 10)
            {
                ScanState = 1;
                ScanRadius = t * 400;
            }
            yield return null;
        }
        scan.SetActive(false);
        noScan.SetActive(true);
        t = 2;
        while (t>0)
        {
            t -= Time.deltaTime;
            ScanState = -1;
            yield return null;
        }

        ScanState = 0;
        _scanner = null;
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
        if (damage <= 0) return;
        var sDamage = Mathf.Min(damage, shield);
        shield -= sDamage;
        damage -= sDamage;
        _dispColor = damage > 0 ? healthColor : shieldColor;
        health -= damage;

        // Andrew TODO
        // PlayerDamageTrack.PlayOneShot(DamageSFX);
        if (_damageCoroutine == null) _damageCoroutine = StartCoroutine(DamageEffect());
        else _damageInd = 1;
    }

    private float _damageInd;
    private Coroutine _damageCoroutine;
    private Color _dispColor;
    private IEnumerator DamageEffect()
    {
        _damageInd = 1;
        while (_damageInd > 0)
        {
            _damageInd -= Time.deltaTime/.4f;
            hitEffect.material.SetFloat("_Alpha", hitCurve.Evaluate(_damageInd));
            hitEffect.material.SetColor("_Color", _dispColor);
            yield return null;
        }
        _damageCoroutine = null;
    }
    
    public void Heal(float healing)
    {
        health += healing;
        health = Mathf.Min(MaxHealth, health);
    }

    private void CheckHealth()
    {
        health = Mathf.Min(MaxHealth, health);
        shield += ShieldRegen * Time.deltaTime;
        shield = Mathf.Min(MaxShield, shield);
        sd.sizeDelta = new Vector2(shield / MaxShield * 850, 32);
        hp.sizeDelta = new Vector2(health / MaxHealth * 1112, 64);
        if (health <= 0)
        {
            Time.timeScale = 0;
            PlayerStats.main.UpdateUI(waveCount, Time.time - _startTime);
        }
    }

    private void DoAttack()
    {
        CurrentWeapon.Fire(this, PlayerInputs.main.Fire);
    }

    public void UseAmmo(float a)
    {
        if (a == 0) return;
        if (a<0) {Debug.Log("don t do that (use negative ammo)");}
        if (a>Ammo) {Debug.Log("dont do that (use more ammo than exists)");}
        Ammo -= a;
        _ammoTime = 1;
        Ammo = Mathf.Max(0, Ammo);
    }
    
    public void AddAmmo(float a)
    {
        if (a<0) Debug.Log("don t do that (add negative ammo)");
        Ammo += a;
        Ammo = Mathf.Min(MaxAmmo, Ammo);
    }
    
}
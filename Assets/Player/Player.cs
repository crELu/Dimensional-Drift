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
    public static float3 Position => burstPos.Data;
    private class IntFieldKey {}
    public static readonly SharedStatic<float3> burstPos = SharedStatic<float3>.GetOrCreate<PlayerManager, IntFieldKey>();
    
    public static float waveTimer, maxWaveTimer;
    public static int waveCount;
    
    public PlayerMovement movement;
    public PlayerInventory inventory;
    
    private InputAction _fireAction;
    private InputAction _weaponUpAction;
    private InputAction _weaponDownAction;
    [Header("Stats Settings")] 
    public List<CharacterAugment> augments;
    public CharacterStats stats;
    protected CharacterStats AddonStats;
    public float health, shield;
    [field:SerializeField] public float Ammo { get; private set; }
    public static bool fire;
    
    [Header("Weapon Settings")] 
    public PlayerWeapon CurrentWeapon => weapons[currentWeapon];
    public int currentWeapon;
    public List<PlayerWeapon> weapons;
    public List<Image> weaponSlots;
    public Sprite weaponSelected, weaponUnselected;
    
    public static List<Attack> Bullets => main.CurrentWeapon.Bullets;
    public RectTransform hp, sd;
    public TextMeshProUGUI waveCounter;
    private Animator _anim;
    
    public VisualEffect minimap;
    public RectTransform minimapIcon;
    public GraphicsBuffer Px;
    [HideInInspector] public CursorLockMode targetCursorMode;

    [Header("Sound Settings")]
    [SerializeField] private AudioSource PlayerDamageTrack;
    [SerializeField] private AudioClip DamageSFX;
    
    void Start()
    {
        _fireAction = InputSystem.actions.FindAction("Fire 1");
        _weaponUpAction  = InputSystem.actions.FindAction("Weapon Up");
        _weaponDownAction  = InputSystem.actions.FindAction("Weapon Down");
        _anim = GetComponent<Animator>();
        main = this;
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
    }
    
    public void AddAugment(Augment augment)
    {
        if (augment is CharacterAugment charAug)
            augments.Add(charAug);
        else if (augment is StatsCharAugment statsAug)
            AddonStats += statsAug.GetStats().characterStats;
        else
        {
            Debug.Log($"Wrong augment type {augment.Target} for character.");
        }
    }

    void Update()
    {
        CheckHealth();
        DoAttack();
        waveCounter.text = $"Wave in {Mathf.Ceil(waveTimer)}s";
        burstPos.Data = transform.position;
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

        var v = transform.forward;
        v.y = 0;
        minimapIcon.transform.rotation = Quaternion.Euler(0, 0, -Quaternion.LookRotation(v).eulerAngles.y);
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
        health = Mathf.Min(stats.flatHealth, health);
        shield += stats.shieldRegen * Time.deltaTime;
        shield = Mathf.Min(stats.flatShield, shield);
        sd.sizeDelta = new Vector2(shield / stats.flatShield * 894, 32);
        hp.sizeDelta = new Vector2(health / stats.flatHealth * 1024, 64);
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
        fire = CurrentWeapon.Fire(this, _fireAction.IsPressed());
    }

    public void UseAmmo(float a)
    {
        if (a<0) {Debug.Log("don t do that (use negative ammo)");}
        if (a>Ammo) {Debug.Log("dont do that (use more ammo than exists)");}
        Ammo -= a;
        Ammo = Mathf.Max(0, Ammo);
    }
    
    public void AddAmmo(float a)
    {
        if (a<0) Debug.Log("don t do that (add negative ammo)");
        Ammo += a;
        Ammo = Mathf.Min(100, Ammo);
    }
    
}
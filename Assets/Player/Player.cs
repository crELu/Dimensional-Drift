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
    
    public PlayerMovement movement;
    
    private InputAction _fireAction;
    
    [Header("Weapon Settings")] 
    public PlayerWeapon currentWeapon;

    public CharacterStats stats;
    public float health, shield;
    [field:SerializeField] public float Ammo { get; private set; }
    public static bool fire;
    public static List<Attack> Bullets => main.currentWeapon.Bullets;
    public RectTransform hp, sd;
    public TextMeshProUGUI waveCounter;
    private Animator _anim;
    
    public VisualEffect minimap;
    public RectTransform minimapIcon;
    public GraphicsBuffer Px;
    
    void Start()
    {
        _fireAction = InputSystem.actions.FindAction("Fire 1");
        _anim = GetComponent<Animator>();
        main = this;
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
    }

    void Update()
    {
        CheckHealth();
        DoAttack();
        waveCounter.text = $"Wave in {Mathf.Ceil(waveTimer)}s";
        burstPos.Data = transform.position;
        
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
    }

    private void CheckHealth()
    {
        health = Mathf.Min(stats.flatHealth, health);
        shield += stats.shieldRegen * Time.deltaTime;
        shield = Mathf.Min(stats.flatShield, shield);
        sd.sizeDelta = new Vector2(shield / stats.flatShield * 1000, 32);
        hp.sizeDelta = new Vector2(health / stats.flatHealth * 1000, 64);
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
        fire = currentWeapon.Fire(this, _fireAction.IsPressed());
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Collider = UnityEngine.Collider;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager main;
    public static Vector3 Position => main.transform.position;
    public PlayerMovement movement;
    
    private InputAction _fireAction;
    
    [Header("Weapon Settings")] 
    public PlayerWeapon currentWeapon;
    [field:SerializeField] public float Ammo { get; private set; }
    public static bool fire;
    public static List<Attack> Bullets => main.currentWeapon.Bullets;

    private Animator _anim;
    
    void Start()
    {
        _fireAction = InputSystem.actions.FindAction("Fire 1");
        _anim = GetComponent<Animator>();
        main = this;
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
    }

    void Update()
    {
        DoAttack();
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
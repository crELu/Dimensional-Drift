using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Collider = UnityEngine.Collider;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager main;
    [Header("Movement Settings")]
    public float baseAccel;
    public float moveSpeed, rotateSpeed, rollSpeed;
    public AnimationCurve accelSpeedScaling;
    public AnimationCurve accelDotScaling;
    public float baseDrag = 5f;
    public AnimationCurve dragSpeedScaling;
    
    [Header("Camera Settings")]
    public Transform cameraFixture;
    public Quaternion normalCameraRot, orthoCameraRot;
    public Vector3 normalCameraPos, orthoCameraPos;
    
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _rollLAction;
    private InputAction _rollRAction;
    private InputAction _dashAction;
    private InputAction _fireAction;

    [Header("Dash Settings")]
    public float dashDur;
    public float dashCooldown;
    public float dashSpeed;
    public AnimationCurve dashCurve;
    private float _dashCd;
    private float _dashSpeed;
    private bool _isDashing;
    private Vector3 _dashDir;
    
    [Header("Weapon Settings")] 
    public PlayerWeapon currentWeapon;
    private float _attackCd;
    public static bool fire;
    public static NativeArray<Bullet> Bullets => main.currentWeapon.Bullets;
    
    private Animator _anim;
    [HideInInspector] public float3 position;
    private bool Dim3 => DimensionManager.currentDim == 0;
    private Camera _camera;
    public Vector3 Right => Dim3 ? transform.right : _camera.transform.right;
    public Vector3 MoveForward => Dim3 ? transform.forward : _camera.transform.up;
    private Vector2 MoveInput => _moveAction.ReadValue<Vector2>();
    private Vector3 LookInput
    {
        get
        {
            var v = _lookAction.ReadValue<Vector2>();
            return new Vector3(v.x, v.y, rollSpeed * (_rollLAction.ReadValue<float>() - _rollRAction.ReadValue<float>()));
        }
    }

    void Start()
    {
        _moveAction = InputSystem.actions.FindAction("Move");
        _lookAction = InputSystem.actions.FindAction("Look");
        _rollLAction = InputSystem.actions.FindAction("Roll Left");
        _rollRAction = InputSystem.actions.FindAction("Roll Right");
        _dashAction = InputSystem.actions.FindAction("Dash");
        _fireAction = InputSystem.actions.FindAction("Fire 1");
        _anim = GetComponent<Animator>();
        _camera = Camera.main;
        main = this;
        DimensionManager.dimSwitch.AddListener(SwitchDims);
        SwitchDims();
    }

    void Update()
    {
        transform.position = position;
        DoAttack();
        DoRotation();
    }

    private void DoAttack()
    {
        _attackCd -= Time.deltaTime;
        if (_attackCd <= 0 && _fireAction.IsPressed())
        {
            fire = true;
            _attackCd = currentWeapon.Cd;
        }
        else
        {
            fire = false;
        }
    }

    private void SwitchDims()
    {
        Debug.Log(DimensionManager.currentDim);
        _camera.transform.localRotation = Quaternion.identity;
        if (DimensionManager.currentDim > 0) // D < 3
        {
            _camera.transform.localPosition = orthoCameraPos;
            Camera.main.orthographic = true;
            Cursor.lockState = CursorLockMode.Confined;
        }
        else // D = 3
        {
            _camera.transform.localPosition = normalCameraPos;
            Camera.main.orthographic = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void DoRotation()
    {
        if (Dim3)
        {
            cameraFixture.localRotation = normalCameraRot;
            var v = LookInput * rotateSpeed * Time.deltaTime;
            var rot = Quaternion.Euler(-v.y, v.x, -v.z);
            transform.rotation *= rot;
        }
        else
        {
            float r = LookInput.z * rotateSpeed * Time.deltaTime;;
            _camera.transform.rotation *= Quaternion.Euler(0, 0, -r);
            
            transform.rotation = Quaternion.Euler(0, transform.rotation.y, 0);
            
            
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            Vector3 mouseScreenPosition = Input.mousePosition; 
            Vector3 mouseDirection = mouseScreenPosition - screenCenter;
            
            float angle = Mathf.Atan2(mouseDirection.y, mouseDirection.x);
            float angleDegrees = -angle * Mathf.Rad2Deg + 90f;
            Quaternion targetRotation = Quaternion.Euler(0f, angleDegrees, 0f);

            transform.rotation = targetRotation;//Quaternion.RotateTowards(transform.rotation, targetRotation, 180 * Time.deltaTime);
            
            cameraFixture.rotation = orthoCameraRot;
        }
    }
    
    public Vector3 GetMovement(Vector3 velocity)
    {
        Vector3 d = MoveInput.y * MoveForward + MoveInput.x * Right;
        
        if (d != Vector3.zero)
        {
            float speedMultipliers = accelDotScaling.Evaluate(Vector3.Dot(d, velocity)) *
                                     accelSpeedScaling.Evaluate(velocity.magnitude/moveSpeed);
            d = baseAccel * speedMultipliers * d;
        }
        
        d += -velocity * (baseDrag * dragSpeedScaling.Evaluate(velocity.magnitude)); 
        return d;
    }
    
    public Vector3 GetDash()
    {
        if (_isDashing) return _dashDir * _dashSpeed;
        
        _dashCd -= Time.deltaTime;
        if (!(_dashAction.ReadValue<float>() > 0) || _dashCd > 0) return Vector3.zero;
        
        var input = MoveInput;

        if (Dim3)
        {
            if (Vector2.Dot(input, Vector2.up) > 0.5f)
            {
                _dashDir = MoveForward;
                StartCoroutine(Dash());
            }
            else if (Vector2.Dot(input, Vector2.up) > -0.5f)
            {
                _dashDir = input.x > 0 ? Right : -Right;
                StartCoroutine(Dash());
            }
        }
        else
        {
            _dashDir = new Vector3(input.x, 0, input.y).normalized;
            StartCoroutine(Dash());
        }
        
        
        return _dashDir * _dashSpeed;
    }

    private IEnumerator Dash()
    {
        _isDashing = true;
        _dashCd = dashCooldown;
        float a = 0;
        while (a < dashDur)
        {
            a += Time.deltaTime;
            _dashSpeed = dashSpeed * dashCurve.Evaluate(a / dashDur);
            yield return new WaitForSeconds(0);
        }
        _isDashing = false;
    }
}
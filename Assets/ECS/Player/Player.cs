using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using Collider = UnityEngine.Collider;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager main;
    [Header("Movement Settings")]
    public float baseAccel;
    public float moveSpeed, rotateSpeed, rollSpeed, dashSpeed, dashCooldown;
    public AnimationCurve accelSpeedScaling;
    public AnimationCurve accelDotScaling;
    public float baseDrag = 5f;
    public AnimationCurve dragSpeedScaling;
    [Header("Camera Settings")]
    public Transform cameraFixture;
    
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _rollLAction;
    private InputAction _rollRAction;
    private InputAction _dashAction;
    
    private float _dashCd;
    
    private Animator _anim;
    [HideInInspector] public float3 position;
    
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
        _anim = GetComponent<Animator>();
        main = this;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        transform.position = position;
        DoRotation();
        _dashCd -= Time.deltaTime;
    }

    private void DoRotation()
    {
        var v = LookInput * rotateSpeed * Time.deltaTime;
        var rot = Quaternion.Euler(-v.y, v.x, -v.z);
        transform.rotation = transform.rotation * rot;
        Debug.DrawRay(transform.position, transform.forward*7);
    }
    
    public Vector3 GetMovement(Vector3 velocity)
    {
        Vector3 d = MoveInput.y * transform.forward + MoveInput.x * transform.right;
        
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
        if (!(_dashAction.ReadValue<float>() > 0) || _dashCd > 0) return Vector3.zero;
        var input = MoveInput;
        Vector3 dashDirection;
        if (Vector2.Dot(input, Vector2.up) > 0.5f)
        {
            dashDirection = transform.forward * dashSpeed;
            _dashCd = dashCooldown;
        }
        else if (Vector2.Dot(input, Vector2.up) > -0.5f)
        {
            dashDirection = (input.x > 0 ? transform.right : -transform.right) * dashSpeed;
            _dashCd = dashCooldown;
        }
        else
        {
            dashDirection = Vector3.zero;
        }

        return dashDirection;
    }
    
    public Vector3 GetRotation(Quaternion currentFacing)
    {
        Vector3 d = LookInput;
        return d;
    }
}
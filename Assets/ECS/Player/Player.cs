using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.Burst;
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
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager main;
    [Header("Movement Settings")]
    public float baseAccel;
    public float moveSpeed, rotateSpeed;
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
    private InputAction _dashAction;
    private InputAction _flyUpAction;
    private InputAction _flyDownAction;

    [Header("Dash Settings")] public float dashDur;
    [FormerlySerializedAs("dashCd")] public float dashCooldown;
    public float dashSpeed;
    public AnimationCurve dashCurve;
    private float _dashCd;
    private float _dashSpeed;
    private bool _isDashing;
    private Vector3 _dashDir;
    
    private Animator _anim;
    [HideInInspector] public float3 position;
    private bool Dim3 => DimensionManager.currentDim == 0;
    private Camera _camera;
    public Vector3 Right => Dim3 ? transform.right : _camera.transform.right;
    public Vector3 MoveForward => Dim3 ? transform.forward : _camera.transform.up;
    
    public Vector3 MoveUp => Dim3 ? transform.up : Vector3.zero;
    private Vector2 MoveInput => _moveAction.ReadValue<Vector2>();
    
    private float FlyInput => _flyUpAction.ReadValue<float>() - _flyDownAction.ReadValue<float>();
    private Vector3 LookInput
    {
        get
        {
            var v = _lookAction.ReadValue<Vector2>();
            return v;
        }
    }

    void Start()
    {
        _moveAction = InputSystem.actions.FindAction("Move");
        _lookAction = InputSystem.actions.FindAction("Look");
        _dashAction = InputSystem.actions.FindAction("Dash");
        _flyUpAction = InputSystem.actions.FindAction("Fly Up");
        _flyDownAction = InputSystem.actions.FindAction("Fly Down");
        _anim = GetComponent<Animator>();
        _camera = Camera.main;
        main = this;
        DimensionManager.dimSwitch.AddListener(SwitchDims);
        SwitchDims();
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        _camera.transform.localPosition = normalCameraPos;
    }

    void Update()
    {
        transform.position = position;
        DoRotation();
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
            cameraFixture.localRotation = normalCameraRot;
            Camera.main.orthographic = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void DoRotation()
    {   
        if (Dim3)
        {
            var inputRotation = LookInput * (rotateSpeed * Time.deltaTime);
            var characterRotation = Quaternion.Euler(0, inputRotation.x, 0);
            transform.rotation *= characterRotation;
            
            // Calculate camera pitch (up/down) using LookInput.y
            float cameraPitch = cameraFixture.localRotation.eulerAngles.x;
            if (cameraPitch > 180f)
                cameraPitch -= 360f;            
            cameraPitch -= LookInput.y * rotateSpeed * Time.deltaTime;
            cameraPitch = math.clamp(cameraPitch, -90f, 90f);
            cameraFixture.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
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
        Vector3 inputVector = MoveInput.y * MoveForward + MoveInput.x * Right + FlyInput * MoveUp;
        Vector3 impulse = Vector3.zero;
        if (inputVector != Vector3.zero)
        {
            float speedMultipliers = accelDotScaling.Evaluate(Vector3.Dot(inputVector, velocity)) *
                                     accelSpeedScaling.Evaluate(velocity.magnitude/moveSpeed);
            impulse = baseAccel * speedMultipliers * inputVector;
        }
        impulse -= velocity * (baseDrag * dragSpeedScaling.Evaluate(velocity.magnitude)); 
        return impulse;
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
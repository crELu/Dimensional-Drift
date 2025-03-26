using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using Collider = UnityEngine.Collider;
using Math = Unity.Physics.Math;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerMovement : MonoBehaviour
{
    [Header("Camera Settings")]
    public Quaternion normalCameraRot, orthoCameraRot;
    public Vector3 normalCameraPos, orthoCameraPos;
    public float orthographicSize;
    public AnimationCurve rotationCurve2D, movementCurve2D;
    public AnimationCurve rotationCurve3D, movementCurve3D;
    
    public float fov = 60f, near = .3f, far = 1000f;
    private float _aspect;
    [SerializeField] private MatrixBlender blender;
    private Matrix4x4 _ortho, _perspective;
    private float _pitch, _yaw;
    [Header("Movement Settings")]
    public float baseAccel;
    public float rotateSpeed, controllerRotateSpeed;
    public float boostMultiplier;
    public AnimationCurve accelSpeedScaling;
    public AnimationCurve accelDotScaling;
    public float baseDrag = 5f;
    public AnimationCurve forwardAlignmentScaling;
    public AnimationCurve dragSpeedScaling;
    public float wallForce;
    public MeshRenderer wall;
    
    [Header("Dash Settings")]
    public float dashDur;
    public float dashCooldown;
    public float dashSpeed;
    public AnimationCurve dashCurve;
    public AnimationCurve dashRollCurve;
    private float _dashCd;
    private float _dashSpeed;
    private bool _isDashing;
    private Vector3 _dashDir;
    
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _controllerLookAction;
    private InputAction _dashAction;
    private InputAction _sprintAction;
    private InputAction _flyUpAction;
    private InputAction _flyDownAction;
    
    private Animator _anim;
    private Camera _camera;
    public Vector2 RelativeMovement { get; private set; }

    [Header("Sound")]
    [SerializeField] private AudioSource DimSwitchTrack;
    [SerializeField] private AudioClip DimSwitchUp;
    [SerializeField] private AudioClip DimSwitchDown;
    
    
    public float3 Position
    {
        set => transform.position = value;
    }
    
    private bool Dim3 => DimensionManager.CurrentDim == 0;
    public Vector3 Right => Dim3 ? transform.right : _camera.transform.right;
    public Vector3 MoveForward => Dim3 ? transform.forward : _camera.transform.up;
    public Vector3 MoveUp => Dim3 ? transform.up : Vector3.zero;
    private Vector2 MoveInput => _moveAction.ReadValue<Vector2>();
    private float FlyInput => _flyUpAction.ReadValue<float>() - _flyDownAction.ReadValue<float>();
    public Quaternion LookRotation => Dim3 ? Camera.main.transform.rotation : transform.rotation;
    public GameObject crosshair;
    private bool _isUsingController;
    private Vector3 LookInput
    {
        get
        {
            var lookVector = _lookAction.ReadValue<Vector2>();
            return lookVector;
        }
    }

    private Vector3 ControllerLookInput
    {
        get
        {
            var lookVector = _controllerLookAction.ReadValue<Vector2>();
            return lookVector;
        }
    }
    
    void Start()
    {
        _moveAction = InputSystem.actions.FindAction("Move");
        _lookAction = InputSystem.actions.FindAction("Look");
        _controllerLookAction = InputSystem.actions.FindAction("Controller Look");
        _dashAction = InputSystem.actions.FindAction("Dash");
        _sprintAction = InputSystem.actions.FindAction("Sprint");
        _flyUpAction = InputSystem.actions.FindAction("Fly Up");
        _flyDownAction = InputSystem.actions.FindAction("Fly Down");
        _anim = GetComponent<Animator>();
        _camera = Camera.main;
        DimensionManager.DimSwitch.AddListener(SwitchDims);
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        //_camera.transform.localPosition = normalCameraPos;
        _camera.orthographicSize = orthographicSize;
        _camera.fieldOfView = fov;
        InputUser.onChange += HandleInputChange;
        
        _isUsingController = Gamepad.all.Count > 0;
        
        _aspect = (float) Screen.width / Screen.height;
        _ortho = Matrix4x4.Ortho(-orthographicSize * _aspect, orthographicSize * _aspect, -orthographicSize, orthographicSize, near, far);
        _perspective = Matrix4x4.Perspective(fov, _aspect, near, far);
        Camera.main.projectionMatrix = _perspective;
        SwitchDims();
    }
    void Update()
    {
        DoRotation();
        wall.material.SetVector("_PlayerPos", transform.position);
        //Debug.Log(_isUsingController);
    }    
    
    private void HandleInputChange(InputUser user, InputUserChange change, InputDevice device)
    {
        if (device != null && (change == InputUserChange.DevicePaired || change == InputUserChange.DeviceLost))
        {
            if (device is Keyboard || device is Mouse)
            {
                _isUsingController = false;
            }
            else if (device is Gamepad)
            {
                _isUsingController = true;
            }
        }
    }    
    
    private void SwitchDims()
    {
        Debug.Log(DimensionManager.CurrentDim);
        switch (DimensionManager.CurrentDim)
        {
            case Dimension.Three:
                StartCoroutine(DoCameraTransition());
                if (!_isUsingController)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    PlayerManager.main.targetCursorMode = CursorLockMode.Locked;

                    DimSwitchTrack.PlayOneShot(DimSwitchUp);
                }   
                crosshair.SetActive(true);
                break;
            case Dimension.Two:
                StartCoroutine(DoCameraTransition());
                if (!_isUsingController)
                {
                    CameraManager.main.Angle = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;
                    Cursor.lockState = CursorLockMode.Confined;
                    PlayerManager.main.targetCursorMode = CursorLockMode.Confined;

                    DimSwitchTrack.PlayOneShot(DimSwitchDown);
                }
                crosshair.SetActive(false);
                break;
            case Dimension.One:
                break;
        }
    }
    
    IEnumerator DoCameraTransition()
    {
        if (!Dim3 == _camera.orthographic)
        {
            yield break;
        }

        if (Dim3)
        {
            _camera.orthographic = false;
        }        
        
        blender.BlendToMatrix(Dim3 ? _perspective : _ortho, DimensionManager.Duration);
        
        while (DimensionManager.t < 1f)
        { 
            yield return null;
        }
        
        if (!Dim3)
        {
            _camera.orthographic = true;
        }
    }
    
    private static float DistanceFromFieldOfViewAndSize(float size, float fov)
        => size / (2.0f * Mathf.Tan(0.5f * Mathf.Deg2Rad * fov));
    
    private static float SizeFromDistanceAndFieldOfView(float distance, float fov)
        => 2.0f * distance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
    
    private static float FieldOfViewFromSizeAndDistance(float size, float distance)
        => 2.0f * Mathf.Atan(size * 0.5f / distance) * Mathf.Rad2Deg;
       
    private void DoRotation()
    {
        if (Dim3)
        {
            Vector2 inputRotation;
            if (_isUsingController)
            {
                inputRotation = ControllerLookInput * (controllerRotateSpeed * Time.deltaTime);
            }
            else
            {
                inputRotation = LookInput * (rotateSpeed * Time.deltaTime);
            }

            _pitch -= inputRotation.y;
            _yaw += inputRotation.x;
            _pitch = Mathf.Clamp(_pitch, -85, 85);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0);
        }
        else
        {
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            Vector3 mouseScreenPosition = Input.mousePosition; 
            Vector3 mouseDirection = mouseScreenPosition - screenCenter;

            float angle;
            if (_isUsingController)
            {
                if (ControllerLookInput == Vector3.zero) return;
                angle = Mathf.Atan2(ControllerLookInput.y, ControllerLookInput.x);
            }
            else
            {
                angle = Mathf.Atan2(mouseDirection.y, mouseDirection.x);
            }

            float angleDegrees = -angle * Mathf.Rad2Deg + 90 + CameraManager.main.Angle;
            Quaternion targetRotation = Quaternion.Euler(0f, angleDegrees, 0f);
            transform.rotation = targetRotation;
        }
    }
    
    public Vector3 GetMovement(Vector3 velocity)
    {
        var moveVector = new Vector3(MoveInput.x, FlyInput, MoveInput.y);
        var moveDir = moveVector.z * MoveForward + moveVector.x * Right + moveVector.y * MoveUp;
        var f = Vector3.ProjectOnPlane(moveDir, transform.up).normalized;
        var r = Vector3.Cross(velocity + transform.forward * 5, transform.up).normalized;
        var u = Vector3.Cross(velocity + transform.forward * 5, transform.right).normalized;
        RelativeMovement = new Vector2(Vector3.Dot(f, r), Vector3.Dot(f, u));
        if (Dim3)
        {
            moveDir += FlyInput * MoveUp;
        }
        Vector3 impulse = Vector3.zero;
        if (moveDir != Vector3.zero)
        {
            float speedMultipliers = accelDotScaling.Evaluate(Vector3.Dot(moveDir, velocity)) *
                                     accelSpeedScaling.Evaluate(velocity.magnitude) *
                                     (_sprintAction.ReadValue<float>() != 0 ? boostMultiplier : 1) *
                                     (Dim3 ? forwardAlignmentScaling.Evaluate(moveVector.normalized.z) : 1);
            Debug.Log(_sprintAction.ReadValue<float>() != 0);
            impulse = baseAccel * speedMultipliers * moveDir;
        }
        impulse -= velocity * (baseDrag * dragSpeedScaling.Evaluate(velocity.magnitude)); 
        if (transform.position.magnitude > 900)
        {
            impulse -= Vector3.ClampMagnitude(transform.position, transform.position.magnitude - 900 + 100) * wallForce;
        }
        
        return impulse;
    }
    
    public Vector3 GetDash()
    {
        if (_isDashing) return _dashDir * _dashSpeed;
        
        _dashCd -= Time.deltaTime;
        if (_dashAction.ReadValue<float>() <= 0 || _dashCd > 0) return Vector3.zero;
        
        var input = MoveInput;
        _dashDir = MoveForward * input.y + Right * input.x;
        if (Dim3)
        {
            _dashDir += FlyInput * MoveUp;
        }

        if (_dashDir.Equals(float3.zero))
        {
            _dashDir = transform.forward;
        }
        else
        {
            _dashDir.Normalize();
        }
        StartCoroutine(Dash(CameraManager.main.RollDir));

        
        
        return _dashDir * _dashSpeed;
    }

    private IEnumerator Dash(float dir)
    {
        _isDashing = true;
        _dashCd = dashCooldown;
        float timer = 0;
        while (timer < dashDur)
        {
            // float dashZoomOutStep = zoomOutMultiplier * timer / dashDur;
            // if (dashZoomOutStep > zoomOutMultiplier - 1)
            // {
            //     dashZoomOutStep = zoomOutMultiplier - dashZoomOutStep;
            // }
            //
            // float smoothStep = Mathf.SmoothStep(0, 1, dashZoomOutStep);
            // if (Dim3)
            // {
            //     _camera.fieldOfView = fov + Mathf.Lerp(0, dashFovOffset, smoothStep);
            // }
            // else
            // {
            //     _camera.orthographicSize = orthographicSize + Mathf.Lerp(0, dashOrthoSizeOffset, smoothStep);
            // }
            CameraManager.main.extraRoll = dir * 360 * dashRollCurve.Evaluate(timer / dashDur);
            timer += Time.deltaTime;
            _dashSpeed = dashSpeed * dashCurve.Evaluate(timer / dashDur);
            yield return null;
        }

        CameraManager.main.extraRoll = 0;
        _isDashing = false;
    }
}



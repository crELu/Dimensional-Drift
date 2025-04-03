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
using UnityEngine.UI;
using UnityEngine.VFX;
using Collider = UnityEngine.Collider;
using Math = Unity.Physics.Math;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerMovement : MonoBehaviour
{
    private static readonly int T = Shader.PropertyToID("_t");
    private static readonly int T2 = Shader.PropertyToID("_t2");
    private static readonly int PlayerPos = Shader.PropertyToID("_PlayerPos");

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
    public RawImage boostImage;
    public bool invertX = false;
    public bool invertY = false;
    
    [Header("Dash Settings")]
    public float dashDur;
    public float dashSpeed;
    public AnimationCurve dashCurve;
    public AnimationCurve dashRollCurve;
    private float _dashCd;
    private float _dashSpeed;
    private bool _isDashing;
    private Vector3 _dashDir;
    
    private Animator _anim;
    private Camera _camera;
    public Vector3 RelativeMovement { get; private set; }

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
    public Quaternion LookRotation => Dim3 ? Camera.main.transform.rotation : transform.rotation;
    public GameObject crosshair;
    
    private bool _isUsingController;
    private float _boost;
    private float _boostUse;
    private float _boostTime;
    
    void Start()
    {
        // Debug.Log("rotate speed: " + rotateSpeed);
        

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
        SettingsManager.Instance.SetBaseMouseSensitivity(rotateSpeed);
    }
    
    void Update()
    {
        DoRotation();
        if (!_camera) _camera = Camera.main;
        boostImage.material.SetFloat(T, _boost/100);
        boostImage.material.SetFloat(T2, _boostUse/100);
        _boostUse -= Time.deltaTime * 40;
        _boostUse = Mathf.Max(0, _boostUse);
        wall.material.SetVector(PlayerPos, transform.position);
        if (Time.time > _boostTime + .7f)
        {
            _boost += Time.deltaTime * PlayerManager.main.BoostRegen;
            _boost = Mathf.Min(100, _boost);
        }
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
        PlayerInputs.main.RebindActions();
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
                }
                DimSwitchTrack.PlayOneShot(DimSwitchUp);
                crosshair.SetActive(true);
                break;
            case Dimension.Two:
                StartCoroutine(DoCameraTransition());
                if (!_isUsingController)
                {
                    CameraManager.main.Angle = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;
                    Cursor.lockState = CursorLockMode.Confined;
                    PlayerManager.main.targetCursorMode = CursorLockMode.Confined;
                }
                DimSwitchTrack.PlayOneShot(DimSwitchDown);
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
                inputRotation = PlayerInputs.main.ControllerLookInput * (controllerRotateSpeed * Time.deltaTime);
            }
            else
            {
                inputRotation = PlayerInputs.main.LookInput * (rotateSpeed * Time.deltaTime);
            }

            // Apply invert settings BEFORE updating pitch/yaw
            if (invertX) inputRotation.x = -inputRotation.x;
            if (invertY) inputRotation.y = -inputRotation.y;

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
                var inp = PlayerInputs.main.ControllerLookInput;
                if (inp == Vector3.zero) return;
                angle = Mathf.Atan2(inp.y, inp.x);
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
        var minp = PlayerInputs.main.MoveInput;
        var moveVector = new Vector3(minp.x, 0, minp.y);
        var moveDir = moveVector.z * MoveForward + moveVector.x * Right + moveVector.y * MoveUp;
        var f = Vector3.ProjectOnPlane(moveDir, transform.up);
        var r = Vector3.Cross(velocity + transform.forward * 5, transform.up).normalized;
        var u = Vector3.Cross(velocity + transform.forward * 5, transform.right).normalized;
        RelativeMovement = new Vector3(Vector3.Dot(f, r), Vector3.Dot(f, u), moveDir.z);
        Vector3 impulse = Vector3.zero;
        if (moveDir != Vector3.zero)
        {
            bool sprint = PlayerInputs.main.Sprint && _boost > 1;
            float speedMultipliers = accelDotScaling.Evaluate(Vector3.Dot(moveDir, velocity)) *
                                     accelSpeedScaling.Evaluate(velocity.magnitude) *
                                     (sprint ? boostMultiplier : 1) *
                                     (Dim3 ? forwardAlignmentScaling.Evaluate(moveVector.normalized.z) : 1);
            if (sprint)
            {
                _boost -= Time.fixedDeltaTime * 10;
                _boostTime = Time.time;
                _boostUse += Time.fixedDeltaTime * 10;
            }
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
        if (!PlayerInputs.main.Dash || _dashCd > 0 || _boost < 30) return Vector3.zero;
        _boost -= 30;
        _boostTime = Time.time;
        _boostUse += 30;
        var input = PlayerInputs.main.MoveInput;
        _dashDir = MoveForward * input.y + Right * input.x;
        
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
        _dashCd = PlayerManager.main.DashCd;
        CameraManager.main.isDashing = true;
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
        CameraManager.main.isDashing = false;
        CameraManager.main.extraRoll = 0;
        _isDashing = false;
    }
}



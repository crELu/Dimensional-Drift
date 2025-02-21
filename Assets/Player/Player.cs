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
using UnityEngine.Serialization;
using Collider = UnityEngine.Collider;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager main;
    [Header("Movement Settings")]
    public float baseAccel;
    public float moveSpeed, rotateSpeed, controllerRotateSpeed;
    public AnimationCurve accelSpeedScaling;
    public AnimationCurve accelDotScaling;
    public float baseDrag = 5f;
    public AnimationCurve dragSpeedScaling;
    [Header("Camera Settings")]
    public Transform cameraFixture;
    public Quaternion normalCameraRot, orthoCameraRot;
    public Vector3 normalCameraPos, orthoCameraPos;
    public float fov;
    public float orthographicSize;
    public float dimSwitchDuration;

    private CameraProjectionBlender _cameraBlender;

    private float       _aspect;
    
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _controllerLookAction;
    private InputAction _dashAction;
    private InputAction _flyUpAction;
    private InputAction _flyDownAction;
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
    [field:SerializeField] public float Ammo { get; private set; }
    public static bool fire;
    public static List<Attack> Bullets => main.currentWeapon.Bullets;

    private Animator _anim;
    [HideInInspector] public float3 position;
    private bool Dim3 => DimensionManager.currentDim == Dimension.Three;
    private Camera _camera;
    public Vector3 Right => Dim3 ? transform.right : _camera.transform.right;
    public Vector3 MoveForward => Dim3 ? transform.forward : _camera.transform.up;
    
    public Vector3 MoveUp => Dim3 ? transform.up : Vector3.zero;
    private Vector2 MoveInput => _moveAction.ReadValue<Vector2>();
    private float FlyInput => _flyUpAction.ReadValue<float>() - _flyDownAction.ReadValue<float>();
    public Quaternion LookRotation => Dim3 ? Camera.main.transform.rotation : transform.rotation;
    private Vector3 LookInput
    {
        get
        {
            var moveVector = _controllerLookAction.ReadValue<Vector2>() * controllerRotateSpeed;
            if (moveVector == Vector2.zero)
            {
                moveVector = _lookAction.ReadValue<Vector2>();
            }
            return moveVector;
        }
    }
    
    void Start()
    {
        _moveAction = InputSystem.actions.FindAction("Move");
        _lookAction = InputSystem.actions.FindAction("Look");
        _controllerLookAction = InputSystem.actions.FindAction("Controller Look");
        _dashAction = InputSystem.actions.FindAction("Dash");
        _flyUpAction = InputSystem.actions.FindAction("Fly Up");
        _flyDownAction = InputSystem.actions.FindAction("Fly Down");
        _fireAction = InputSystem.actions.FindAction("Fire 1");
        _anim = GetComponent<Animator>();
        _camera = Camera.main;
        main = this;
        DimensionManager.dimSwitch.AddListener(SwitchDims);
        SwitchDims();
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        _camera.transform.localPosition = normalCameraPos;
        
        
        _cameraBlender = _camera.gameObject.AddComponent<CameraProjectionBlender>();
        _cameraBlender.fieldOfView = fov;
        _cameraBlender.orthographicSize = orthographicSize;
    }

    void Update()
    {
        transform.position = position;
        DoAttack();
        DoRotation();
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

    private void SwitchDims()
    {
        Debug.Log(DimensionManager.currentDim);
        // _camera.transform.localPosition = orthoCameraPos;
        // float angle = Mathf.Atan2(transform.forward.z, transform.forward.x) * Mathf.Rad2Deg;
        // _camera.transform.localRotation = Quaternion.Euler(0, 0, angle - 90f);
        // Camera.main.orthographic = true;
        switch (DimensionManager.currentDim)
        {
            case Dimension.Three:
                // _camera.transform.localPosition = normalCameraPos;
                // _camera.transform.localRotation = normalCameraRot;
                // Camera.main.orthographic = false;
                StartCoroutine(DoCameraTransition(Dimension.Three));
                Cursor.lockState = CursorLockMode.Locked;                       
                break;
            case Dimension.Two:
                StartCoroutine(DoCameraTransition(Dimension.Two));
                Cursor.lockState = CursorLockMode.Confined;
                break;
            case Dimension.One:
                break;
        }
    }
    
    // IEnumerator DoCameraTransition(Dimension newDimension)
    // {
    //     float timer = 0f;
    //     Vector3 startPosition = _camera.transform.localPosition;
    //     Quaternion startRotation = _camera.transform.localRotation;
    //
    //     Vector3 targetPosition = newDimension == Dimension.Two ? orthoCameraPos : normalCameraPos;
    //     bool targetIsOrthographic = newDimension == Dimension.Two;
    //
    //     // // Set the target blend value based on the dimension
    //     // if (targetIsOrthographic)
    //     // {
    //     //     _cameraBlender.Orthographic();
    //     // }
    //     // else
    //     // {
    //     //     _cameraBlender.Perspective();
    //     // }
    //
    //     while (timer < 1f)
    //     {
    //         timer += Time.deltaTime / dimSwitchDuration;
    //         float smoothStep = Mathf.SmoothStep(0, 1, timer);
    //
    //         // Manually update the CameraProjectionBlender
    //         // _cameraBlender.ManualUpdate(Time.deltaTime);
    //
    //         // Interpolate position
    //         _camera.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, smoothStep);
    //
    //         // Ensure the camera looks at the player
    //         Vector3 directionToPlayer = transform.position - _camera.transform.position;
    //         Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
    //         _camera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothStep);
    //
    //         yield return null;
    //     }
    //
    //     // Finalize the transition
    //     _camera.orthographic = targetIsOrthographic;
    //     if (_camera.orthographic)
    //     {
    //         _camera.transform.localRotation = Quaternion.identity;
    //     }
    // }    
    IEnumerator DoCameraTransition(Dimension newDimension)
    {
        float timer = 0f;
        Vector3 startPosition = _camera.transform.localPosition;
        float startRotation = cameraFixture.localRotation.eulerAngles.x;
        // Matrix4x4 startProjectionMatrix = _camera.projectionMatrix;
        float startFOV = _camera.fieldOfView;
        float startOrthoSize = _camera.orthographicSize;
        // cameraFixture.rotation = orthoCameraRot;
    
        // float angle = Mathf.Atan2(transform.forward.z, transform.forward.x) * Mathf.Rad2Deg;
    
        float targetFixtureRotation = newDimension == Dimension.Two
            ? orthoCameraRot.eulerAngles.x
            : normalCameraRot.eulerAngles.x;
        Vector3 targetPosition = newDimension == Dimension.Two ? orthoCameraPos : normalCameraPos;
        // Matrix4x4 targetProjectionMatrix = newDimension == Dimension.Two ? _ortho : _perspective;
        // Quaternion targetRotation = newDimension == Dimension.Two ? orthoCameraRot : normalCameraRot;
        // float targetFOV = newDimension == Dimension.Two ? 1 : fov;
        // float targetOrthoSize = newDimension == Dimension.Two ? orthographicSize : orthographicSize / 4;
        if (startPosition == targetPosition)
        {
            yield break;
        }

        if (newDimension == Dimension.Three)
        {
            _camera.orthographic = false;
        }
        while (timer < 1f)
        {
            timer += Time.deltaTime / dimSwitchDuration;
            float smoothStep = Mathf.SmoothStep(0, 1, timer);
        
            _camera.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, smoothStep);
            cameraFixture.localRotation = Quaternion.Euler(new Vector3(Mathf.Lerp(startRotation, targetFixtureRotation, smoothStep), 0, 0));
            Vector3 directionToPlayer = transform.position - _camera.transform.position;
            // Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
            // Quaternion upwardRotation = CalculateUpwardRotation(lookRotation);
            // Quaternion targetRotation = lookRotation * upwardRotation;
            // _camera.projectionMatrix = MatrixLerp(startProjectionMatrix, targetProjectionMatrix, smoothStep);
            // _camera.transform.rotation = targetRotation;
            // _camera.transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, smoothStep);
            // _camera.orthographicSize = Mathf.Lerp(startOrthoSize, targetOrthoSize, smoothStep);
            // _camera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, smoothStep);
        
            yield return null;
        }
        
        cameraFixture.localRotation = Quaternion.Euler(new Vector3(targetFixtureRotation, 0, 0));
        if (newDimension == Dimension.Two)
        {
            _camera.orthographic = true;
        }

        // if (newDimension == Dimension.Two)
        // {
        //     _camera.transform.localRotation = Quaternion.identity;
        // }
        // else
        // {
        //     _camera.transform.localRotation = normalCameraRot;
        // }
    }
    
    public Matrix4x4 MatrixLerp(Matrix4x4 from, Matrix4x4 to, float time)
    {
        Matrix4x4 ret = new Matrix4x4();
        for (int i = 0; i < 16; i++)
            ret[i] = Mathf.Lerp(from[i], to[i], time);
        return ret;
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
            cameraPitch = math.clamp(cameraPitch, -75f, 70f);
            cameraFixture.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
        }
        else
        {
            // transform.rotation = Quaternion.Euler(0, transform.rotation.y, 0);
            // Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            // Vector3 mouseScreenPosition = Input.mousePosition; 
            // Vector3 mouseDirection = mouseScreenPosition - screenCenter;
            //
            // float angle = Mathf.Atan2(mouseDirection.y, mouseDirection.x);
            // float angleDegrees = -angle * Mathf.Rad2Deg + 90f - _camera.transform.localEulerAngles.z;
            // Quaternion targetRotation = Quaternion.Euler(0f, angleDegrees, 0f);
            //
            // transform.rotation = targetRotation;
            // cameraFixture.rotation = Quaternion.Euler(cameraFixture.rotation.eulerAngles.x, -angleDegrees, cameraFixture.rotation.eulerAngles.x);
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
            yield return null;
        }
        _isDashing = false;
    }
}
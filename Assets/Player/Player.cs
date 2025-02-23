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
    [HideInInspector] 
    public float3 position;
    public float3 velocity;
    private bool Dim3 => DimensionManager.CurrentDim == Dimension.Three;
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
        DimensionManager.DimSwitch.AddListener(SwitchDims);
        SwitchDims();
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        _camera.transform.localPosition = normalCameraPos;
        
        
        // _cameraBlender = _camera.gameObject.AddComponent<CameraProjectionBlender>();
        // _cameraBlender.fieldOfView = fov;
        // _cameraBlender.orthographicSize = orthographicSize;
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
        Debug.Log(DimensionManager.CurrentDim);
        // _camera.transform.localPosition = orthoCameraPos;
        // float angle = Mathf.Atan2(transform.forward.z, transform.forward.x) * Mathf.Rad2Deg;
        // _camera.transform.localRotation = Quaternion.Euler(0, 0, angle - 90f);
        // Camera.main.orthographic = true;
        switch (DimensionManager.CurrentDim)
        {
            case Dimension.Three:
                // _camera.transform.localPosition = normalCameraPos;
                // _camera.transform.localRotation = normalCameraRot;
                // Camera.main.orthographic = false;
                StartCoroutine(DoCameraTransition());
                Cursor.lockState = CursorLockMode.Locked;                       
                break;
            case Dimension.Two:
                StartCoroutine(DoCameraTransition());
                Cursor.lockState = CursorLockMode.Confined;
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

        DimensionManager.CanSwitch = false;
        if (Dim3)
        {
            _camera.orthographic = false;
        }        
        float timer = 0f;

        Vector3 orthoSimulatedPosition = new Vector3(0, 0,
            -DistanceFromFieldOfViewAndSize(orthographicSize, 1));
        float perspectiveSize = SizeFromDistanceAndFieldOfView(
            normalCameraPos.magnitude, fov);
        
        Vector3 startPosition = Dim3 ? orthoSimulatedPosition : normalCameraPos;
        float startFixtureRotation = cameraFixture.rotation.eulerAngles.x;
        if (startFixtureRotation > 180f)
            startFixtureRotation -= 360f;
        float startSize = Dim3 ? _camera.orthographicSize * 4 : perspectiveSize;

        Quaternion targetCameraRotation = Dim3 ? normalCameraRot : orthoCameraRot;
        float targetFixtureRotation = !Dim3
            ? orthoCameraRot.eulerAngles.x
            : normalCameraRot.eulerAngles.x;
        Vector3 targetPosition =
            !Dim3 ? orthoSimulatedPosition : normalCameraPos;
        float targetSize =
            !Dim3 ? _camera.orthographicSize * 4 : perspectiveSize;

        Quaternion nearEndRotation = Quaternion.identity;
        float nearEndTransitionTime = 0.7f;
        while (timer < 1f)
        {
            timer += Time.deltaTime / dimSwitchDuration;
            float smoothStep = Mathf.SmoothStep(0, 1, timer);
            
            float currentSize = Mathf.Lerp(startSize, targetSize, smoothStep);
            _camera.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, smoothStep);
            cameraFixture.rotation = Quaternion.Euler(
                    Mathf.Lerp(startFixtureRotation, targetFixtureRotation,
                        smoothStep), cameraFixture.eulerAngles.y,
                    cameraFixture.eulerAngles.z);
            
            Vector3 directionToPlayer = transform.position - _camera.transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
            _camera.transform.rotation = targetRotation;

            _camera.fieldOfView = Mathf.Clamp(FieldOfViewFromSizeAndDistance(currentSize, -_camera.transform.localPosition.z), 1, fov);
            if (Dim3 && timer > nearEndTransitionTime)
            {
                if (nearEndRotation == Quaternion.identity)
                {
                    nearEndRotation = _camera.transform.localRotation;
                }
                _camera.transform.localRotation = Quaternion.Slerp(nearEndRotation, targetCameraRotation, (smoothStep - nearEndTransitionTime) / (1 - nearEndTransitionTime));
            }
            yield return null;
        }
        // Set all values to their target value
        cameraFixture.rotation = Quaternion.Euler(new Vector3(
            targetFixtureRotation, cameraFixture.eulerAngles.y,
            cameraFixture.eulerAngles.z));
        _camera.transform.localPosition = targetPosition;
        cameraFixture.rotation = Quaternion.Euler(targetFixtureRotation,
            cameraFixture.eulerAngles.y, cameraFixture.eulerAngles.z);
        _camera.transform.localRotation = targetCameraRotation;
        
        if (!Dim3)
        {
            _camera.transform.localRotation = Quaternion.identity;
            _camera.orthographic = true;
            _camera.transform.localPosition = orthoCameraPos;
        }

        DimensionManager.CanSwitch = true;
    }

    /// <summary>
    /// Calculates a distance based on a size and field of view.
    /// </summary>
    /// <param name="size">The size of the camera.</param>
    /// <param name="fov">The camera's field of view.</param>
    /// <returns>The distance away the camera should be.</returns>
    private static float DistanceFromFieldOfViewAndSize(float size, float fov)
        => size / (2.0f * Mathf.Tan(0.5f * Mathf.Deg2Rad * fov));

    /// <summary>
    /// Gets a camera size value based on a distance and field of view.
    /// </summary>
    /// <param name="distance">The distance away the camera is.</param>
    /// <param name="fov">The camera's field of view.</param>
    /// <returns>The size value.</returns>
    private static float SizeFromDistanceAndFieldOfView(float distance, float fov)
        => 2.0f * distance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);

    /// <summary>
    /// Calculates the field of view needed to get a given frustum size at a given distance.
    /// </summary>
    /// <param name="size">The size of the camera.</param>
    /// <param name="distance">The distance away the camera is.</param>
    /// <returns>The field of view.</returns>
    private static float FieldOfViewFromSizeAndDistance(float size, float distance)
        => 2.0f * Mathf.Atan(size * 0.5f / distance) * Mathf.Rad2Deg;
   
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
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            Vector3 mouseScreenPosition = Input.mousePosition; 
            Vector3 mouseDirection = mouseScreenPosition - screenCenter;
            
            float angle = Mathf.Atan2(mouseDirection.y, mouseDirection.x);
            float angleDegrees = -angle * Mathf.Rad2Deg + 90f + cameraFixture.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(0f, angleDegrees, 0f);
            Quaternion fixtureRotation = cameraFixture.rotation;
            transform.rotation = targetRotation;
            cameraFixture.rotation = fixtureRotation;
        }
    }
    
    public Vector3 GetMovement(Vector3 velocity)
    {
        Vector3 inputVector = MoveInput.y * MoveForward + MoveInput.x * Right + FlyInput * MoveUp;
        if (Dim3)
        {
            inputVector += FlyInput * MoveUp;
        }
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
        StartCoroutine(Dash());

        
        
        return _dashDir * _dashSpeed;
    }
    
    
    private IEnumerator Dash()
    {
        _isDashing = true;
        _dashCd = dashCooldown;
        float timer = 0;
        while (timer < dashDur)
        {
            timer += Time.deltaTime;
            _dashSpeed = dashSpeed * dashCurve.Evaluate(timer / dashDur);
            yield return null;
        }
        _isDashing = false;
    }
}
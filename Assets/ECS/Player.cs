using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using Collider = UnityEngine.Collider;


public class PlayerAuthoring : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseAccel;
    public float gravity, moveSpeed, jumpForce;
    public AnimationCurve accelSpeedScaling;
    public AnimationCurve accelDotScaling;
    public float airSpeedModifer = .5f;
    public float baseGroundDrag = 5f;
    public AnimationCurve groundDragSpeedScaling;
    public float baseAirDrag = 1f;
    public AnimationCurve airDragSpeedScaling;
    private int groundContacts;
    [Header("Camera Settings")]
    public Transform cameraFixture;
    public Vector2 cameraSpeed, camRange;
    
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    
    private Rigidbody _rb;
    private Vector3 velocity;
    private Animator _anim;
    public bool inAir;
    
    private bool IsGrounded => groundContacts > 0;
    private Vector3 MoveInput
    {
        get { var d = moveAction.ReadValue<Vector2>();
            return Quaternion.Euler(0, _look.x, 0) * new Vector3(d.x, 0, d.y).normalized; }
    }
    
    private Vector2 LookInput => lookAction.ReadValue<Vector2>();
    private Vector2 _look;
    
    public Vector3 Velocity => _rb.linearVelocity;

    public Vector3 movement;

    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        jumpAction = InputSystem.actions.FindAction("Jump");
        _rb = GetComponent<Rigidbody>();
        _anim = GetComponent<Animator>();
    }

    void Update()
    {
        HandleMovementUpdate();
        _look += Vector2.Scale(LookInput, cameraSpeed) * Time.deltaTime;
        _look.y = Mathf.Clamp(_look.y, camRange.x, camRange.y);
        cameraFixture.rotation = Quaternion.Euler(-_look.y, _look.x, 0);
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovementUpdate()
    {
        if (jumpAction.IsPressed() && IsGrounded)
        {
            _rb.AddForce(jumpForce * _rb.mass * Vector3.up, ForceMode.Impulse);
        }
    }
    private void HandleMovement()
    {
        Vector3 d = MoveInput;
        
        Vector3 sidewaysVelocity = Vector3.ProjectOnPlane(Velocity, Vector3.up);
        
        if (d != Vector3.zero)
        {
            float speedMultipliers = (IsGrounded ? 1 : airSpeedModifer) *
                                     accelDotScaling.Evaluate(Vector3.Dot(d, sidewaysVelocity)) *
                                     accelSpeedScaling.Evaluate(sidewaysVelocity.magnitude/moveSpeed);
            d = baseAccel * speedMultipliers * d;
        }
        
        d += -sidewaysVelocity * (IsGrounded ? 
            baseGroundDrag * groundDragSpeedScaling.Evaluate(sidewaysVelocity.magnitude):
            baseAirDrag * airDragSpeedScaling.Evaluate(sidewaysVelocity.magnitude));
        
        d += gravity * Vector3.down;
        _rb.AddForce(d, ForceMode.Acceleration);

        groundContacts = 0;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        groundContacts++;
    }
    private void OnTriggerStay(Collider other)
    {
        groundContacts++;
    }
}

class PlayerBaker : Baker<PlayerAuthoring>
{
    public override void Bake(PlayerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Player
        {
        });
    }
}

public struct Player : IComponentData
{
}

[BurstCompile]
public partial struct PlayerSystem : ISystem
{
    public void OnCreate(ref SystemState state) { }

    public void OnDestroy(ref SystemState state) { }
    
    [BurstCompile]
    public partial struct PlayerJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        public float DeltaTime;
    
        private void Execute([ChunkIndexInQuery] int chunkIndex, Entity e, LocalTransform t, EnemyHealth b, GunEnemyData g, PhysicsVelocity p)
        {
            if (b.Health < 1)
            {
                Ecb.DestroyEntity(chunkIndex, e);
            }
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        new PlayerJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            Ecb = ecb
        }.ScheduleParallel();
    }

    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}


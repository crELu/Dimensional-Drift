using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace Enemies.AI
{
    public static class EnemyPhysicsManager
    {
        private static Vector3 GetImpulse(Vector3 targetPosition, 
            LocalTransform transform, PhysicsVelocity physicsVelocity, 
            EnemyPhysicsConsts moveConsts, float deltaTime)
        {
            Vector3 direction = 
                (targetPosition - (Vector3)transform.Position).normalized;
            Vector3 impulse = moveConsts.Acceleration
                              * moveConsts.AccelerationSpeedScaling.Evaluate(
                                  Vector3.Magnitude(
                                      physicsVelocity.Linear) / moveConsts.MaxSpeed)
                              * direction;
            impulse -= moveConsts.Drag * (Vector3)physicsVelocity.Linear;
            return impulse * deltaTime;
        }

        public static void PerformEnemyMove(LocalTransform transform, 
            Vector3 targetPosition, ref PhysicsVelocity physicsVelocity,
            ref PhysicsMass physicsMass, EnemyPhysicsConsts moveConsts, 
            float deltaTime)
        {
            float distance = Vector3.Distance(
                transform.Position, targetPosition);
            if (distance > Mathf.Epsilon)
            {
                Vector3 impulse = GetImpulse(targetPosition, transform, 
                    physicsVelocity, moveConsts, deltaTime);
                physicsVelocity.ApplyImpulse(physicsMass, transform.Position, 
                    transform.Rotation, impulse, transform.Position);
            }
        }
    }
}

public struct EnemyPhysicsConsts
{
    public float Acceleration, Drag, MaxSpeed;
    public AnimationCurve AccelerationSpeedScaling;
}
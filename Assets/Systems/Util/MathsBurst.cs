using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

public class MathsBurst
{
    public static quaternion GetRandomRotationWithinCone(ref Random r, float y, float p)
    {
        float randomYaw =  r.NextFloat(-y, y);
        float randomPitch = r.NextFloat(-p, p);

        quaternion yawRotation = quaternion.AxisAngle(new float3(0,1,0), randomYaw);
        quaternion pitchRotation = quaternion.AxisAngle(new float3(1,0,0), randomPitch);

        return math.mul(yawRotation, pitchRotation);
    }
    
    public static float CompoundPercentage(float a, float b)
    {
        return (1 + a / 100f) * (1 + b / 100f) - 1;
    }
    
    public static Vector3 ApplyRot(Quaternion r, Vector3 v)
    {
        return Quaternion.LookRotation(v) * r * Vector3.forward;
    }
    public static Vector3 ApplyRot(Vector3 r, Vector3 v)
    {
        return Quaternion.LookRotation(v) * Quaternion.Euler(r) * Vector3.forward;
    }
    public static Vector3 ApplyRot(float x, float y, float z, Vector3 v)
    {
        return Quaternion.LookRotation(v) * Quaternion.Euler(x, y, z) * Vector3.forward;
    }

    public static List<int> GenerateRandomIndices(Random r, int max)
    {
        List<int> indices = Enumerable.Range(0, max).ToList();
        
        return indices.OrderBy(x => r.NextFloat()).ToList();
    }
    
    public static float NormalizeAngle(float angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }
    
    public static Quaternion Rotate(Transform current, Vector3 target, Vector3 localAxis, Vector3 localForward, float rotationSpeed, float min = -180, float max = 180)
    {
        localAxis.Normalize();
        localForward.Normalize();
        Vector3 dirToTarget = target - current.position;

        Vector3 dirToTargetLocal = current.InverseTransformDirection(dirToTarget);
        
        Vector3 projectedDirection = Vector3.ProjectOnPlane(dirToTargetLocal, localAxis).normalized;

        Vector3 forward = Vector3.ProjectOnPlane(localForward, localAxis).normalized;

        float currentAngle = Vector3.SignedAngle(forward, current.localRotation * forward, localAxis);
        float angleDelta = Vector3.SignedAngle(forward, projectedDirection, localAxis);

        float newAngle = Mathf.MoveTowardsAngle(currentAngle, currentAngle + angleDelta, rotationSpeed * Time.deltaTime);
        newAngle = Mathf.Clamp(newAngle, min, max);
        Quaternion rotationDelta = Quaternion.AngleAxis(newAngle - currentAngle, localAxis);
        return rotationDelta;
    }
    
    public static float? CalculateOptimalPitch(float velocity, Vector3 startPosition, Vector3 targetPosition, float gravity)
    {
        Vector3 displacement = targetPosition - startPosition;
        float horizontalDistance = new Vector3(displacement.x, 0, displacement.z).magnitude;
        float verticalDistance = displacement.y;

        if (gravity <= 0 || velocity <= 0)
        {
            Debug.LogError("Invalid gravity or velocity values.");
            return null;
        }

        float vSquared = velocity * velocity;
        float underRoot = vSquared * vSquared - gravity * (gravity * horizontalDistance * horizontalDistance + 2 * verticalDistance * vSquared);

        if (underRoot < 0)
        {
            return null;
        }

        float root = Mathf.Sqrt(underRoot);
        float angle1 = Mathf.Atan((vSquared + root) / (gravity * horizontalDistance));
        float angle2 = Mathf.Atan((vSquared - root) / (gravity * horizontalDistance));

        float angle1Degrees = Mathf.Rad2Deg * angle1;
        float angle2Degrees = Mathf.Rad2Deg * angle2;

        if (angle1Degrees >= 0 && angle2Degrees >= 0)
        {
            return Mathf.Min(angle1Degrees, angle2Degrees);
        }
        else if (angle1Degrees >= 0)
        {
            return angle1Degrees;
        }
        else if (angle2Degrees >= 0)
        {
            return angle2Degrees;
        }
        else
        {
            return null; // No valid angle
        }
    }
    
    public static Vector3 QuaternionToTorque(Quaternion current, Quaternion target, float gain)
    {
        Quaternion relativeRotation = target * Quaternion.Inverse(current);

        // Ensure the shortest path is taken
        if (relativeRotation.w < 0)
        {
            relativeRotation.x = -relativeRotation.x;
            relativeRotation.y = -relativeRotation.y;
            relativeRotation.z = -relativeRotation.z;
            relativeRotation.w = -relativeRotation.w;
        }

        // Convert quaternion to axis-angle representation
        relativeRotation.ToAngleAxis(out float angle, out Vector3 axis);

        // Avoid NaN issues when the angle is very small
        if (Mathf.Approximately(angle, 0f)) return Vector3.zero;

        angle = Mathf.Deg2Rad * angle;
        Vector3 angularVelocity = axis * (angle / Time.fixedDeltaTime);

        // Calculate torque using T = I * alpha (assuming uniform moment of inertia for simplicity)
        Vector3 torque = angularVelocity * gain;

        return torque;
    }
}



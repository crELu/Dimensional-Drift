﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Latios;
using UnityEngine;
using TMPro;
using Unity.Burst;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

[BurstCompile]
public class MathsBurst
{
    public static int ChooseWeightedRandom(ref Rng.RngSequence r, float[] weights, float totalWeight)
    {
        if (weights == null || weights.Length == 0)
            throw new System.ArgumentException("Weights array must not be null or empty.");

        // Pick a random value between 0 (inclusive) and totalWeight (exclusive).
        float randomValue = r.NextFloat(0f, totalWeight);

        // Iterate through the weights and return the index where the random value falls.
        float cumulativeWeight = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulativeWeight += weights[i];
            if (randomValue < cumulativeWeight)
            {
                return i;
            }
        }

        // Fallback (should not happen unless due to floating point imprecision).
        return weights.Length - 1;
    }
    
    public static quaternion GetRandomRotationWithinCone(ref Rng.RngSequence r, float y, float p)
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
    
    public static float3 DimSwitcher(float3 a, bool dim3)
    {
        return dim3 ? a : new float3(a.x, 0, a.z);
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
    
    public static float3 ClampMagnitude(float3 v, float maxMag)
    {
        if (math.lengthsq(v) > math.square(maxMag))
        {
            v = math.normalize(v) * maxMag;
        }
        return v;
    }
    
    
    
    public static quaternion RotateTowards(quaternion from, quaternion to, float maxDegreesDelta)
    {
        if (maxDegreesDelta <= 0.0f)
            return from;

        float dot = math.dot(from, to);
        quaternion target = to;

        if (dot < 0.0f)
        {
            target = new quaternion(-to.value.x, -to.value.y, -to.value.z, -to.value.w);
            dot = -dot;
        }

        dot = math.clamp(dot, -1.0f, 1.0f);
        float theta = math.acos(dot) * 2.0f * Mathf.Rad2Deg;

        if (theta < math.EPSILON)
            return target;

        float t = math.clamp(maxDegreesDelta / theta, 0, 1);
        return math.slerp(from, target, t);
    }
    
    public static float3 RotateVectorTowards(float3 current, float3 target, float maxDegreesDelta)
    {
        // Normalize the input vectors
        float3 currentNormalized = math.normalize(current);
        float3 targetNormalized = math.normalize(target);

        // Calculate the angle between the two vectors in radians
        float angle = math.acos(math.dot(currentNormalized, targetNormalized));

        // If the angle is already within the limit, return the target vector
        if (angle <= math.radians(maxDegreesDelta))
        {
            return targetNormalized * math.length(current); // Preserve the original magnitude
        }

        // Calculate the rotation axis using the cross product
        float3 axis = math.normalize(math.cross(currentNormalized, targetNormalized));

        // Create a quaternion for the rotation
        quaternion rotation = quaternion.AxisAngle(axis, math.radians(maxDegreesDelta));

        // Apply the rotation to the current vector
        float3 newDirection = math.mul(rotation, currentNormalized) * math.length(current);

        return newDirection;
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



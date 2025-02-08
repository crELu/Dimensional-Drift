using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Enemies.AI
{
    public static partial class PatternManager
    {
        public static Vector3 GetTargetPosition(MovePatternType movePatternType, LocalTransform enemyTransform)
        {
            switch (movePatternType)
            {
                case MovePatternType.Wander:
                    return GetWanderPatternTargetPosition();
                case MovePatternType.Chase:
                    return GetChasePatternTargetPosition();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum MovePatternType
    {
        Wander,
        Chase
    }
}
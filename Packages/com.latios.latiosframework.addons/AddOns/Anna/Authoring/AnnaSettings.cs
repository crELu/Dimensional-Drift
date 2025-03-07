using Latios.Authoring;
using Latios.Psyshock;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Latios.Anna
{
    public class AnnaSettings : MonoBehaviour
    {
        public Bounds worldBounds         = new Bounds(float3.zero, new float3(1f));
        public int3   subdivisionsPerAxis = new int3(2, 2, 2);
        public float3 gravity             = new float3(0f, -9.81f, 0f);
        [Range(0f, 1f)]
        public float linearDamping = 0.05f;
        [Range(0f, 1f)]
        public float angularDamping = 0.05f;
        [Min(1)]
        public int solverIterations = 4;
    }

    public class AnnaSettingsBaker : Baker<AnnaSettings>
    {
        public override void Bake(AnnaSettings authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PhysicsSettings
            {
                collisionLayerSettings = new CollisionLayerSettings
                {
                    worldAabb                = new Aabb(authoring.worldBounds.min, authoring.worldBounds.max),
                    worldSubdivisionsPerAxis = math.max(1, authoring.subdivisionsPerAxis)
                },
                gravity        = authoring.gravity,
                linearDamping  = (half)authoring.linearDamping,
                angularDamping = (half)authoring.angularDamping,
                numIterations  = (byte)authoring.solverIterations
            });
        }
    }
}


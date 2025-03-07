using Latios.Psyshock;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Latios.Anna
{
    public static class CoreExtensions
    {
        public static PhysicsSettings GetPhysicsSettings(this LatiosWorldUnmanaged latiosWorld)
        {
            if (latiosWorld.sceneBlackboardEntity.HasComponent<PhysicsSettings>())
                return latiosWorld.sceneBlackboardEntity.GetComponentData<PhysicsSettings>();
            if (latiosWorld.worldBlackboardEntity.HasComponent<PhysicsSettings>())
                return latiosWorld.worldBlackboardEntity.GetComponentData<PhysicsSettings>();
            return new PhysicsSettings
            {
                collisionLayerSettings = CollisionLayerSettings.kDefault,
                gravity                = new float3(0f, -9.81f, 0f),
                linearDamping          = (half)0.05f,
                angularDamping         = (half)0.05f,
                numIterations          = 4
            };
        }
    }
}


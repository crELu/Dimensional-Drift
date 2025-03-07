using System.Collections.Generic;
using Latios.Psyshock.Authoring;
using Latios.Transforms.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Latios.Anna.Authoring
{
    public class CollisionTagAuthoring : MonoBehaviour
    {
        public enum Mode
        {
            IncludeEnvironmentRecursively,
            IncludeKinematicRecursively,
            ExcludeRecursively,
            IncludeEnvironmentSelfOnly,
            IncludeKinematicSelfOnly,
            ExcludeSelfOnly,
        }

        public Mode mode;
    }

    [BakeDerivedTypes]
    public class CollisionTagAuthoringBaker : Baker<UnityEngine.Collider>
    {
        static List<UnityEngine.Collider> s_colliderCache = new List<UnityEngine.Collider>();
        static List<ColliderAuthoring>    s_compoundCache = new List<ColliderAuthoring>();

        [BakingType]
        struct RequestPrevious : IRequestPreviousTransform { }

        public override void Bake(UnityEngine.Collider authoring)
        {
            if (this.GetMultiColliderBakeMode(authoring, out _) == MultiColliderBakeMode.Ignore)
                return;

            var  search        = authoring.gameObject;
            bool isEnvironment = false;
            bool isKinematic   = false;
            while (search != null)
            {
                var tag = GetComponentInParent<CollisionTagAuthoring>(search);
                if (tag == null)
                    break;

                if (tag.mode == CollisionTagAuthoring.Mode.IncludeEnvironmentSelfOnly)
                {
                    if (search == authoring.gameObject)
                    {
                        isEnvironment = true;
                        break;
                    }
                }
                else if (tag.mode == CollisionTagAuthoring.Mode.IncludeKinematicSelfOnly)
                {
                    if (search == authoring.gameObject)
                    {
                        isKinematic = true;
                        break;
                    }
                }
                else if (tag.mode == CollisionTagAuthoring.Mode.ExcludeSelfOnly)
                {
                    if (search != authoring.gameObject)
                    {
                        break;
                    }
                }
                else if (tag.mode == CollisionTagAuthoring.Mode.IncludeEnvironmentRecursively)
                {
                    isEnvironment = true;
                    break;
                }
                else if (tag.mode == CollisionTagAuthoring.Mode.IncludeKinematicRecursively)
                {
                    isKinematic = true;
                    break;
                }

                search = GetParent(search);
            }

            if (isEnvironment)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent<EnvironmentCollisionTag>(entity);
            }
            else if (isKinematic)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<KinematicCollisionTag>(entity);
                AddComponent<RequestPrevious>(      entity);
            }
        }
    }

    public class CollisionTagAuthoringCompoundBaker : Baker<Psyshock.Authoring.ColliderAuthoring>
    {
        [BakingType]
        struct RequestPrevious : IRequestPreviousTransform { }

        public override void Bake(ColliderAuthoring authoring)
        {
            var  search        = authoring.gameObject;
            bool isEnvironment = false;
            bool isKinematic   = false;
            while (search != null)
            {
                var tag = GetComponentInParent<CollisionTagAuthoring>(search);
                if (tag == null)
                    break;

                if (tag.mode == CollisionTagAuthoring.Mode.IncludeEnvironmentSelfOnly)
                {
                    if (search == authoring.gameObject)
                    {
                        isEnvironment = true;
                        break;
                    }
                }
                else if (tag.mode == CollisionTagAuthoring.Mode.IncludeKinematicSelfOnly)
                {
                    if (search == authoring.gameObject)
                    {
                        isKinematic = true;
                        break;
                    }
                }
                else if (tag.mode == CollisionTagAuthoring.Mode.ExcludeSelfOnly)
                {
                    if (search != authoring.gameObject)
                    {
                        break;
                    }
                }
                else if (tag.mode == CollisionTagAuthoring.Mode.IncludeEnvironmentRecursively)
                {
                    isEnvironment = true;
                    break;
                }
                else if (tag.mode == CollisionTagAuthoring.Mode.IncludeKinematicRecursively)
                {
                    isKinematic = true;
                    break;
                }

                search = GetParent(search);
            }

            if (isEnvironment)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent<EnvironmentCollisionTag>(entity);
            }
            else if (isKinematic)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<KinematicCollisionTag>(entity);
                AddComponent<RequestPrevious>(      entity);
            }
        }
    }
}


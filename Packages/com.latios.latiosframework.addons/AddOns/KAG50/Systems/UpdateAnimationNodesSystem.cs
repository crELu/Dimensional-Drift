using Unity.Entities;

namespace Latios.KAG50
{
    [UpdateInGroup(typeof(KinemationGraphRootSuperSystem))]
    [UpdateBefore(typeof(AnimationBlendWeightsSystem))]
    public partial class UpdateAnimationNodesSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var dependency = Dependency;

            dependency = new UpdateAnimationStateMachineNodeJob
            {
            }.ScheduleParallel(dependency);

            Dependency = dependency;
        }
    }
}


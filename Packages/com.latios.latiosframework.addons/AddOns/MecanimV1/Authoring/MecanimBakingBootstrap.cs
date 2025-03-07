using Latios.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Latios.Mecanim.Authoring
{
    public static class MecanimBakingBootstrap
    {
        /// <summary>
        /// Adds Mecanim bakers and baking systems into baking world
        /// </summary>
        public static void InstallMecanimAddon(ref CustomBakingBootstrapContext context)
        {
#if UNITY_EDITOR
            context.filteredBakerTypes.Add(typeof(MecanimSmartBaker));
            context.bakingSystemTypesToInject.Add(TypeManager.GetSystemTypeIndex<Systems.MecanimAnimatorControllerSmartBlobberSystem>());
#endif
        }
    }
}


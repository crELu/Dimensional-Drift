using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Latios.Cyline
{
    public static class CylineBootstrap
    {
        /// <summary>
        /// Install Cyline systems into the World. Typically recommend to install both in the editor and runtime worlds.
        /// </summary>
        /// <param name="world">The world to install Cyline into.</param>
        public static void InstallCyline(World world)
        {
            BootstrapTools.InjectSystem(TypeManager.GetSystemTypeIndex<Systems.BuildLineRenderer3DMeshSystem>(), world);
        }
    }
}


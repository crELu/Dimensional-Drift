using Unity.Entities;

namespace Enemies.AI
{
    public class IntelAuthoring : BaseAuthor
    {
        public float value;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new Intel{BaseValue = value});
        }
    }
    
    public struct Intel : IComponentData
    {
        public float BaseValue;
    }
}
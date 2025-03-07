using Latios.KAG50.Asset;
using Unity.Entities;

namespace Latios.KAG50
{
    public struct StateMachineStateRef
    {
        public AnimationBufferId m_StateId;
        public byte              m_StateIndex;
        public bool IsValid => m_StateId.IsValid;

        public static StateMachineStateRef Null => new() { m_StateId = AnimationBufferId.Null };
    }

    public struct AnimationStateMachineComponent : IComponentData
    {
        public BlobAssetReference<AnimationGraphBlob> m_AnimationGraphBlob;
        public byte                                   m_StateMachineIndex;
        public float                                  m_Weight;

        public StateMachineStateRef m_CurrentState;
#if UNITY_EDITOR || DEBUG
        public StateMachineStateRef m_PreviousState;
#endif

        public ref StateMachineBlob GetStateMachineBlob()
        {
            return ref m_AnimationGraphBlob.Value.StateMachines[m_StateMachineIndex];
        }

        public ref AnimationStateBlob GetCurrentStateBlob()
        {
            return ref GetStateMachineBlob().States[m_CurrentState.m_StateIndex];
        }
    }
}


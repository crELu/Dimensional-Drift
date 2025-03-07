using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GraphProcessor;
using Latios.KAG50.Asset;

namespace Latios.KAG50.Editor
{
    public class AnimationStateMachineEditor : BaseGraphWindow
    {
        AnimationToolbarView toolbarView;

        protected override void OnDestroy()
        {
            graphView?.Dispose();
        }

        protected override void InitializeWindow(BaseGraph graph)
        {
            titleContent = new GUIContent("Animation StateMachine Editor");

            Debug.Assert(graph != null);
            var animationStateMachineAsset = graph as AnimationStateMachineAsset;
            Debug.Assert(animationStateMachineAsset != null);

            if (graphView == null)
            {
                graphView = new AnimationStateMachineView(this);
                toolbarView = new AnimationToolbarView(graphView, animationStateMachineAsset.m_OwnerGraph);
                graphView.Add(toolbarView);
            }

            rootView.Add(graphView);
        }

        protected override void InitializeGraphView(BaseGraphView view)
        {
            //graphView.OpenPinned<ExposedParameterView>();
            toolbarView.UpdateButtonStatus();
        }
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GraphProcessor;
using Latios.KAG50.Asset;

namespace Latios.KAG50.Editor
{
    public class AnimationGraphEditor : BaseGraphWindow
    {
        AnimationToolbarView toolbarView;

        protected override void OnDestroy()
        {
            graphView?.Dispose();
        }

        protected override void InitializeWindow(BaseGraph graph)
        {
            titleContent = new GUIContent("Animation Graph Editor");

            if (graphView == null)
            {
                graphView = new AnimationGraphView(this);
                toolbarView = new AnimationToolbarView(graphView);
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

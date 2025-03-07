using System;
using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using Latios.KAG50.Asset;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Latios.KAG50.Editor
{
    public class AnimationStateView : BaseGraphView
    {
        // Nothing special to add for now
        public AnimationStateView(EditorWindow window) : base(window)
        {
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            BuildStackNodeContextualMenu(evt);
            base.BuildContextualMenu(evt);
        }

        /// <summary>
        /// Add the New Stack entry to the context menu
        /// </summary>
        /// <param name="evt"></param>
        protected void BuildStackNodeContextualMenu(ContextualMenuPopulateEvent evt)
        {
            Vector2 position = (evt.currentTarget as VisualElement).ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);
            evt.menu.AppendAction("New Stack", (e) => AddStackNode(new BaseStackNode(position)), DropdownMenuAction.AlwaysEnabled);
        }

        //public override void DragPerformedCallback(DragPerformEvent e)
        //{
        //	var mousePos = (e.currentTarget as VisualElement).ChangeCoordinatesTo(contentViewContainer, e.localMousePosition);
        //	var dragData = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
        //
        //	// Drag and Drop for elements inside the graph
        //	if (dragData != null)
        //	{
        //		var exposedParameterFieldViews = dragData.OfType<ExposedParameterFieldView>();
        //		if (exposedParameterFieldViews.Any())
        //		{
        //			foreach (var paramFieldView in exposedParameterFieldViews)
        //			{
        //				RegisterCompleteObjectUndo("Create Parameter Node");
        //				var paramNode = BaseNode.CreateFromType<AnimationParameterNode>(mousePos);
        //				paramNode.parameterGUID = paramFieldView.parameter.guid;
        //				paramNode.parameter = paramFieldView.parameter;
        //				AddNode(paramNode);
        //			}
        //		}
        //	}
        //}
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using GraphProcessor;
using Latios.KAG50.Asset;

namespace Latios.KAG50.Editor
{
    [NodeCustomEditor(typeof(SingleClipNode))]
    public class SingleClipView : ClipNodeView
    {
        public override void Enable()
        {
            base.Enable();
            var node = nodeTarget as SingleClipNode;

            // Create your fields using node's variables and add them to the controlsContainer
            if (node.Clip != null)
            {
                var button = new Button(() =>
                {
                    Selection.activeObject = node.Clip;
                });
                button.text = "Select Animation Clip in Project windows";
                controlsContainer.Add(button);
            }
        }
    }
}

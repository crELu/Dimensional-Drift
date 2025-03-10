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
    [NodeCustomEditor(typeof(EntryStateNode))]
    public class EntryStateView : BaseNodeView
    {
        public override void Enable()
        {
            base.Enable();
            SetNodeColor(new Color(21f / 255, 110f / 255, 49f / 255));
        }
    }
}
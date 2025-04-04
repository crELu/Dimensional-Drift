using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Item : ScriptableObject
{
    public string itemTitle;
    public virtual string Description => throw new NotImplementedException();
    public float baseCost;
    public virtual Sprite Icon => throw new NotImplementedException();
    public int rarity;
    public virtual void DoAction()
    {
        
    }
}

public class SpritePreviewAttribute : PropertyAttribute { }
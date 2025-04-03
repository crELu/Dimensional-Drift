using UnityEngine;

public class Item : ScriptableObject
{
    public string itemTitle;
    [TextArea(2, 8)] public string description;
    public float baseCost;
    public Sprite icon;
    public Sprite rarity;
    public virtual void DoAction()
    {
        
    }
}


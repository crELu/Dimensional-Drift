using UnityEngine;

[CreateAssetMenu(fileName = "Item Augment", menuName = "Items/Augment")]
public class AugmentItem : Item
{
    public Augment augment;
    public AugmentType type;
    public ShopAugmentTarget target;
    [SpritePreview] public Sprite icon;
    [TextArea(2, 8)] public string textT1;
    [TextArea(2, 8)] public string textT2;
    [TextArea(2, 8)] public string textT3;
    public override string Description => rarity == 0? textT1 : rarity == 1 ? textT2 : textT3;
    public override Sprite Icon => icon;
    public override void DoAction()
    {
        switch (target)
        {
            case ShopAugmentTarget.Character:
                PlayerManager.main.weapons[0].AddAugment(augment);
                break;
            default:
                PlayerManager.main.weapons[(int)target].AddAugment(augment);
                break;
        }
    }
}

public enum ShopAugmentTarget
{
    Weapon1,
    Weapon2,
    Weapon3,
    Weapon4,
    Character
}
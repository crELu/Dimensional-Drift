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
        augment.icon = icon;
        switch (target)
        {
            case ShopAugmentTarget.Character:
                PlayerManager.main.AddAugment(augment, rarity);
                PlayerStats.main.carAug.Add(icon);
                break;
            default:
                PlayerManager.main.weapons[(int)target].AddAugment(augment, rarity);
                if (target == ShopAugmentTarget.Weapon1) PlayerStats.main.gunAug.Add(icon);
                if (target == ShopAugmentTarget.Weapon2) PlayerStats.main.lasAug.Add(icon);
                if (target == ShopAugmentTarget.Weapon3) PlayerStats.main.canAug.Add(icon);
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
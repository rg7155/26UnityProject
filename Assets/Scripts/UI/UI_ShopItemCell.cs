using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 상점 셀 프리팹 — ShopItemData 하나를 표시하고 구매 버튼을 처리. UI_ShopPanel이 항목 수만큼 생성.
// 표시 텍스트는 effect 종류에 따라 ShopService 파생값으로 구성.
public class UI_ShopItemCell : MonoBehaviour
{
    [SerializeField] Button _button;
    [SerializeField] TMP_Text _label;

    ShopItemData _item;
    Action _onPurchased;

    public void Bind(ShopItemData item, Action onPurchased)
    {
        _item = item;
        _onPurchased = onPurchased;
        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClick);
        }
        Refresh();
    }

    public void Refresh()
    {
        if (_item == null) return;
        if (_label != null)  _label.text = BuildLabel();
        if (_button != null) _button.interactable = ShopService.CanBuy(_item);
    }

    string BuildLabel()
    {
        int tier = ShopService.TierOf(_item.id);
        int max = ShopService.MaxTier(_item);
        bool maxed = ShopService.IsMaxed(_item);
        int cost = ShopService.Cost(_item);

        switch (_item.effect)
        {
            case ShopEffectType.StartHp:
            {
                int now = ShopService.HpTotal(tier, _item.valuePerTier);
                if (maxed) return $"{_item.displayName}  MAX\n{now}";
                int next = ShopService.HpTotal(tier + 1, _item.valuePerTier);
                return $"{_item.displayName}  [{tier}/{max}]\n{now} -> {next}   {cost}G";
            }
            default: // Damage
            {
                int now = ShopService.DamagePct(tier, _item.valuePerTier);
                if (maxed) return $"{_item.displayName}  MAX\n+{now}%";
                int next = ShopService.DamagePct(tier + 1, _item.valuePerTier);
                return $"{_item.displayName}  [{tier}/{max}]\n+{now}% -> +{next}%   {cost}G";
            }
        }
    }

    void OnClick()
    {
        if (ShopService.Buy(_item)) _onPurchased?.Invoke();
    }

    void OnDestroy()
    {
        if (_button != null) _button.onClick.RemoveAllListeners();
    }
}

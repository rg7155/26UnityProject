using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    protected WeaponData _data;
    protected int _damage;

    public WeaponData Data => _data;

    public virtual void Init(WeaponData data)
    {
        _data = data;
        _damage = data.damage;
    }

    public virtual void UpgradeDamage(float multiplier)
    {
        _damage = Mathf.RoundToInt(_damage * multiplier);
    }

    public virtual void UpgradeFireRate(float multiplier) { }
    public virtual void UpgradeRange(float multiplier) { }
    public virtual void UpgradeRadius(float multiplier) { }
    public virtual void UpgradeInterval(float multiplier) { }
}

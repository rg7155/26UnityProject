using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    protected WeaponData _data;
    protected int _damage;

    public virtual void Init(WeaponData data)
    {
        _data = data;
        _damage = data.damage;
    }

    public virtual void UpgradeDamage(float multiplier)
    {
        _damage = Mathf.RoundToInt(_damage * multiplier);
    }
}

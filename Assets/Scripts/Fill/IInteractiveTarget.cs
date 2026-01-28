using UnityEngine;

namespace Chromatic.Combat
{
    public interface IInteractiveTarget
    {
        void OnHit(float damage);
    }
}
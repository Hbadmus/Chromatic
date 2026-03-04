using UnityEngine;

namespace Chromatic.Combat
{
    public interface IInteractiveTarget
    {
        // 必须加上 Color bulletColor，这样合同才对得上
        void OnHit(float damage, Color bulletColor);
    }
}
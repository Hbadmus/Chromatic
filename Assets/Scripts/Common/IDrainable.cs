using UnityEngine;

public interface IDrainable
{
    bool CanDrain { get; }
    void OnDrain();
}
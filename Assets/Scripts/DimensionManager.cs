using UnityEngine;
using UnityEngine.Events;

public class DimensionManager : MonoBehaviour
{
    public UnityEvent dimSwitch;

    public static Dimension currentDim;
    public static Dimension pastDim;

    void SwitchDimension(Dimension newDim)
    {
        if (Mathf.Abs((int)newDim - (int)currentDim) > 1) return;
        pastDim = currentDim;
        currentDim = newDim;
        dimSwitch.Invoke();
    }
}

public enum Dimension
{
    Three,
    Two,
    One,
    Zero
}
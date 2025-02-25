using System;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DimensionManager : MonoBehaviour
{
    public static UnityEvent dimSwitch;

    public static Dimension currentDim => burstDim.Data;
    private class IntFieldKey {}
    public static readonly SharedStatic<Dimension> burstDim = SharedStatic<Dimension>.GetOrCreate<DimensionManager, IntFieldKey>();
    public static Dimension pastDim;
    
    private InputAction _dimUpAction;
    private InputAction _dimDownAction;
    
    private void Awake()
    {
        burstDim.Data = Dimension.Two;
        dimSwitch = new UnityEvent();
        _dimUpAction = InputSystem.actions.FindAction("Dim Up");
        _dimDownAction = InputSystem.actions.FindAction("Dim Down");
    }
    
    void Update()
    {
        _dimUpAction.performed += _ => SwitchDimension(currentDim + 1);
        _dimDownAction.performed += _ => SwitchDimension(currentDim - 1);
    }

    void SwitchDimension(Dimension newDim)
    {
        if (Mathf.Abs((int)newDim - (int)currentDim) > 1 || newDim == currentDim || (newDim < 0) || (Dimension.Two < newDim)) return;
        pastDim = currentDim;
        burstDim.Data = newDim;
        dimSwitch.Invoke();
    }
}
// Three = 0, Two = 1, etc. Done for convenience of the default being 0.
public enum Dimension
{
    Three,
    Two,
    One,
    Zero
}
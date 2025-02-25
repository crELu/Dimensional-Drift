using System;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DimensionManager : MonoBehaviour
{
    public static UnityEvent DimSwitch;

    public static Dimension CurrentDim => burstDim.Data;
    private class IntFieldKey {}    
    public static readonly SharedStatic<Dimension> burstDim = SharedStatic<Dimension>.GetOrCreate<DimensionManager, IntFieldKey>();    
    public static Dimension PastDim;
    public static bool CanSwitch;
    
    private InputAction _dimUpAction;
    private InputAction _dimDownAction;
    
    private void Awake()
    {
        burstDim.Data = Dimension.Three;
        DimSwitch = new UnityEvent();
        _dimUpAction = InputSystem.actions.FindAction("Dim Up");
        _dimDownAction = InputSystem.actions.FindAction("Dim Down");
        CanSwitch = true;
    }
    
    void Update()
    {
        _dimUpAction.performed += _ => SwitchDimension(CurrentDim + 1);
        _dimDownAction.performed += _ => SwitchDimension(CurrentDim - 1);
    }

    void SwitchDimension(Dimension newDim)
    {
        if (!CanSwitch || Mathf.Abs((int)newDim - (int)CurrentDim) > 1 || newDim == CurrentDim || (newDim < 0) || (Dimension.Two < newDim)) return;
        PastDim = CurrentDim;
        burstDim.Data = newDim;
        DimSwitch.Invoke();
    }
}

public enum Dimension
{
    Three,
    Two,
    One,
    Zero
}
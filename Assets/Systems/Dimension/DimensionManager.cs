using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DimensionManager : MonoBehaviour
{
    public static UnityEvent DimSwitch;

    public static Dimension CurrentDim;
    public static Dimension PastDim;
    public static bool CanSwitch;
    
    private InputAction _dimUpAction;
    private InputAction _dimDownAction;
    
    private void Awake()
    {
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
        if (!CanSwitch || Mathf.Abs((int)newDim - (int)CurrentDim) > 1 || newDim == CurrentDim || (newDim < 0) || (Dimension.Zero < newDim)) return;
        PastDim = CurrentDim;
        CurrentDim = newDim;
        DimSwitch.Invoke();
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
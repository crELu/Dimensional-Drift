using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DimensionManager : MonoBehaviour
{
    public static UnityEvent dimSwitch;

    public static Dimension currentDim;
    public static Dimension pastDim;
    
    private InputAction _dimUpAction;
    private InputAction _dimDownAction;
    
    private void Awake()
    {
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
        if (Mathf.Abs((int)newDim - (int)currentDim) > 1 || newDim == currentDim || (newDim < 0) || (Dimension.Zero < newDim)) return;
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
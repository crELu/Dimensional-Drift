using System;
using System.Collections;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DimensionManager : MonoBehaviour
{
    public static UnityEvent DimSwitch;
    private static DimensionManager _instance;
    public static Dimension CurrentDim => burstDim.Data;
    private class IntFieldKey {}    
    public static readonly SharedStatic<Dimension> burstDim = SharedStatic<Dimension>.GetOrCreate<DimensionManager, IntFieldKey>();    
    public static Dimension PastDim;
    public static bool CanSwitch => Mathf.Approximately(t, 1);
    public static float t;
    public static float Duration => _instance.dimSwitchDuration;
    public float dimSwitchDuration;
    
    private InputAction _dimUpAction;
    private InputAction _dimDownAction;
    
    private void Awake()
    {
        _instance = this;
        burstDim.Data = Dimension.Three;
        DimSwitch = new UnityEvent();
        _dimUpAction = InputSystem.actions.FindAction("Dim Up");
        _dimDownAction = InputSystem.actions.FindAction("Dim Down");
        t = 1;
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
        StartCoroutine(SwitchDims());
        DimSwitch.Invoke();
        
    }

    IEnumerator SwitchDims()
    {
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / dimSwitchDuration;
            yield return null;
        }
        t = 1f;
    }
}

public enum Dimension
{
    Three,
    Two,
    One,
    Zero
}
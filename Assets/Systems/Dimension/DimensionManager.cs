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
    public static bool Dim3 => CurrentDim == Dimension.Three;
    public static Dimension CurrentDim => burstDim.Data;
    private class IntFieldKey {}    
    public static readonly SharedStatic<Dimension> burstDim = SharedStatic<Dimension>.GetOrCreate<DimensionManager, IntFieldKey>();    
    public static Dimension PastDim;
    public static bool CanSwitch => Mathf.Approximately(t, 1);
    public static float t;
    public static float normT => Dim3 ? 1-t : t;
    public static float Duration => _instance.dimSwitchDuration;
    public float dimSwitchDuration;
    
    public AnimationCurve timeCurve;
    
    private InputAction _dimSwitchAction;
    
    private void Awake()
    {
        _instance = this;
        burstDim.Data = Dimension.Three;
        DimSwitch = new UnityEvent();
        _dimSwitchAction = InputSystem.actions.FindAction("Dim Switch");
        t = 1;
    }
    
    void Update()
    {
        if (_dimSwitchAction.triggered) SwitchDimension();
    }

    void SwitchDimension()
    {
        if (!CanSwitch) return;
        PastDim = CurrentDim;
        burstDim.Data = PastDim == Dimension.Three ? Dimension.Two :  Dimension.Three;
        StartCoroutine(SwitchDims());
        DimSwitch.Invoke();
        
    }

    IEnumerator SwitchDims()
    {
        t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dimSwitchDuration;
            Time.timeScale = timeCurve.Evaluate(t);
            yield return null;
        }
        t = 1f;
        Time.timeScale = 1f;
    }
}

public enum Dimension
{
    Three,
    Two,
    One,
    Zero
}
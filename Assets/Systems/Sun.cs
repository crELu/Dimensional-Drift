using UnityEngine;

public class Sun : MonoBehaviour
{
    public Quaternion normalRot, orthoRot;
    void Start()
    {
        DimensionManager.DimSwitch.AddListener(SwitchDims);
        SwitchDims();
    }

    void SwitchDims()
    {
        if (DimensionManager.CurrentDim > 0) // D < 3
        {
            transform.rotation = orthoRot;
        }
        else // D = 3
        {
            transform.rotation = normalRot;
        }
    }
}

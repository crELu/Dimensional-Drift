using UnityEngine;

public class SquishManager : MonoBehaviour
{
    public static Vector4 data;

    [Range(0,1)] public float t1;
    [Range(0,1)] public float t2;
    public float h;
    public float d;

    private void Update()
    {
        data = new Vector4(Mathf.Pow(t1, .1f), h, t2, d);
    }
}
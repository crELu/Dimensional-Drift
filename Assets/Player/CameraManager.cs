using System;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager main;
    public Transform board;
    public Transform lookTarget;
    private Vector3 _offset;
    public Vector3 offsetMult = new Vector3(0, 2, -5);
    public float smoothAccel, smoothVel, rollSpeed, rollModifier;
    private Vector3 _velocity, _accel;
    private Vector3 smoothTarget;
    public bool isDashing;
    private float _roll;
    public float RollDir => Mathf.Sign(_roll);
    [HideInInspector] public float extraRoll;
    public Vector3 orthoCameraPos, normalCameraPos;
    [field: SerializeField] public float Angle { get; set; }
    private Vector3 Up2d => Quaternion.Euler(0, Angle, 0) * Vector3.forward;
    private void Start()
    {
        main = this;
        _offset = normalCameraPos;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        _offset = Vector3.Lerp(normalCameraPos, orthoCameraPos, DimensionManager.normT);
        Vector3 targetPosition;
        if (DimensionManager.Dim3)
        {
            var v = PlayerManager.main.movement.RelativeMovement;
            var tRoll = v.x * 65;
            _roll = Mathf.Lerp(_roll, tRoll, Time.deltaTime * rollSpeed);
            board.localRotation = Quaternion.Euler(0, 0, _roll + extraRoll);
            var disp = Vector3.Scale(v, offsetMult) * (v.z+1)/2;
            targetPosition = PlayerManager.main.transform.position + PlayerManager.main.transform.TransformDirection(_offset + disp);
        }
        else
        {
            board.localRotation = Quaternion.Euler(0, 0, 0);
            targetPosition = PlayerManager.main.transform.position + PlayerManager.main.transform.TransformDirection(_offset);
        }

        var a = isDashing ? rollModifier : 1;
        smoothTarget = Vector3.Slerp(smoothTarget, targetPosition, smoothAccel * a * Time.deltaTime);
        transform.position = Vector3.Slerp(transform.position, smoothTarget, smoothVel * a * Time.deltaTime);
        var lookTarg = Vector3.Slerp(lookTarget.position, PlayerManager.Position, DimensionManager.normT);
        
        transform.LookAt(lookTarg, DimensionManager.Dim3 ? Vector3.up : Up2d);
        
    }
}

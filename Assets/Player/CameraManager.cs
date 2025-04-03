using System;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager main;
    public Transform board;
    public Transform lookTarget;
    private Vector3 _offset;
    public AnimationCurve offsetX, offsetY, offsetZ;
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
    public RectTransform center, canv;
    private void Start()
    {
        main = this;
        _offset = normalCameraPos;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update()
    {
    }

    private Vector3 pastLocal;
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
            Vector3 scale = new Vector3(offsetX.Evaluate(v.x), offsetY.Evaluate(v.y), offsetZ.Evaluate(v.z));
            var disp = Vector3.Scale(v, scale) * (v.z+1)/2;
            targetPosition = _offset + disp;
        }
        else
        {
            board.localRotation = Quaternion.Euler(0, 0, 0);
            targetPosition = _offset;
        }

        var a = isDashing ? rollModifier : 1;
        pastLocal = Vector3.Slerp(pastLocal, targetPosition, smoothAccel * a * Time.deltaTime);
        
        var targ = PlayerManager.main.transform.TransformPoint(pastLocal);
        transform.position = Vector3.Slerp(transform.position, targ, smoothVel * a * Time.deltaTime);
        
        var lookTarg = Vector3.Slerp(lookTarget.position, PlayerManager.Position, DimensionManager.normT);
        transform.LookAt(lookTarg, DimensionManager.Dim3 ? Vector3.up : Up2d);
        var screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        var playerCenter = Camera.main.WorldToScreenPoint(PlayerManager.main.transform.position);
        playerCenter.z = 1;
        
        center.position = Vector3.Lerp(screenCenter, playerCenter, DimensionManager.normT);
        if (DimensionManager.normT == 1)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.down, Up2d);
            transform.position = targetPosition;
        }
    }
}

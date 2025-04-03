
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputs: MonoBehaviour
{
    public static PlayerInputs main;
    public PlayerInput playerInput;
    private InputAction _moveAction;
    public Vector2 MoveInput => _moveAction.ReadValue<Vector2>();
    private InputAction _lookAction;
    public Vector3 LookInput => _lookAction.ReadValue<Vector2>();
    private InputAction _controllerLookAction;
    public Vector3 ControllerLookInput => _controllerLookAction.ReadValue<Vector2>();
    private InputAction _dashAction;
    public bool Dash => _dashAction.triggered;
    private InputAction _sprintAction;
    public bool Sprint => _sprintAction.ReadValue<float>() != 0;
    private InputAction _fireAction;
    public bool Fire => _fireAction.ReadValue<float>() != 0;
    private InputAction _weaponUpAction;
    public bool WeaponUp => _weaponUpAction.triggered;
    private InputAction _weaponDownAction;
    public bool WeaponDown => _weaponDownAction.triggered;
    private InputAction _scanAction;
    public bool Scan => _scanAction.triggered;
    

    private void Awake()
    {
        main = this;
        DontDestroyOnLoad(this);
        RebindActions();
    }

    public void RebindActions()
    {
        _moveAction = playerInput.currentActionMap.FindAction("Move");
        _lookAction = playerInput.currentActionMap.FindAction("Look");
        _controllerLookAction = playerInput.currentActionMap.FindAction("Controller Look");
        _dashAction = playerInput.currentActionMap.FindAction("Dash");
        _sprintAction = playerInput.currentActionMap.FindAction("Sprint");
        
        _fireAction = playerInput.currentActionMap.FindAction("Fire 1");
        _weaponUpAction = playerInput.currentActionMap.FindAction("Weapon Up");
        _weaponDownAction = playerInput.currentActionMap.FindAction("Weapon Down");
        _scanAction = playerInput.currentActionMap.FindAction("Scan");
    }
    
}

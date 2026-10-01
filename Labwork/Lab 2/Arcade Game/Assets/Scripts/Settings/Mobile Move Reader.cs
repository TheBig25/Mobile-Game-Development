using UnityEngine;
using UnityEngine.InputSystem;

public class MobileMoveReader : MonoBehaviour
{
    [SerializeField] InputActionReference moveAction;
    [SerializeField] InputActionReference shootAction;
    [SerializeField] InputActionReference boostAction;

    void OnEnable() => moveAction.action.Enable();
    void OnDisable() => moveAction.action.Disable();



    // Update is called once per frame
    void Update()
    {
        Vector2 move = moveAction.action.ReadValue<Vector2>();
        Vector2 shoot = shootAction.action.ReadValue<Vector2>();
        Vector2 boost = boostAction.action.ReadValue<Vector2>();
    }
}

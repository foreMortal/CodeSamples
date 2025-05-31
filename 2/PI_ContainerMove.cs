using UnityEngine;

public class PI_ContainerMove : MonoBehaviour
{
    [SerializeField] private RectTransform _containersWindow;
    [SerializeField] private Transform _gameField;
    [SerializeField] private LayerMask _filterMask;
    [SerializeField] private ContactFilter2D _filter;

    private IContainersManager _conManager;
    private ContainerPositionValidator _validator;
    private Collider2D[] _results = new Collider2D[1];
    private Controls _controls;
    private LetterContainerParent _grabbedContainer;
    private Vector2 _cw_leftDownPoint, _cw_rightUpPoint;
    private bool _isPointerPressed, _isContainerGrabbed, _isVerified;
    private float _grabbingTimer;
    private void OnEnable() => _controls.Enable();
    private void OnDisable() => _controls.Disable(); 

    private void Awake()
    {
        _controls = new Controls();
        _filter.SetLayerMask(_filterMask);
        _validator = GetComponent<ContainerPositionValidator>();
        _conManager = _containersWindow.GetComponentInParent<IContainersManager>();

        CalculateWindow(_containersWindow, out _cw_leftDownPoint, out _cw_rightUpPoint);
    }

    private void Update()
    {
        float pointerPress = _controls.Player.PointerPress.ReadValue<float>();
        Vector2 pointerPos = _controls.Player.PointerPosition.ReadValue<Vector2>();

        if (!_isPointerPressed && pointerPress > 0)
        {
            _isPointerPressed = true;
            _isVerified = ValidatePointPosition(pointerPos, _cw_leftDownPoint, _cw_rightUpPoint);
            
            if (Physics2D.OverlapPoint(pointerPos, _filter, _results) > 0)
            {
                _grabbedContainer = _results[0].GetComponent<LetterContainerParent>();
                _isContainerGrabbed = true;
                _isVerified = true;
            }

        }
        else if (pointerPress > 0)
        {
            if (_isVerified)
            {
                Vector2 delta = _controls.Player.PointerDelta.ReadValue<Vector2>();
                //grab container
                if (!ValidatePointPosition(pointerPos, _cw_leftDownPoint, _cw_rightUpPoint))
                {
                    if(_isContainerGrabbed)
                        _grabbedContainer.Grab(_gameField);
                        
                    _isVerified = false;
                }
                else if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))//scroll containers
                    _conManager.MoveContainers(delta.x);
            }

            //move container and show placement validation
            else if (_isContainerGrabbed)
            {
                _grabbingTimer += Time.deltaTime * 3f;
                _grabbedContainer.Move(Vector3.Lerp(_grabbedContainer.transform.position, pointerPos, _grabbingTimer));

                if (Time.frameCount % 10 == 0)
                    _validator.ValidateContainerPosition(_grabbedContainer);
            }
        }
        else
        {
            //either place in handlers or return to origin
            if (_isContainerGrabbed)
            {
                if(!_isVerified)
                    _validator.ValidatePlacement(_grabbedContainer);
                
                _isContainerGrabbed = false;
                _grabbedContainer = null;
            }
            _grabbingTimer = 0f;
            _isPointerPressed = false;
            _isVerified = false;
        }
    }

    private bool ValidatePointPosition(Vector2 pointPos, Vector2 leftDownPos, Vector2 rightUpPos)
    {
        return pointPos.x >= leftDownPos.x && pointPos.y >= leftDownPos.y &&
               pointPos.x <= rightUpPos.x && pointPos.y <= rightUpPos.y;
    }

    private void CalculateWindow(RectTransform UIobject, out Vector2 leftDownPos, out Vector2 rightUpPos)
    {
        leftDownPos = new Vector2(UIobject.localPosition.x, UIobject.localPosition.y) - UIobject.sizeDelta / 2f;
        rightUpPos = new Vector2(UIobject.localPosition.x, UIobject.localPosition.y) + UIobject.sizeDelta / 2f;

        leftDownPos = _gameField.TransformPoint(leftDownPos);
        rightUpPos = _gameField.TransformPoint(rightUpPos);
    }
}
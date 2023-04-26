using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuckSelector : MonoBehaviour
{
    [SerializeField]
    private float _selectionTransitionSpeed = 1f;

    [SerializeField]
    private float _transitionAcceleration = 0.01f;

    private float _accelerationSpeed = 0f;
   
    public bool IsTransitioning => _isTransitioning;
    
    private Vector3 _startPosition;
    private Transform _followTransform;
    
    private bool _isTransitioning;
    private bool _hasTarget = false;
    private void Awake()
    {
        _startPosition = transform.position;
    }

    public void SetAim(Vector2 pAim)
    {
        transform.up = new Vector3(pAim.x, pAim.y, 0);
    }

    public void UnselectPuck()
    {
        _hasTarget = false;
        _followTransform = null;
        _isTransitioning = true;
    }
    
    public void SetNewFollowTarget(Transform pPuckTransform)
    {
        if (pPuckTransform != null)
        {
            _hasTarget = true;
            _isTransitioning = true;
            _followTransform = pPuckTransform;   
        }
        else
            UnselectPuck();
    }

    private void Update()
    {
        if (_hasTarget)
        {
            if (_isTransitioning)
            {
                _accelerationSpeed += _transitionAcceleration;
                
                transform.position = Vector3.MoveTowards(transform.position, _followTransform.position, (_selectionTransitionSpeed + _accelerationSpeed) * Time.deltaTime);
                if ((transform.position - _followTransform.position).magnitude < 0.05f)
                {
                    _isTransitioning = false;
                }
            }
            else
            {
                _accelerationSpeed = 0;
                transform.position = _followTransform.position;
            }
        }
        else
        {
            if (_isTransitioning)
            {
                _accelerationSpeed += _transitionAcceleration;
                
                transform.position = Vector3.MoveTowards(transform.position, _startPosition, _selectionTransitionSpeed * Time.deltaTime);
            }
            else
            {
                _accelerationSpeed = 0;
                
                transform.position = _startPosition;
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimVisual : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField] 
    private GameObject uiStick;
    
    [SerializeField]
    private float movementRange = 50;

    [SerializeField] 
    private float returnAnimationTime = 0.015f;
    
    private Vector2 anchorPoint;
    private Vector2 returnFrom;

    private float returnTime;

    private bool isReturning = false;
    public float fade
    {
        get => canvasGroup.alpha;
        set => canvasGroup.alpha = value;
    }

    public void SetAnchorPoint(Vector2 pAnchorPoint)
    {
        if (isReturning) return;
        
        anchorPoint = pAnchorPoint;
    }

    public void UpdateAim(Vector2 aimPoint)
    {
        if (isReturning) return;

        var delta = Vector2.ClampMagnitude((aimPoint - anchorPoint),movementRange);
        uiStick.transform.position = anchorPoint + delta;
    }

    public void ReturnAnimation(Vector2 pReturnFrom)
    {
        if (isReturning) return;
        
        returnFrom = uiStick.transform.position;
        isReturning = true;
        returnTime = 0f;
    }

    private void Update()
    {
        if (isReturning)
        {
            returnTime += Time.deltaTime / returnAnimationTime;
            if (returnTime >= 1)
            {
                returnTime = 1;
                isReturning = false;
                gameObject.SetActive(false);
            }
            uiStick.transform.position = Vector2.Lerp(returnFrom, anchorPoint, (returnTime*2));
            fade = Mathf.Lerp(0f, 1f,1-returnTime);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuckShooter : MonoBehaviour
{
    [SerializeField] 
    private GameObject puckPrefab;

    private GameObject puck;
    
    public void Shoot(Vector2 shootDirection)
    {
        if (puck == null)
        {
            puck = GameObject.Find("NetworkPuck");
        }

        if (puck != null)
        {
            var rb = puck.GetComponent<Rigidbody2D>();
            rb.AddForce(-(shootDirection*0.05f),ForceMode2D.Impulse);            
        }
    }
}

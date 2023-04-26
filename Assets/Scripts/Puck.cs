using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Mirror.Experimental;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public class Puck : MonoBehaviour
{
    [SerializeField]
    private float puckForce = 0.5f;

    [SerializeField]
    private AudioSource _collisionSound;

    private float _collisionSoundCooldownTime = 0.05f;
    private float _collisionSoundCooldownTimer; 
    
    public NetworkIdentity netId => _netId;
    private NetworkIdentity _netId;
    public event Action<Puck,bool> SideChange;
    private Rigidbody2D _rb;
    private NetworkRigidbody2D _rbNetwork;
    private bool _isOnTopSide;
    private Vector3 startPosition;
    
    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _netId = GetComponent<NetworkIdentity>();
    }

    public void AddForce(Vector2 pDirection,ForceMode2D pForceMode)
    {
        _rb.AddForce(pDirection*puckForce,pForceMode);
    }

    public void Update()
    {
        bool oldValue = _isOnTopSide;
        _isOnTopSide = IsOnTopSide();

        _collisionSoundCooldownTimer += Time.deltaTime;

        if (oldValue != _isOnTopSide)
        {
            SideChange?.Invoke(this,_isOnTopSide);
        }
    }

    public void ResetPosition()
    {
        transform.position = startPosition;
        _rb.velocity = Vector2.zero;
    }

    public bool IsOnTopSide()
    {
        if (transform.position.y > 0)
            return true;
        return false;
    }
    
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (_collisionSoundCooldownTimer > _collisionSoundCooldownTime)
        {
            _collisionSound.Play();
            _collisionSoundCooldownTimer = 0;
        }
    }
}

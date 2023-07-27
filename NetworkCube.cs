using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkCube : NetworkBehaviour
{
    [SerializeField] Vector3 _dropRate;
    bool _startDrop = false;
    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.position;
    }

    // Start is called before the first frame update
    [Server]
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            StartCoroutine(CountDownDrop());
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        transform.position = _startPos;
    }

    private void Update()
    {
        if(_startDrop)
            transform.Translate(_dropRate * Time.deltaTime);
    }

    private IEnumerator CountDownDrop()
    {
        yield return new WaitForSeconds(1.5f);
        _startDrop = true;
    }


}

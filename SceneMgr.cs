using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneMgr : NetworkBehaviour
{
    public GameObject GridPrefab;

    public override void OnStartServer()
    {
        base.OnStartServer();
        Spawn(GridPrefab);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {

        }
    }
}

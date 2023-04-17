using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Components : RaycastHandler
{
    internal PlayerController p;
    internal SpriteRenderer sp;

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();

        p = FindObjectOfType<PlayerController>().GetComponent<PlayerController>();
        sp = GetComponent<SpriteRenderer>();
    }
}
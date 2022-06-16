using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //A Source Engine-Style player controller
    public float speed = 10.0f;
    public float jumpPower = 10.0f;
    public float gravity = 9.8f;
    void Start()
    {

    }

    void Update()
    {
        //Move the player
        float sts = Input.GetAxis("Horizontal");
        float fab = Input.GetAxis("Vertical");
    }
}

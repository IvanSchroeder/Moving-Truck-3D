using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using MoreMountains.NiceVibrations;

public class FollowMouse : MonoBehaviour
{
    private Camera cam;
    private bool isPressed = false;
    //private Vector3 lastPos;

    private GameObject Hand;

    private bool canTurnOnPencil = false;

    void Start()
    {
        Hand = transform.GetChild(0).gameObject;
        Hand.SetActive(false);
        cam = Camera.main;
    }

    private void Update()
    {
        /*if (GameManager.Instance.EndLevel)
        {
            gameObject.SetActive(false);
        }*/

        if (Input.GetMouseButtonDown(0))
        {
            isPressed = true;
            if (canTurnOnPencil)
            {
                Hand.SetActive(true);
            }
        }
        if (isPressed)
        {
            MoveToMouse();
        }
        if (Input.GetMouseButtonUp(0))
        {
            isPressed = false;
            Hand.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            canTurnOnPencil = true;
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            canTurnOnPencil = false;
        }

    }

    public void MoveToMouse()
    {
        float distance = cam.nearClipPlane +.15f ;
        Vector3 targetPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, distance);
        transform.position = targetPos;//cam.ScreenToWorldPoint(targetPos);


        //lastPos = transform.position;
    }

}
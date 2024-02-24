using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class destroyMe : MonoBehaviour
{
    public void destroyGameObject()
    {
        Destroy(gameObject);
    }

    public void destroyParent()
    {
        Destroy(transform.parent.gameObject);
    }
}

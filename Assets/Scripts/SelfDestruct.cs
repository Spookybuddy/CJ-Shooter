using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(Implode());
    }

    private IEnumerator Implode()
    {
        yield return new WaitForSeconds(0.17f);
        Destroy(gameObject);
    }
}
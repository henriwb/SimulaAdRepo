using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleEndCardCallScript : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(OpenEndCard());
    }
    
    IEnumerator OpenEndCard()
    {
        yield return new WaitForSeconds(2);
        EndCardController.Instance.OpenEndCard(); //Call this line anywhere to open the end card and start the animations.
    }
}

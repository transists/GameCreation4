using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TItleManager : MonoBehaviour
{
    public string nextSceneName = "";
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Space)) // A�{�^�� or Space�L�[�ŊJ�n
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}

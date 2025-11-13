using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TItleManager : MonoBehaviour
{
    public string nextSceneName = "";
    AudioSource SE;
    public AudioClip enter;
    
    // Start is called before the first frame update
    void Start()
    {
        SE = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Space)) // Aボタン or Spaceキーで開始
        {
            SE.PlayOneShot(enter);
            Invoke("EnterGame", 1f);
        }
    }

    void EnterGame()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Goal : MonoBehaviour
{
    public string nextSceneName = "";
    //private bool loading = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var pc = other.GetComponent<PlayerController>();
        if (pc == null) return; // プレイヤー以外は無視

        if (!pc.CanGoal)
        {
            // ロック中：ゴールしない
            Debug.Log("Goal locked: detected recently, wait a bit.");
            return;
        }

        // ゴールOK：シーン遷移
        SceneManager.LoadScene(nextSceneName);
    }
}

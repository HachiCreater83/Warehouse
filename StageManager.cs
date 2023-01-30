using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class StageManager : MonoBehaviour
{
    [SerializeField, Header("シーン切り替え時に表示されるCanvas")] private GameObject CutInCanvas = null;
    [SerializeField, Header("このScene後に飛ばすScene名")] private string nextSceneName;


    //コルーチン一定時間後に次の処理する
    public IEnumerator DelayCoroutine()
    {
        CutInCanvas.SetActive(true);
        // Time.timeScale の影響を受けずに実時間で2秒待つ
        yield return new WaitForSecondsRealtime(2);
        //2秒後にシーンを切り替える
        //ここに次のシーンへいく命令を書く
        SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
    }
    //ゲームシーンを読み込み
    public void OnClickStart()
    {
        GetComponent<AudioSource>().Play();
        StartCoroutine(DelayCoroutine());
    } 

    //現在のシーンを再読み込み
    public void OnClickButtonRetry()
    {
        GetComponent<AudioSource>().Play();
        StartCoroutine(DelayCoroutine());
    }
    //ゲームプレイ終了
    public void OnclickStarButtonExit()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
    //ゲームプレイ終了
    Application.Quit();
    #endif
    }
}

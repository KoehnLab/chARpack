using UnityEngine;
using UnityEngine.SceneManagement;

public class Login : MonoBehaviour
{

    private static Login _singleton;

    public static Login Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(Login)} instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }

    public GameObject serverListInstance;

    private void Awake()
    {
        Singleton = this;
    }


    public void normal()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void host()
    {
        SceneManager.LoadScene("ServerScene");
    }

    public void quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    public void showServerList()
    {
        GameObject serverListPrefab = (GameObject)Resources.Load("prefabs/ServerList");
        if (serverListInstance == null)
        {
            Vector3 spawnPos = transform.position - new Vector3(1, 0, 0) * 0.1f + new Vector3(0, 1, 0) * 0.1f;
            serverListInstance = Instantiate(serverListPrefab, spawnPos, Quaternion.identity);
            gameObject.SetActive(false);
        }

    }
}

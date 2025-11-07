using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EditorSceneRestart : MonoBehaviour
{
#if UNITY_EDITOR
    private void Update()
    {
        // Hold down R key to restart scene
        if (Input.GetKey(KeyCode.R))
        {
            Debug.Log("Restarting scene...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
#endif
}


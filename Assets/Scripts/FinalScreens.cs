using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalScreens : MonoBehaviour {
    [SerializeField] private GameObject WinScreen = null;
    [SerializeField] private GameObject LevelLabel = null;
    [SerializeField] private GameObject RestartButton = null;
    [SerializeField] private GameObject ObjectsLabel = null;
    [SerializeField] private GameObject ObjectsPanel = null;
    [SerializeField] private GameObject ObjectsPanelWithText = null;
    [SerializeField] private float delaySeconds;
    private bool result;

    private void OnEnable() {
        DragAndDrop.OnGameWin += LevelWin;
    }

    private void OnDisable() {
        DragAndDrop.OnGameWin -= LevelWin;
    }

    private void LevelWin() {
        result = true;
        LevelFinished(result);
    }

    public void LevelFinished(bool result) {
        if (result) {
            WinScreen.SetActive(true);
            LevelLabel.SetActive(true);
            RestartButton.SetActive(true);

            LeanTween.scale(WinScreen, Vector3.one, 0.5f).setEaseOutBounce().setDelay(delaySeconds);
            LeanTween.scale(LevelLabel, Vector3.zero, 0.5f).setEaseInBack();
            LeanTween.scale(RestartButton, Vector3.zero, 0.5f).setEaseInBack();
            //LeanTween.scale(ObjectsLabel, Vector3.zero, 1f).setEaseInBack().setDelay(0.5f);
            LeanTween.scale(ObjectsPanel, new Vector3(1f, 0f, 1f), 0.5f).setEaseInBack().setDelay(0.5f);
            LeanTween.scale(ObjectsPanelWithText, new Vector3(0f, 1f, 0f), 0.5f).setEaseInBack();
        }
    }
}

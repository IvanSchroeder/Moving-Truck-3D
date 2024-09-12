using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour {
    [Header("Component")]
    [SerializeField] private TextMeshProUGUI levelLabel;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI objectsLabel;
    [SerializeField] private TextMeshProUGUI objectsText;
    [SerializeField] private GameObject objectsPanels;
    [SerializeField] private GameObject objectsPanelWithText;
    [SerializeField] private GameObject tutorialLabels;
    [SerializeField] private GameObject firstTutorialLabel;
    [SerializeField] private GameObject secondTutorialLabel;
    [SerializeField] private LevelData levelData;
    [SerializeField] private Camera canvasCamera;

    [SerializeField] private LayerMask mask01;
    [SerializeField] private LayerMask mask02;

    private void OnEnable() {
        LevelManager.OnLevelStart += SetLevelData;
        LevelManager.OnLevelUpdate += UpdateLevelText;

        DragAndDrop.OnObjectListChange += UpdateObjectsText;
        DragAndDrop.OnGameWin += SetCamerasCullingMasks;
        DragAndDrop.OnFirstDrag += SetTutorialsDrag;
        DragAndDrop.OnFirstLoad += SetTutorialsLoad;
    }

    private void OnDisable() {
        LevelManager.OnLevelStart -= SetLevelData;
        LevelManager.OnLevelUpdate -= UpdateLevelText;

        DragAndDrop.OnObjectListChange -= UpdateObjectsText;
        DragAndDrop.OnGameWin -= SetCamerasCullingMasks;

        DragAndDrop.OnFirstDrag -= SetTutorialsDrag;
        DragAndDrop.OnFirstLoad -= SetTutorialsLoad;
    }

    private void Start() {
        canvasCamera = GameObject.FindGameObjectWithTag("CanvasCamera").GetComponent<Camera>();

        UpdateObjectsText(levelData.ObjectsToInstantiate.Count);

        objectsPanels.transform.localScale = new Vector3(1f, 0f, 1f);
        LeanTween.scale(objectsPanels, Vector3.one, 0.5f).setEaseOutBack();

        objectsPanelWithText.transform.localScale = new Vector3(1f, 0f, 1f);
        LeanTween.scale(objectsPanelWithText, Vector3.one, 0.5f).setEaseOutBack().setDelay(0.5f);

        UpdateTutorialsText();
    }

    private void SetLevelData(LevelData _currentLevel) {
        levelData = _currentLevel;
    }

    private void UpdateLevelText(int level) {
        level++;
        levelText.text = level.ToString("000");
    }

    private void UpdateObjectsText(int amount) {
        objectsText.text = amount.ToString("0");

        LeanTween.cancel(objectsText.gameObject);
        objectsText.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
        LeanTween.scale(objectsText.gameObject, new Vector3(1.5f, 1.5f, 1.5f), 1f).setEase(LeanTweenType.punch);
    }

    private void UpdateTutorialsText() {
        if (levelData.hasTutorial) {
            tutorialLabels = Instantiate(levelData.tutorialScreen, levelData.tutorialScreen.transform.localPosition, Quaternion.Euler(Vector3.zero));
            tutorialLabels.transform.SetParent(this.transform);
            tutorialLabels.transform.localScale = new Vector3(1f, 1f, 1f);
            tutorialLabels.transform.localPosition = new Vector3(0f, 0f ,0f);
            tutorialLabels.transform.localRotation = Quaternion.Euler(Vector3.zero);
            LeanTween.scale(tutorialLabels, tutorialLabels.transform.localScale * 1.1f, 0.5f).setEaseOutSine().setLoopPingPong();

            firstTutorialLabel = tutorialLabels.transform.GetChild(0).gameObject;
            secondTutorialLabel = tutorialLabels.transform.GetChild(1).gameObject;

            secondTutorialLabel.SetActive(false);
        }
    }

    private void SetTutorialsDrag() {
        firstTutorialLabel.SetActive(false);
        secondTutorialLabel.SetActive(true);
    }

    private void SetTutorialsLoad() {
        secondTutorialLabel.SetActive(false);
    }

    private void SetCamerasCullingMasks() {
        Camera mainCamera = Utils.GetMainCamera();
        mainCamera.cullingMask = mask01;
        canvasCamera.cullingMask = mask02;
    }

    private void Show(Camera camera, string layerName) {
        camera.cullingMask |= 1 << LayerMask.NameToLayer(layerName);
    }

    private void Hide(Camera camera, string layerName) {
        camera.cullingMask &= ~(1 << LayerMask.NameToLayer(layerName));
    }

    private void Toggle(Camera camera, string layerName) {
        camera.cullingMask ^= 1 << LayerMask.NameToLayer(layerName);
    }
}

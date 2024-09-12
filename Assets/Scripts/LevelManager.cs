using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class LevelManager : MonoBehaviour {
    public static LevelManager current;

    [SerializeField] private LevelData currentLevelData;
    [SerializeField] private LevelData nextLevelData;
    private static LevelData s_nextLevelData;

    [SerializeField] public List<GameObject> ObjectsList;
    [SerializeField] public List<LevelData> LevelsList;
    [SerializeField] public List<LevelData> RandomLevelsList;

    [SerializeField] public List<LevelData> InspectorLevels;
    private static List<LevelData> ShortenedList = new List<LevelData>();

    [SerializeField] private int levelCount;

    [SerializeField] public GameObject slotPrefab;
    [SerializeField] public GameObject dummySlotPrefab;
    [SerializeField] private GameObject scrollContent;

    [SerializeField] private int objectsAmount;

    public static Action<LevelData> OnLevelStart;
    public static Action<int> OnLevelUpdate;
    public static Action OnObjectsCreated;

    // Tiny Sauce
    private bool isUserCompleteLevel;
    private int score = 100;

    private void Awake() {
        if (current == null) {
            current = this;
        }
        else if (current != null) {
            Destroy(gameObject);
        }

        LoadCurrentLevel();
    }

    private void Start() {
        OnLevelUpdate?.Invoke(levelCount);
        OnLevelStart?.Invoke(currentLevelData);

        objectsAmount = currentLevelData.ObjectsToInstantiate.Count;

        int count = 0;
        foreach(GameObject obj in currentLevelData.ObjectsToInstantiate) {
            Vector3 rotationVector = new Vector3(0f, 0f, 0f);
            Quaternion rotation = Quaternion.Euler(rotationVector);
            Vector3 scale = new Vector3(0f, 0f, 0f);

            GameObject slot = Instantiate(slotPrefab, Vector3.zero, obj.transform.localRotation, scrollContent.transform);
            slot.transform.SetParent(scrollContent.transform);
            slot.transform.localPosition = new Vector3(0f, 0f, 0f);

            rotationVector = new Vector3(0f, 0f, 0f);
            rotation = Quaternion.Euler(rotationVector);
            slot.transform.localRotation = rotation;

            GameObject dummySlot = Instantiate(dummySlotPrefab, slot.transform.position, rotation);
            dummySlot.transform.SetParent(slot.transform);
            dummySlot.transform.localPosition = Vector3.zero;

            rotationVector = new Vector3(-90f, 0f, 0f);
            rotation = Quaternion.Euler(rotationVector);
            dummySlot.transform.localRotation = rotation;

            scale = new Vector3(1f, 1f, 1f);
            dummySlot.transform.localScale = new Vector3(0f, 0f, 0f);
            LeanTween.scale(dummySlot, scale, 0.5f).setEaseOutBack().setDelay(0.5f + count/100f);

            GameObject tempObj = Instantiate(obj, slot.transform.position, obj.transform.localRotation);
            IDraggable draggableComponent = tempObj.GetComponent<IDraggable>();
            tempObj.transform.SetParent(slot.transform, true);
            tempObj.transform.localPosition = dummySlot.transform.localPosition;

            scale = tempObj.transform.localScale;
            if (draggableComponent.resetScale) {
                scale = scale / draggableComponent.resetScaleAmount;
            }

            tempObj.transform.localScale = new Vector3(0f, 0f, 0f);
            LeanTween.scale(tempObj, scale, 0.5f).setEaseOutBack().setDelay(0.5f + count/100f);

            draggableComponent.scaleInUI = scale;
            draggableComponent.referenceSlot = slot;
            draggableComponent.dummySlot = dummySlot.transform;
            
            ObjectsList.Add(tempObj);
            count++;
        }

        OnObjectsCreated?.Invoke();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Q)) {
            ResetPrefs();
        }

        if (Input.GetKeyDown(KeyCode.E)) {
            ContinueNextLevel();
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            RestartScene();
        }
    }

    private void LoadCurrentLevel() {
        levelCount = PlayerPrefs.GetInt("CurrentLevel");
        TinySauce.OnGameStarted("Level " + (levelCount + 1).ToString());
        //Load from playerprefs
        if (ShortenedList.Count == 0) {
            ShortenedList = new List<LevelData>(RandomLevelsList);
            InspectorLevels = new List<LevelData>(ShortenedList);
        }

        if (levelCount < 0) {
            currentLevelData = LevelsList[0];
            levelCount = 0;
        }
        else if (levelCount <= LevelsList.Count - 1) {
            currentLevelData = LevelsList[levelCount];
        }
        else {
            SetRandomLevels();
        }

        Debug.Log("Loaded level " + currentLevelData + " from PlayerPrefs");
    }

    private void SetRandomLevels() {
        if (s_nextLevelData != null) {
            currentLevelData = s_nextLevelData;
        }
        else {
            currentLevelData = Utils.GetRandomElement(ShortenedList);
        }

        ShortenedList.Remove(currentLevelData);

        if (ShortenedList.Count >= 2) {
            s_nextLevelData = Utils.GetRandomElement(ShortenedList);

            while (s_nextLevelData == currentLevelData) {
                s_nextLevelData = Utils.GetRandomElement(RandomLevelsList);
            }
        }
        else if (ShortenedList.Count == 1) {
            s_nextLevelData = ShortenedList[0];
            ShortenedList = new List<LevelData>(RandomLevelsList);
        }
        else if (ShortenedList.Count == 0) {
            ShortenedList = new List<LevelData>(RandomLevelsList);

            s_nextLevelData = Utils.GetRandomElement(ShortenedList);

            while (s_nextLevelData == currentLevelData) {
                s_nextLevelData = Utils.GetRandomElement(RandomLevelsList);
            }
        }

        InspectorLevels = new List<LevelData>(ShortenedList);
        nextLevelData = s_nextLevelData;
    }

    public void RestartScene() {
        if (s_nextLevelData != null) {
            s_nextLevelData = currentLevelData;
            nextLevelData = s_nextLevelData;
        }
        Utils.RestartScene();
    }

    private void SendLevelData() {
        OnLevelStart?.Invoke(currentLevelData);
    }

    public void ContinueNextLevel() {
        levelCount++;
        PlayerPrefs.SetInt("CurrentLevel", levelCount);
        PlayerPrefs.Save();
        TinySauce.OnGameFinished(isUserCompleteLevel, score, "Level " + (levelCount + 1).ToString());
        Utils.RestartScene();
    }

    public void ResetPrefs() {
        levelCount = 0;
        PlayerPrefs.SetInt("CurrentLevel", levelCount);
        PlayerPrefs.Save();
        Debug.Log("Restarted Levels");
        RestartScene();
    }
}

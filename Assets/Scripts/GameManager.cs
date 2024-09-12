using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
    [Header("Component")]
    [SerializeField] private LevelData levelData;

    private void OnEnable() {
        LevelManager.OnLevelStart += SetLevelData;
    }

    private void Start() {
        Application.targetFrameRate = 60;
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            Application.Quit();
        }
    }

    private void SetLevelData(LevelData _currentLevel) {
        levelData = _currentLevel;
    }
}

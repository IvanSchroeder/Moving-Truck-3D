using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Assets/Scriptable Objects/Levels/New Level Data")]
public class LevelData : ScriptableObject {
    public List<GameObject> ObjectsToInstantiate;
    public bool hasTutorial;
    public GameObject tutorialScreen;
    public GameObject truckPrefab;
    public BoundsInt initialArea;
    public int objectsAmount;

    public void Start() {
        objectsAmount = ObjectsToInstantiate.Count;
    }
}

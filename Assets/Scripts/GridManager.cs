using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;

public class GridManager : MonoBehaviour {
    public static GridManager current;

    private Camera mainCamera;

    private static Dictionary<TileType, TileBase> tileBaseTipes;

    [Header("Components")]
    [SerializeField] private LevelData levelData;
    public GridLayout gridLayout;
    [NonSerialized] public Grid grid;
    [SerializeField] public Tilemap truckTilemap;
    [SerializeField] private Tilemap truckInteractiveTilemap;

    [Header("Tiles")]
    [SerializeField] private TileBase freeTile;
    [SerializeField] private TileBase fullTile;
    [SerializeField] private TileBase emptyTile;
    [SerializeField] private TileBase greenTile;
    [SerializeField] private TileBase yellowTile;
    [SerializeField] private TileBase redTile;

    [Header("References")]
    [SerializeField] public GameObject truck;
    [SerializeField] public GameObject truckBox;
    [SerializeField] public GameObject truckWall;
    [SerializeField] public GameObject truckTargetPosition;
    [SerializeField] private GameObject selectedObject = null;
    [SerializeField] private IDraggable draggedObject = null;
    [SerializeField] private AnimationCurve easingCurve;
    [SerializeField] private float weightAmount;

    [Header("Info")]
    [SerializeField] private BoundsInt initialArea;
    private bool isPressingMouse = false;
    public BoundsInt startingDragArea;
    public BoundsInt previousDragArea;
    public BoundsInt currentDragArea;
    
    private Coroutine SelectCoroutine;
    private WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

    public static Action OnTargetCellSelected;
    public static Action<bool> OnNewCellSelected;
    public static Action<Vector3Int, bool> OnInsideBounds;
    public static Action<Vector3Int, bool> OnOutsideBounds;
    public static Action<bool> OnCellFree;
    public static Action<bool> OnCellFull;
    public static Action<bool> OnCellEmpty;

    private void OnEnable() {
        DragAndDrop.OnMouseInput += SetMouseState;
        DragAndDrop.OnObjectSelected += SetSelectedObject;
        DragAndDrop.OnGameWin += SetMainTileMapState;
        DragAndDrop.OnGameWin += StartTruck;

        DragAndDrop.OnDragStart += SetStartingObjectArea;
        DragAndDrop.OnObjectPlaced += TakeArea;
        DragAndDrop.OnObjectSnaped += WeightTruck;

        LevelManager.OnLevelStart += SetLevelData;
    }

    private void OnDisable() {
        DragAndDrop.OnMouseInput -= SetMouseState;
        DragAndDrop.OnObjectSelected -= SetSelectedObject;
        DragAndDrop.OnGameWin -= SetMainTileMapState;
        DragAndDrop.OnGameWin -= StartTruck;

        DragAndDrop.OnDragStart -= SetStartingObjectArea;
        DragAndDrop.OnObjectPlaced -= TakeArea;
        DragAndDrop.OnObjectSnaped -= WeightTruck;

        LevelManager.OnLevelStart -= SetLevelData;
    }

    private void Awake() {
        if (current == null) {
            current = this;
        }
        else if (current != null) {
            Destroy(gameObject);
        }

        if (grid == null) grid = gridLayout.gameObject.GetComponent<Grid>();
        if (mainCamera == null) mainCamera = Utils.GetMainCamera();
    }

    private void Start() {
        tileBaseTipes = new Dictionary<TileType, TileBase>();

        tileBaseTipes.Add(TileType.Free, freeTile);
        tileBaseTipes.Add(TileType.Full, fullTile);
        tileBaseTipes.Add(TileType.Empty, emptyTile);
        tileBaseTipes.Add(TileType.Green, greenTile);
        tileBaseTipes.Add(TileType.Yellow, yellowTile);
        tileBaseTipes.Add(TileType.Red, redTile);

        Vector3Int startingTruckCellPosition = Utils.GetCellCoordinatePosition(levelData.truckPrefab.transform.position, truckTilemap);
        //Vector3 startingTruckWorldPosition = Utils.GetCellWorldPosition(startingTruckCellPosition, truckTilemap);
        Vector3 startingTruckWorldPosition = truckTilemap.GetCellCenterWorld(startingTruckCellPosition);

        truck = Instantiate(levelData.truckPrefab, levelData.truckPrefab.transform.position, levelData.truckPrefab.transform.rotation);
        truck.transform.position = new Vector3(startingTruckWorldPosition.x, truck.transform.position.y, startingTruckWorldPosition.z);
        truckBox = truck.transform.GetChild(0).transform.GetChild(1).gameObject;
        truckWall = truckBox.transform.GetChild(0).gameObject;

        transform.parent = truckBox.transform;

        initialArea.size = levelData.initialArea.size;
        initialArea.position = startingTruckCellPosition;
        
        truckTilemap.ResizeBounds();
        truckTilemap.CompressBounds();

        truckInteractiveTilemap.ResizeBounds();
        truckInteractiveTilemap.CompressBounds();

        ClearArea(truckTilemap.cellBounds, TileType.Empty, truckTilemap);
        ClearArea(initialArea, TileType.Free, truckTilemap);
        ClearArea(truckTilemap.cellBounds, TileType.Empty, truckInteractiveTilemap);
    }

    private IEnumerator SelectNewCell() {
        currentDragArea.position = truckTilemap.WorldToCell(selectedObject.transform.position);
        draggedObject.currentCellData = currentDragArea;

        while (selectedObject != null && isPressingMouse) {
            if (currentDragArea != previousDragArea) {
                FollowObject();
                OnNewCellSelected?.Invoke(true);
            }

            previousDragArea = currentDragArea;

            yield return waitForEndOfFrame;
        }
    }

    #region ----- Useful Methods -----

    private void SetMouseState(bool _isPressingMouse) {
        isPressingMouse = _isPressingMouse;

        if (isPressingMouse && selectedObject != null) {
            ResetCoroutine(SelectCoroutine);
            SelectCoroutine = StartCoroutine(SelectNewCell());
        }
        else if (!isPressingMouse && selectedObject != null) {
            OnTargetCellSelected?.Invoke();
        }
    }

    public void SetSelectedObject(GameObject obj) {
        selectedObject = obj;
        
        if (selectedObject != null) {
            draggedObject = obj.GetComponent<IDraggable>();
        }
        else {
            draggedObject = null;
        }
    }

    private void ResetCoroutine(Coroutine coroutine) {
        if (coroutine != null) {
            StopCoroutine(coroutine);
            coroutine = null;
        }
    }

    private void SetMainTileMapState() {
        truckTilemap.GetComponent<TilemapRenderer>().enabled = false;
    }

    private void SetLevelData(LevelData data) {
        levelData = data;
    }

    private void StartTruck() {
        LeanTween.scale(truckWall, Vector3.one, 1f).setEaseOutBack().setDelay(0.5f);
        LeanTween.move(truck, truck.transform.position, 1f).setEaseInBack().setDelay(1f);
        LeanTween.move(truck, truckTargetPosition.transform.position, 5f).setEaseInOutQuad().setDelay(2f);
    }

    private void WeightTruck() {
        LeanTween.cancel(truckBox);
        LeanTween.moveLocalY(truckBox, weightAmount, 1f).setEasePunch();
    }

    #endregion

    #region ----- Tile Detection Methods -----

    private void SetStartingObjectArea() {
        currentDragArea.position = Utils.GetCellCoordinatePosition(draggedObject.transform.position, truckTilemap);

        currentDragArea.size = draggedObject.cellSize.size;

        startingDragArea = currentDragArea;

        draggedObject.startingCellData = startingDragArea;

        draggedObject.currentCellData = currentDragArea;

        TileBase[] tilesUnderObjectArray = GetTilesBlock(currentDragArea, truckTilemap);

        int size = tilesUnderObjectArray.Length;
        TileBase[] tileArray = new TileBase[size];

        if (draggedObject.isPlaced) {
            SetTilesBlock(currentDragArea, TileType.Free, truckTilemap);
            SetTilesBlock(currentDragArea, TileType.Green, truckInteractiveTilemap);
        }
        else {
            SetTilesBlock(currentDragArea, TileType.Red, truckInteractiveTilemap);
        }
    }

    private void FollowObject() {
        TileBase[] tilesUnderObjectArray = GetTilesBlock(currentDragArea, truckTilemap);

        int size = tilesUnderObjectArray.Length;
        TileBase[] filledTileArray = new TileBase[size];

        for (int i = 0; i < size; i++) {
            var tile = tilesUnderObjectArray[i];

            if (tile == tileBaseTipes[TileType.Free] || tile == tileBaseTipes[TileType.Full]) {
                OnInsideBounds?.Invoke(currentDragArea.position, true);
            }
            else if (tile == tileBaseTipes[TileType.Empty]) {
                OnOutsideBounds?.Invoke(currentDragArea.position, false);
            }

            if (tile == tileBaseTipes[TileType.Free]) {
                filledTileArray[i] = tileBaseTipes[TileType.Green];
            }
            else if (tile == tileBaseTipes[TileType.Full]) {
                //filledTileArray[i] = tileBaseTipes[TileType.Yellow];
                FillTiles(filledTileArray, TileType.Red);
                OnCellFull?.Invoke(false);
                break;
            }
            else if (tile == tileBaseTipes[TileType.Empty] || tile == null) {
                //filledTileArray[i] = tileBaseTipes[TileType.Red];
                FillTiles(filledTileArray, TileType.Red);
                break;
            }

            OnCellFree?.Invoke(true);
        }

        SetTilesBlock(previousDragArea, TileType.Empty, truckInteractiveTilemap);
        truckInteractiveTilemap.SetTilesBlock(currentDragArea, filledTileArray);
    }

    public bool CanTakeArea(BoundsInt areaUnderObject, Tilemap tilemap) {
        TileBase[] tilesUnderObject = GetTilesBlock(areaUnderObject, tilemap);

        foreach (var tile in tilesUnderObject) {
            if (tile == tileBaseTipes[TileType.Full] || tile == tileBaseTipes[TileType.Empty] || tile == null) {
                return false;
            }
        }

        return true;
    }

    public bool CanRotateArea(BoundsInt previousAreaUnderObject, BoundsInt newAreaUnderObject, Tilemap tilemap) {
        ClearArea(previousAreaUnderObject, TileType.Free, truckTilemap);
        TileBase[] tilesUnderNewArea = GetTilesBlock(newAreaUnderObject, tilemap);

        foreach (var tile in tilesUnderNewArea) {
            if (tile == tileBaseTipes[TileType.Full] || tile == tileBaseTipes[TileType.Empty] || tile == null) {
                ClearArea(previousAreaUnderObject, TileType.Full, truckTilemap);
                return false;
            }
        }

        ClearArea(newAreaUnderObject, TileType.Full, truckTilemap);
        return true;
    }

    public void TakeArea(bool canBePlaced, bool wasPlaced) {
        SetTilesBlock(draggedObject.currentCellData, TileType.Empty, truckInteractiveTilemap);

        if (canBePlaced && !wasPlaced) { //from UI to truck
            SetTilesBlock(draggedObject.currentCellData, TileType.Full, truckTilemap);
        }
        else if (canBePlaced && wasPlaced) { // from truck to truck
            SetTilesBlock(draggedObject.startingCellData, TileType.Free, truckTilemap);
            SetTilesBlock(draggedObject.currentCellData, TileType.Full, truckTilemap);
        }
        /*else if (!canBePlaced && !wasPlaced) { // from UI to UI
            // Do nothing on main tilemap;
        }*/
        else  if (!canBePlaced && wasPlaced) { // from truck to UI
            SetTilesBlock(draggedObject.startingCellData, TileType.Free, truckTilemap);
        }

        startingDragArea = new BoundsInt();
        currentDragArea = new BoundsInt();
    }

    private void ClearArea(BoundsInt area, TileType tileType, Tilemap tilemap) {
        TileBase[] tilesToClear = new TileBase[area.size.x * area.size.y * area.size.z];
        FillTiles(tilesToClear, tileType);
        tilemap.SetTilesBlock(area, tilesToClear);
    }

    #endregion

    #region ----- Tilemap Extension Methods -----

    private static TileBase[] GetTilesBlock(BoundsInt area, Tilemap tilemap) {
        TileBase[] array = new TileBase[area.size.x * area.size.y * area.size.z];
        int counter = 0;

        foreach (var v in area.allPositionsWithin) {
            Vector3Int pos = new Vector3Int(v.x, v.y, 0);
            array[counter] = tilemap.GetTile(pos);
            counter++;
        }

        return array;
    }

    private static void SetTilesBlock(BoundsInt area, TileType tileType, Tilemap tilemap) {
        int size = area.size.x * area.size.y * area.size.z;
        TileBase[] tileArray = new TileBase[size];
        FillTiles(tileArray, tileType);
        tilemap.SetTilesBlock(area, tileArray);
    }

    private static void FillTiles(TileBase[] arr, TileType tileType) {
        for (int i = 0; i < arr.Length; i++) {
            arr[i] = tileBaseTipes[tileType];
        }
    }

    #endregion
}

public enum TileType {
    Free,
    Full,
    Empty,
    Green,
    Yellow,
    Red
}
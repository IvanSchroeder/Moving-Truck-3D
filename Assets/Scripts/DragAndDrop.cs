using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

//[RequireComponent(typeof(MeshCollider))]
public class DragAndDrop : MonoBehaviour {
    private Camera mainCamera;
    [SerializeField] private GridLayout gridLayout;

    [Header("Objects References")]
    [SerializeField] private List<IDraggable> ObjectsInUI;
    [SerializeField] private List<IDraggable> ObjectsInTruck;
    [SerializeField] private LevelData levelData;
    [SerializeField] private IDraggable draggedObject;
    [SerializeField] private GameObject selectedObject;
    [SerializeField] private GameObject currentSelectedObject;
    [SerializeField] private GameObject previousSelectedObject;
    [SerializeField] private GameObject previewSelectedObject;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Material whiteTransparent;
    [SerializeField] private Material greenTransparent;
    [SerializeField] private Material redTransparent;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private ParticleSystem starParticles;
    [SerializeField] private ParticleSystem starAndRibbonsParticles;

    public Dictionary<GameObject, MeshRenderer> previewObjectsModels;

    [Header("Input Settings")]
    [SerializeField] private float elapsedInputTime;
    [SerializeField] private float inputTimeThreshold;
    [SerializeField] private float YinputDistanceThreshold;
    [SerializeField] private float XinputDistanceThreshold;
    public bool hasPressedMouse;
    public bool hasLiftedMouse;
    public bool isDraggingObject;
    public bool isRotating;
    public bool lockInput = true;
    public bool lockRotation = false;
    public bool isCheckingInput = false;
    public bool firstDrag = false;
    public bool firstLoad = false;
    [SerializeField] private Vector2 firstPressPos;
    [SerializeField] private float distance;
    [SerializeField] private Vector2 currentSwipe;

    [Header("Lerping Settings")]
    [SerializeField] private AnimationCurve speedCurve;
    [SerializeField] private AnimationCurve localSpeedCurve;
    [SerializeField] private float desiredDragDuration;
    [SerializeField] private float desiredLerpDuration;
    [SerializeField] private float desiredRotationDuration;
    [SerializeField] private float dragOffsetx;
    [SerializeField] private float dragOffsety;
    [SerializeField] private float dragOffsetz;
    private bool hasSelectedNewCell = false;

    // Coroutines
    private Coroutine InputCoroutine;
    private Coroutine DragCoroutine;
    private Coroutine LerpCoroutine;
    private Coroutine LocalLerpCoroutine;
    private Coroutine LocalLerpCoroutine2;
    private Coroutine RotationCoroutine;
    private WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

    // Events
    public static Action<GameObject> OnObjectSelected;
    public static Action<bool> OnMouseInput;
    public static Action OnDragStart;
    public static Action OnDragEnd;
    public static Action OnGameWin;
    public static Action<bool, bool> OnObjectPlaced;
    public static Action OnObjectSnaped;
    public static Action<int> OnObjectListChange;

    public static Action OnFirstDrag;
    public static Action OnFirstLoad;

    private void OnEnable() {
        LevelManager.OnLevelStart += SetLevelData;
        //LevelManager.OnObjectsCreated += UnlockInput;

        GridManager.OnNewCellSelected += ChangeSelectionState;
        GridManager.OnTargetCellSelected += CheckAreaUnderObject;
        GridManager.OnTargetCellSelected += DisablePreviewModel;

        GridManager.OnInsideBounds += SetPreviewModelPosition;
        GridManager.OnOutsideBounds += SetPreviewModelPosition;

        GridManager.OnCellFree += SetPreviewModelColor;
        GridManager.OnCellFull += SetPreviewModelColor;
    }

    private void OnDisable() {
        LevelManager.OnLevelStart -= SetLevelData;
        //LevelManager.OnObjectsCreated -= UnlockInput;

        GridManager.OnNewCellSelected -= ChangeSelectionState;
        GridManager.OnTargetCellSelected -= CheckAreaUnderObject;
        GridManager.OnTargetCellSelected -= DisablePreviewModel;

        GridManager.OnInsideBounds -= SetPreviewModelPosition;
        GridManager.OnOutsideBounds -= SetPreviewModelPosition;

        GridManager.OnCellFree -= SetPreviewModelColor;
        GridManager.OnCellFull -= SetPreviewModelColor;
    }

    private void Awake() {
        if (mainCamera == null) mainCamera = Utils.GetMainCamera();
    }

    private void Start() {
        if (gridLayout == null) gridLayout = GridManager.current.gridLayout;

        previewObjectsModels = new Dictionary<GameObject, MeshRenderer>();
        ObjectsInUI = new List<IDraggable>();

        foreach(GameObject obj in LevelManager.current.ObjectsList) {
            IDraggable draggableObject = obj.GetComponentInChildren<IDraggable>();

            GameObject tempPreview = draggableObject.previewPrefab;
            draggableObject.previewObject = Instantiate(
                draggableObject.previewPrefab,
                obj.transform.position,
                draggableObject.previewPrefab.transform.rotation);

            previewObjectsModels.Add(draggableObject.previewObject, draggableObject.previewObject.GetComponentInChildren<MeshRenderer>());
            previewObjectsModels[draggableObject.previewObject].enabled = false;

            ObjectsInUI.Add(draggableObject);
        }

        Invoke("UnlockInput", 2f);

        /*int defaultValue = EventSystem.current.pixelDragThreshold;		
        EventSystem.current.pixelDragThreshold = Mathf.Max(defaultValue, (int) (defaultValue * Screen.dpi / 160f));*/
    }

    private void Update() {
        if (lockInput) return;

        if (Input.GetMouseButtonDown(0) && !isRotating) {
            hasPressedMouse = true;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, float.MaxValue, targetMask)) {
                draggedObject = hit.collider.transform.GetComponentInParent<IDraggable>();

                firstPressPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                selectedObject = draggedObject.gameObject;
                
                if (previousSelectedObject == selectedObject) currentSelectedObject = previousSelectedObject;
                else currentSelectedObject = selectedObject;

                isCheckingInput = true;

                ResetCoroutine(InputCoroutine);
                InputCoroutine = StartCoroutine(CheckInputMode());
            }
            else {
                ResetMouseInputChecks();
                return;
            }
        }

        if (hasPressedMouse && Input.GetMouseButtonUp(0)) {
            hasLiftedMouse = true;
            isCheckingInput = false;
        }

        if (Input.GetMouseButtonUp(0) && isDraggingObject) {
            isDraggingObject = false;
            firstPressPos = Vector3.zero;
            distance = 0f;
            currentSwipe = Vector3.zero;

            ResetCoroutine(LerpCoroutine);
            ResetCoroutine(DragCoroutine);
            ResetCoroutine(LocalLerpCoroutine);
            ResetCoroutine(LocalLerpCoroutine2);

            OnDragEnd?.Invoke();
            OnMouseInput?.Invoke(false);
        }
        else if (Input.GetMouseButtonUp(0) && !isDraggingObject) {
            ResetMouseInputChecks();
        }
    }

    private IEnumerator CheckInputMode() {
        elapsedInputTime = 0f;
        Vector2 currentMousePosition = firstPressPos;
        distance = Vector2.Distance(currentMousePosition, firstPressPos);
        currentSwipe = currentMousePosition - firstPressPos;

        bool hasInput = false;

        while (!hasInput) {
            currentMousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y); 
            distance = Vector2.Distance(currentMousePosition, firstPressPos);
            currentSwipe = currentMousePosition - firstPressPos;
            elapsedInputTime += Time.deltaTime;

            if (((YinputDistanceThreshold < Mathf.Abs(currentSwipe.y)) && (Mathf.Abs(currentSwipe.x) < XinputDistanceThreshold))
                || (inputTimeThreshold < elapsedInputTime)
                && !draggedObject.isPlaced) {
                elapsedInputTime = 0f;
                DragObject();
                hasInput = true;
                yield break;
            }
            else if ((YinputDistanceThreshold < Mathf.Abs(distance) || (inputTimeThreshold < elapsedInputTime))
                && draggedObject.isPlaced) {
                elapsedInputTime = 0f;
                DragObject();
                hasInput = true;
                yield break;
            }
            else if ((Mathf.Abs(currentSwipe.y) < YinputDistanceThreshold) && (Mathf.Abs(currentSwipe.x) < XinputDistanceThreshold) && !isCheckingInput) {
                elapsedInputTime = 0f;
                RecheckAreaForRotation();
                scrollRect.horizontal = true;
                hasInput = true;
                yield break;
            }
            else if (XinputDistanceThreshold < Mathf.Abs(currentSwipe.x)) {
                scrollRect.horizontal = true;
                hasInput = true;
                yield break;
            }

            yield return waitForEndOfFrame;
        }
    }

    private void DragObject() {

        isDraggingObject = true;

        DisableColliders();

        OnObjectSelected?.Invoke(currentSelectedObject);
        currentSelectedObject.transform.parent = null;
        LeanTween.cancel(draggedObject.gameObject);
        draggedObject.transform.localScale = new Vector3(1f, 1f, 1f);
        previewSelectedObject = draggedObject.previewObject;

        ResetCoroutine(DragCoroutine);
        ResetCoroutine(LerpCoroutine);
        ResetCoroutine(LocalLerpCoroutine);
        ResetCoroutine(LocalLerpCoroutine2);
        ResetCoroutine(draggedObject.SlotLerpCoroutine);

        DragCoroutine = StartCoroutine(DragUpdate());

        if (draggedObject.objectOrientation == ObjectOrientation.Horizontal) {
            LocalLerpCoroutine = StartCoroutine(LocalLerp(draggedObject.modelAreaPivot, draggedObject.horizontalAreaPivot.localPosition));
        }
        else if (draggedObject.objectOrientation == ObjectOrientation.Vertical) {
            LocalLerpCoroutine = StartCoroutine(LocalLerp(draggedObject.modelAreaPivot, draggedObject.verticalAreaPivot.localPosition));
        }

        LocalLerpCoroutine2 = StartCoroutine(LocalLerp(draggedObject.modelCenterPivot, draggedObject.modelPositionOffset));

        OnDragStart?.Invoke();

        draggedObject.isInUI = false;
        ObjectsInUI.Remove(draggedObject);
        ObjectsInTruck.Remove(draggedObject);

        if (draggedObject.isPlaced) {
            SetPreviewModelColor(true);
            SetPreviewModelPosition(Vector3Int.RoundToInt(currentSelectedObject.transform.position), true);

            OnObjectSnaped?.Invoke();
        }
        else {
            SetPreviewModelColor(false);
            SetPreviewModelPosition(Vector3Int.RoundToInt(currentSelectedObject.transform.position), false);

            OnObjectListChange?.Invoke(ObjectsInUI.Count);
        }

        foreach (IDraggable obj in ObjectsInUI) {
            obj.gameObject.transform.parent = null;
        }

        draggedObject.slotElement.ignoreLayout = true;

        scrollRect.horizontal = false;

        foreach (IDraggable obj in ObjectsInUI) {
            obj.SetSlotState();
        }

        OnMouseInput?.Invoke(true);

        Vector3 rotationVector = new Vector3(0f, 0f, 0f);
        Quaternion rotation = Quaternion.identity;

        rotationVector = new Vector3(0f, 0f, 0f);

        rotation = Quaternion.Euler(rotationVector);

        LeanTween.rotateLocal(draggedObject.gameObject, rotationVector, 0.5f).setEaseOutBack();

        if (levelData.hasTutorial) {
            if (!firstDrag) {
                OnFirstDrag?.Invoke();
                firstDrag = true;
            }
        }
    }

    private IEnumerator DragUpdate() {
        float localElapsedTime = 0f;
        float localPercentageComplete = 0f;

        hasSelectedNewCell = true;

        Vector3 mousePosition = new Vector3(0f, 0f, 0f);

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, float.MaxValue, groundMask)) {
            mousePosition = hit.point;
        }

        Vector3 dragOffset = currentSelectedObject.transform.position - mousePosition;
        Vector3 targetPosition = new Vector3(0f, 0f, 0f);

        while (Input.GetMouseButton(0)) {
            ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, float.MaxValue, groundMask)) {
                mousePosition = hit.point;
            }

            //targetPosition con offset
            mousePosition.x += dragOffsetx;
            mousePosition.y += dragOffsety;
            mousePosition.z += dragOffsetz;
            targetPosition = mousePosition + dragOffset;

            if (hasSelectedNewCell) {
                OnMouseInput?.Invoke(true);
            }

            if (currentSelectedObject.transform.position != targetPosition) {
                localElapsedTime += Time.deltaTime;
                localPercentageComplete = localElapsedTime / desiredDragDuration;
                currentSelectedObject.transform.position = Vector3.Lerp(currentSelectedObject.transform.position, targetPosition, speedCurve.Evaluate(localPercentageComplete));
            }
            else if (currentSelectedObject.transform.position == targetPosition) {
                localElapsedTime = 0f;
                localPercentageComplete = 0f;
            }

            yield return waitForEndOfFrame;
        }
    }

    private IEnumerator LerpToTruck(GameObject obj, Vector3 targetPosition) {
        float elapsedInputTime = 0f;
        float percentageComplete = 0f;
        
        while (elapsedInputTime < desiredLerpDuration) {
            elapsedInputTime += Time.deltaTime;
            percentageComplete = elapsedInputTime / desiredLerpDuration;

            obj.transform.position = Vector3.Lerp(obj.transform.position, targetPosition, speedCurve.Evaluate(percentageComplete));

            if (obj.transform.position == targetPosition || elapsedInputTime >= desiredLerpDuration) {
                obj.transform.position = targetPosition;
                ObjectsInTruck.Add(draggedObject);
                GameObject particles = Instantiate(starParticles.gameObject,
                    draggedObject.modelCenterPivot.position,
                    starParticles.gameObject.transform.rotation);
                particles.GetComponent<ParticleSystem>().Play();
                obj.transform.parent = GridManager.current.truckBox.transform;
                OnObjectSnaped?.Invoke();
                CheckWinCondition();
                yield break;
            }

            yield return waitForEndOfFrame;
        }
    }

    private IEnumerator LocalLerp(Transform pivot, Vector3 targetPosition) {
        float localElapsedTime = 0f;
        float localPercentageComplete = 0f;
        
        while (localElapsedTime < desiredLerpDuration) {
            localElapsedTime += Time.deltaTime;
            localPercentageComplete = localElapsedTime / desiredLerpDuration;

            pivot.localPosition = Vector3.Lerp(pivot.localPosition, targetPosition, localSpeedCurve.Evaluate(localPercentageComplete));

            if (pivot.localPosition == targetPosition) {
                pivot.localPosition = targetPosition;
                yield break;
            }

            yield return waitForEndOfFrame;
        }
    }

    private IEnumerator LerpRotation() {
        float elapsedTime = 0f;
        float percentageComplete = 0f;

        Vector3 rotationVector = Vector3.zero;
        Quaternion rotation = Quaternion.identity;
        Vector3 targetPosition = Vector3.zero;

        if (draggedObject.objectOrientation == ObjectOrientation.Horizontal) {
            rotationVector = new Vector3(0f, -90f, 0f);

            draggedObject.objectOrientation = ObjectOrientation.Vertical;
        }
        else if (draggedObject.objectOrientation == ObjectOrientation.Vertical) {
            rotationVector = new Vector3(0f, 0f, 0f);

            draggedObject.objectOrientation = ObjectOrientation.Horizontal;
        }

        rotation = Quaternion.Euler(rotationVector);

        if (draggedObject.isPlaced) {
            if (draggedObject.objectOrientation == ObjectOrientation.Horizontal) {
                targetPosition = draggedObject.horizontalAreaPivot.localPosition;
            }
            else if (draggedObject.objectOrientation == ObjectOrientation.Vertical) {
                targetPosition = draggedObject.verticalAreaPivot.localPosition;
            }
        }

        while (elapsedTime < desiredRotationDuration) {
            elapsedTime += Time.deltaTime;
            percentageComplete = elapsedTime / desiredRotationDuration;

            draggedObject.modelAreaPivot.localRotation = Quaternion.Lerp(draggedObject.modelAreaPivot.localRotation, rotation, speedCurve.Evaluate(percentageComplete));

            if (draggedObject.isPlaced) {
                draggedObject.modelAreaPivot.localPosition = Vector3.Lerp(draggedObject.modelAreaPivot.localPosition,
                    targetPosition, speedCurve.Evaluate(percentageComplete));
            }

            if (draggedObject.modelAreaPivot.localRotation == rotation) {
                draggedObject.modelAreaPivot.localRotation = rotation;
                previewObjectsModels[draggedObject.previewObject].transform.parent.parent.transform.localRotation = draggedObject.modelAreaPivot.localRotation;

                draggedObject.cellSize.size = new Vector3Int(
                    draggedObject.cellSize.size.y,
                    draggedObject.cellSize.size.x,
                    draggedObject.cellSize.size.z);

                isRotating = false;

                ResetMouseInputChecks();

                draggedObject = null;

                yield break;
            }

            yield return waitForEndOfFrame;
        }
    }

    public void CheckAreaUnderObject() {
        if (currentSelectedObject != null) {
            IDraggable draggedObject = currentSelectedObject.GetComponentInParent<IDraggable>();

            if (draggedObject != null) {
                Vector3Int coordinate = draggedObject.currentCellData.position;
                bool areaIsFree = GridManager.current.CanTakeArea(draggedObject.currentCellData, GridManager.current.truckTilemap);

                bool objectIsPlaced = draggedObject.isPlaced;

                if (areaIsFree) {
                    if (!objectIsPlaced) {
                        // from UI to truck
                        draggedObject.isPlaced = true;
                        draggedObject.isInUI = false;
                        OnObjectPlaced?.Invoke(draggedObject.isPlaced, false);
                    }
                    else if (objectIsPlaced) {
                        // from truck to truck
                        draggedObject.isPlaced = true;
                        draggedObject.isInUI = false;
                        OnObjectPlaced?.Invoke(draggedObject.isPlaced, true);
                    }

                    Place(coordinate, GridManager.current.truckTilemap);
                }
                else {
                    if (!objectIsPlaced) {
                        // from UI to UI
                        draggedObject.isPlaced = false;
                        draggedObject.isInUI = true;
                        OnObjectPlaced?.Invoke(draggedObject.isPlaced, false);
                    }
                    else if (objectIsPlaced) {
                        // from truck to UI
                        draggedObject.isPlaced = false;
                        draggedObject.isInUI = true;
                        OnObjectPlaced?.Invoke(draggedObject.isPlaced, true);
                    }

                    ReturnObjectoToSlotUI();
                }

                scrollRect.horizontal = true;
            }
        }
    }

    public void RecheckAreaForRotation() {
        if (!lockRotation) {
            if (draggedObject != null) {
                if (draggedObject.isInUI) {
                    isRotating = true;
                    ResetCoroutine(RotationCoroutine);
                    RotationCoroutine = StartCoroutine(LerpRotation());
                }
                else if (draggedObject.isPlaced) {
                    Vector3Int tempCellSize = new Vector3Int(
                            draggedObject.cellSize.size.y,
                            draggedObject.cellSize.size.x,
                            draggedObject.cellSize.size.z);
                    Vector3Int coordinate = draggedObject.currentCellData.position;

                    BoundsInt tempCellData = new BoundsInt(coordinate, tempCellSize);

                    bool areaIsFree = GridManager.current.CanRotateArea(draggedObject.currentCellData, tempCellData, GridManager.current.truckTilemap);

                    if (areaIsFree) {
                        draggedObject.currentCellData.position = coordinate;
                        draggedObject.currentCellData.size = tempCellSize;
                        draggedObject.startingCellData = draggedObject.currentCellData;
                        isRotating = true;
                        ResetCoroutine(RotationCoroutine);
                        RotationCoroutine = StartCoroutine(LerpRotation());
                    }
                    else {
                        isRotating = false;

                        //Shake position
                        LeanTween.cancel(draggedObject.modelCenterPivot.gameObject);
                        LeanTween.moveLocalZ(draggedObject.modelCenterPivot.gameObject, 0f, 0.5f).setEasePunch();

                        Debug.Log("Cant rotate object");
                    }

                    scrollRect.horizontal = true;
                }
            }
        }
        else {
            ResetMouseInputChecks();
        }
    }

    public void Place(Vector3Int coordinate, Tilemap tilemap) {
        Vector3 cellPosition = tilemap.GetCellCenterWorld(coordinate);

        ResetCoroutine(LerpCoroutine);

        if (currentSelectedObject != null) {
            LerpCoroutine = StartCoroutine(LerpToTruck(currentSelectedObject, cellPosition));
        }

        if (levelData.hasTutorial) {
            if (!firstLoad) {
                OnFirstLoad?.Invoke();
                firstLoad = true;
            }
        }
    }

    public void ReturnObjectoToSlotUI() {
        ResetCoroutine(LocalLerpCoroutine);
        ResetCoroutine(LocalLerpCoroutine2);
        ResetCoroutine(LerpCoroutine);

        LocalLerpCoroutine = StartCoroutine(LocalLerp(draggedObject.modelAreaPivot, draggedObject.startingModelAreaPivot));
        LocalLerpCoroutine2 = StartCoroutine(LocalLerp(draggedObject.modelCenterPivot, draggedObject.modelPositionCenter));

        ObjectsInUI.Add(draggedObject);
        ObjectsInTruck.Remove(draggedObject);
        OnObjectListChange?.Invoke(ObjectsInUI.Count);

        foreach (IDraggable obj in ObjectsInUI) {
            obj.gameObject.transform.parent = null;
        }

        draggedObject.slotElement.ignoreLayout = false;

        foreach (IDraggable obj in ObjectsInUI) {
            obj.SetSlotState();
        }

        if (draggedObject.resetScale) LeanTween.scale(draggedObject.gameObject, new Vector3(1f, 1f, 1f) / draggedObject.resetScaleAmount, 0.5f).setEaseOutBack();

        DisablePreviewModel();

        ResetObjectsSelection();
    }

    private void CheckWinCondition() {
        if (ObjectsInTruck.Count == levelData.objectsAmount) {
            lockInput = true;

            foreach (IDraggable obj in ObjectsInTruck) {
                GameObject particles = Instantiate(starAndRibbonsParticles.gameObject,
                    obj.modelCenterPivot.position,
                    starParticles.gameObject.transform.rotation);
                particles.GetComponent<ParticleSystem>().Play();
            }

            OnGameWin?.Invoke();

            Debug.Log("You win");
        }
        else {
            GameObject particles = Instantiate(starParticles.gameObject,
                draggedObject.modelCenterPivot.position,
                starParticles.gameObject.transform.rotation);
            particles.GetComponent<ParticleSystem>().Play();

            Debug.Log("Missing " + (levelData.objectsAmount - ObjectsInTruck.Count) + " objects");
        }

        ResetObjectsSelection();
    }

    private void ResetCoroutine(Coroutine coroutine) {
        if (coroutine != null) {
            StopCoroutine(coroutine);
            coroutine = null;
        }
    }

    private void EnableColliders() {
        foreach (GameObject obj in LevelManager.current.ObjectsList) {
            obj.GetComponentInChildren<BoxCollider>().enabled = true;
        }
    }

    private void DisableColliders() {
        foreach (GameObject obj in LevelManager.current.ObjectsList) {
            obj.GetComponentInChildren<BoxCollider>().enabled = false;
        }
    }

    private void EnablePreviewModel() {
        if (previewSelectedObject) previewObjectsModels[previewSelectedObject].enabled = true;
    }

    private void DisablePreviewModel() {
        if (previewSelectedObject) previewObjectsModels[previewSelectedObject].enabled = false;
    }

    private void ResetMouseInputChecks() {
        lockRotation = false;
        hasPressedMouse = false;
        hasLiftedMouse = false;
        isCheckingInput = false;
    }

    private void ResetObjectsSelection() {
        EnableColliders();

        previousSelectedObject = selectedObject;
        selectedObject = null;
        currentSelectedObject = null;
        previewSelectedObject = null;
        draggedObject = null;
        ResetMouseInputChecks();
    }

    private void SetPreviewModelColor(bool isFree) {
        Material[] materials = previewObjectsModels[previewSelectedObject].materials;

        if (isFree) {
            for (int i = 0; i < materials.Length; i++) {
                materials[i] = greenTransparent;
            }
        }
        else if (!isFree) {
            for (int i = 0; i < materials.Length; i++) {
                materials[i] = redTransparent;
            }
        }

        previewObjectsModels[previewSelectedObject].materials = materials;
    }

    private void SetPreviewModelPosition(Vector3Int position, bool state) {
        Vector3Int truckCellCoordinate = Utils.GetCellCoordinatePosition(currentSelectedObject.transform.position, GridManager.current.truckTilemap);

        if (draggedObject.objectOrientation == ObjectOrientation.Vertical && draggedObject.hasDepthCellSize) {
            truckCellCoordinate = Utils.GetCellCoordinatePosition(draggedObject.verticalAreaPivot.position, GridManager.current.truckTilemap);
        }

        if (state) {
            EnablePreviewModel();
        }
        else {
            DisablePreviewModel();
        }

        previewSelectedObject.transform.position = GridManager.current.truckTilemap.GetCellCenterWorld(truckCellCoordinate);
    }

    private void ChangeSelectionState(bool state) {
        hasSelectedNewCell = state;
    }

    private void SetLevelData(LevelData data) {
        levelData = data;
    }

    private void UnlockInput() {
        lockInput = false;
        Debug.Log("Input is unlocked!");
    }
}

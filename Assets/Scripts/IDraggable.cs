using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class IDraggable : MonoBehaviour {
    [SerializeField] public GameObject parentObject;
    [SerializeField] public Transform horizontalAreaPivot;
    [SerializeField] public Transform verticalAreaPivot;
    [SerializeField] public Transform modelAreaPivot;
    [SerializeField] public Transform modelCenterPivot;

    [SerializeField] public Vector3 modelPositionCenter;
    [SerializeField] public Vector3 modelPositionOffset;
    [SerializeField] public Vector3 startingModelAreaPivot;
    [SerializeField] public GameObject referenceSlot;
    [SerializeField] public Transform dummySlot;
    [SerializeField] public LayoutElement slotElement;

    [SerializeField] public GameObject previewPrefab;
    [SerializeField] public GameObject previewObject;

    [SerializeField] public Vector3 scaleInUI;
    [SerializeField] public Quaternion rotationInUI;
    [SerializeField] public bool resetScale = false;
    [SerializeField] public float resetScaleAmount;

    public BoundsInt cellSize;

    public ObjectOrientation objectOrientation;

    public BoundsInt startingCellData;
    public BoundsInt currentCellData;

    public bool isPlaced;
    public bool isInUI;
    public bool hasDepthCellSize = false;

    public float elapsedTime;
    public float desiredLerpDuration = 1f;
    private float percentageComplete;

    public Coroutine SlotLerpCoroutine;
    private WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

    private void Awake() {
        // Starts in UI
        isPlaced = false;
        isInUI = true;

        // Starts horizontal
        objectOrientation = ObjectOrientation.Horizontal;

        //Guardar offset para cuando vuelvan al camion, despues mover el modelo al centro y tambien guardar la posicion del centro para la calle
        SaveModelCenterPositions();
    }

    private void Start() {
        slotElement = referenceSlot.GetComponent<LayoutElement>();
        slotElement.ignoreLayout = false;

        rotationInUI = transform.localRotation;
    }

    public void SetSlotState() {
        if (isInUI) {
            ResetCoroutine(SlotLerpCoroutine);
            SlotLerpCoroutine = StartCoroutine(LerpToUI(this.gameObject, this));
        }
    }

    public void SaveModelCenterPositions() {
        modelPositionOffset = modelCenterPivot.localPosition;
        modelCenterPivot.localPosition = new Vector3(0f, modelCenterPivot.localPosition.y, 0f);
        modelPositionCenter = modelCenterPivot.localPosition;
        startingModelAreaPivot = modelAreaPivot.localPosition;
    }

    public IEnumerator LerpToUI(GameObject obj, IDraggable draggableComponent) {
        elapsedTime = 0f;
        percentageComplete = 0f;

        Vector3 targetPosition = draggableComponent.dummySlot.transform.position;

        LeanTween.cancel(this.gameObject);
        Vector3 rotationVector = new Vector3(0f, 0f, 0f);
        Quaternion rotation = Quaternion.identity;
        rotation = rotationInUI;
        rotationVector = new Vector3(rotation.x, rotation.y, rotation.z);
        LeanTween.rotateLocal(this.gameObject, rotationVector, 0.5f).setEaseOutBack();
        
        while (elapsedTime < desiredLerpDuration) {
            elapsedTime += Time.deltaTime;
            percentageComplete = elapsedTime / desiredLerpDuration;
            targetPosition = draggableComponent.dummySlot.transform.position;

            obj.transform.position = Vector3.Lerp(obj.transform.position, targetPosition, percentageComplete);

            if (elapsedTime >= desiredLerpDuration) {
                elapsedTime = 0f;
                percentageComplete = 0f;
                draggableComponent.slotElement.ignoreLayout = false;
                transform.parent = referenceSlot.transform;
                obj.transform.position = targetPosition;
                yield break;
            }

            yield return waitForEndOfFrame;
        }
    }

    private void ResetCoroutine(Coroutine coroutine) {
        if (coroutine != null) {
            StopCoroutine(coroutine);
            coroutine = null;
        }
    }
}

public enum ObjectOrientation {
    Horizontal,
    Vertical
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class Utils
{
    public static Vector3 ScreenToWorld(Camera camera, Vector3 position) {
        if (camera.orthographic) position.z = camera.nearClipPlane;
        position.z = 3.3f;
        return camera.ScreenToWorldPoint(position);
    }

    public static void RestartScene() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static int GetFirstIndex<T>(List<T> list) {
        int firstIndex = 0;
        return firstIndex;
    }

    public static T GetFirstElement<T>(List<T> list) {
        var firstElement = list[GetFirstIndex(list)];
        return firstElement;
    }

    public static int GetRandomIndex<T>(List<T> list) {
        int randomIndex = Random.Range(0, list.Count - 1);
        return randomIndex;
    }

    public static T GetRandomElement<T>(List<T> list) {
        var randomElement = list[GetRandomIndex(list)];
        return randomElement;
    }

    public static int GetLastIndex<T>(List<T> list) {
        int lastIndex = list.Count - 1;
        return lastIndex;
    }

    public static T GetLastElement<T>(List<T> list) {
        var lastElement = list[GetLastIndex(list)];
        return lastElement;
    }

    public static Camera GetMainCamera() {
        Camera camera = Camera.main;
        return camera;
    }

    public static Vector3 GetMouseWorldPosition() {
        Ray ray = GetMainCamera().ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit raycastHit)) {
            return raycastHit.point;
        }
        else {
            return Vector3.zero;
        }
    }

    public static Vector3Int GetMouseCellPosition(GridLayout gridLayout) {
        Vector3 mousePos = Utils.GetMouseWorldPosition();
        Vector3Int cellCoordinate = gridLayout.WorldToCell(mousePos);
        return cellCoordinate;
    }

    public static T GetCell<T>(Vector3Int coordinate, Tilemap tilemap) where T : TileBase {
        T tile = tilemap.GetTile<T>(coordinate);
        return tile;
    }

    public static Vector3Int GetCellCoordinatePosition(Vector3 cellPosition, GridLayout gridLayout) {
        Vector3Int cellCoordinate = gridLayout.WorldToCell(cellPosition);
        return cellCoordinate;
    }

    public static Vector3 GetCellWorldPosition(Vector3Int cellCoordinate, GridLayout gridLayout) {
        Vector3 cellPosition = gridLayout.CellToWorld(cellCoordinate);
        return cellPosition;
    }

    public static Vector3Int GetObjectCellPosition(GameObject obj, GridLayout gridLayout) {
        Vector3 objectPos = obj.transform.position;
        Vector3Int cellCoordinate = gridLayout.WorldToCell(objectPos);
        return cellCoordinate;
    }

    /*TileBase[] allTiles = truckInteractiveTilemap.GetTilesBlock(initialArea);
            Vector3Int[] tilesPositions = truckInteractiveTilemap.cellBounds.allPositionsWithin;
            TruckTilesList = new List<Vector3Int>(allTiles);

            for (int x = 0; x < initialArea.size.x; x++) {
                for (int y = 0; y < initialArea.size.y; y++) {
                    int index = x + y * initialArea.size.x;
                    TileBase tile = TruckTilesList[index];
                    if (tile == emptyTile) {
                        
                    }
                }
            }*/
}

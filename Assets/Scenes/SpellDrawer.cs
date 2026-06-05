using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SpellDrawer : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Line Settings")]
    public GameObject linePrefab; // Buat prefab dari GameObject LineRenderer-mu
    public RectTransform drawArea;
    
    private LineRenderer currentLine;
    private List<Vector2> currentPoints = new List<Vector2>();
    
    // Simpan semua garis jika nanti ingin di-parse
    public List<LineRenderer> allStrokes = new List<LineRenderer>();

    public void OnPointerDown(PointerEventData eventData)
    {
        // 1. Buat garis baru setiap kali klik
        CreateNewLine();
        AddPoint(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        AddPoint(eventData);
    }

    private void CreateNewLine()
    {
        GameObject newLineObj = Instantiate(linePrefab, drawArea);
        currentLine = newLineObj.GetComponent<LineRenderer>();
        currentPoints.Clear();
        allStrokes.Add(currentLine);
    }

    private void AddPoint(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(drawArea, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
        {
            // Set Z ke 0 relatif terhadap Canvas
            worldPoint.z = drawArea.position.z - 1f;; 
            
            // Hindari titik duplikat jika mouse tidak bergerak jauh
            if (currentPoints.Count == 0 || Vector2.Distance(currentPoints[currentPoints.Count - 1], worldPoint) > 0.1f)
            {
                currentPoints.Add(worldPoint);
                currentLine.positionCount = currentPoints.Count;
                currentLine.SetPosition(currentPoints.Count - 1, worldPoint);
            }
        }
    }
}
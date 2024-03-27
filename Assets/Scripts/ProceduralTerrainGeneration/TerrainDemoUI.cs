using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TerrainDemoUI : MonoBehaviour
{
    [SerializeField] private Slider perlinStretch, heightMultiplier, maxSlope, pathWidth;
    [SerializeField] private Button randomizeSeed;
    [SerializeField] private TerrainEditor terrainEditor;
    Vector2 rotation = Vector2.zero;
    const string xAxis = "Mouse X"; 
    const string yAxis = "Mouse Y";
    [Range(0.1f, 9f)][SerializeField] float sensitivity = 2f;
    [Tooltip("Limits vertical camera rotation. Prevents the flipping that happens when rotation goes above 90.")]
    [Range(0f, 90f)][SerializeField] float yRotationLimit = 88f;
    private void Start()
    {
        perlinStretch.value = terrainEditor.GetPerlinStretch();
        heightMultiplier.value = terrainEditor.GetHeightMultiplier();
        maxSlope.value = terrainEditor.GetMaxSlope();
        pathWidth.value = terrainEditor.GetPathWidth();
        randomizeSeed.onClick.AddListener(() => terrainEditor.RandomizeSeed());
    }
    private void Update()
    {
        // If user is holding mouse2, rotate the main camera
        if (Input.GetMouseButton(1))
        {
            // Take the current camera rotation and add the mouse movement to it
            rotation.x = Camera.main.transform.eulerAngles.y + Input.GetAxis(xAxis) * sensitivity;
            rotation.y = Camera.main.transform.eulerAngles.x - Input.GetAxis(yAxis) * sensitivity;
            // Clamp the vertical rotation to prevent flipping
            rotation.y = Mathf.Clamp(rotation.y, -yRotationLimit, yRotationLimit);
            // Apply the new rotation to the camera
            Camera.main.transform.rotation = Quaternion.Euler(rotation.y, rotation.x, 0);
        }
        // Move camera with WASD
        float moveSpeed = 100;
        if (Input.GetKey(KeyCode.LeftShift))
            moveSpeed *= 2;
        if (Input.GetKey(KeyCode.W))
            Camera.main.transform.position += Camera.main.transform.forward * moveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S))
            Camera.main.transform.position -= Camera.main.transform.forward * moveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.A))
            Camera.main.transform.position -= Camera.main.transform.right * moveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D))
            Camera.main.transform.position += Camera.main.transform.right * moveSpeed * Time.deltaTime;

        // Move up/down with Q/E
        if (Input.GetKey(KeyCode.Q))
            Camera.main.transform.position -= Vector3.up * moveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E))
            Camera.main.transform.position += Vector3.up * moveSpeed * Time.deltaTime;
    }
    public void SetPerlinStretch()
    {
        terrainEditor.SetPerlinStretch(perlinStretch.value);
    }
    public void SetHeightMultiplier()
    {
        terrainEditor.SetHeightMultiplier(heightMultiplier.value);
    }
    public void SetMaxSlope()
    {
        terrainEditor.SetMaxSlope(maxSlope.value);
    }
    public void SetPathWidth()
    {
        terrainEditor.SetPathWidth((int)pathWidth.value);
    }
}

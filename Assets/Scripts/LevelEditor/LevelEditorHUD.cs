﻿using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ObjectTypeTab
{
    Prefab, Required
}

public class LevelEditorHUD : MonoBehaviour
{
    [Header("Set In Editor")]
    public Button prefabViewToggleButton;
    public Button requiredViewToggleButton;

    public GameObject prefabScrollView;
    public Transform prefabScrollViewContent;
    public LevelPrefabContainer levelPrefabContainer;
    public GameObject levelButtonPrefab;

    public Button playButton;
    public Button saveButton;

    public LayerMask gizmoLayerMask;
    public LayerMask selectionLayerMask;
    public Material outlineMaterial;

    public GameObject playerPrefab;

    [Header("Set at Runtime")]
    public GameObject playerInstance;
    public GizmoColor currentGizmoColor;
    public bool isUsingGizmo = false;
    public GameObject currentSelectedObject;

    public Inspector inspector;
    public LevelEditorGizmo levelEditorGizmo;
    public Camera levelEditorCamera;

    private List<LevelEditorPrefabButton> prefabButtons;


    private bool isWorkshopLevel;

    private void Awake()
    {
        levelEditorCamera = GetComponentInParent<Camera>();
        levelEditorGizmo = GetComponent<LevelEditorGizmo>();
        inspector = GetComponentInChildren<Inspector>();

        playerInstance = Instantiate(playerPrefab);
        playerInstance.SetActive(false);
    }

    void Start()
    {
        prefabButtons = new List<LevelEditorPrefabButton>();

        prefabViewToggleButton.onClick.RemoveAllListeners();
        prefabViewToggleButton.onClick.AddListener(() => TogglePrefabMenu(ObjectTypeTab.Prefab));

        requiredViewToggleButton.onClick.RemoveAllListeners();
        requiredViewToggleButton.onClick.AddListener(() => TogglePrefabMenu(ObjectTypeTab.Required));

        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(() => PlayTest());

        saveButton.onClick.RemoveAllListeners();
        saveButton.onClick.AddListener(() => Save());

        prefabScrollView.SetActive(false);
        PopulatePrefabMenu();
    }

    void Update()
    {
        // If we are loading into a workshop level, we don't want the HUD to do anything
        if (isWorkshopLevel)
        {
            return;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (Input.GetMouseButtonDown(0))
        {
            // Break out if we clicked on the UI, prevents clearing the object when clicking on UI
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Ray ray = levelEditorCamera.ScreenPointToRay(Input.mousePosition);

            if(Physics.Raycast(ray, out RaycastHit gizmoHit, 1000, gizmoLayerMask))
            {
                isUsingGizmo = true;
                GizmoType tempType = gizmoHit.collider.gameObject.GetComponent<GizmoType>();
                if(tempType == null)
                {
                    return;
                }
                currentGizmoColor = tempType.gizmoColor;
                return; // break out so that we dont also select an object
            }
            if (Physics.Raycast(ray, out RaycastHit hit, 1000, selectionLayerMask))
            {
                SelectObject(hit.collider.gameObject);
            }
            else
            {
                UnselectCurrentObject();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isUsingGizmo = false;
            levelEditorGizmo.lastMousePosition = Vector3.zero;
        }
    }

    private void SelectObject(GameObject objectToSelect)
    {
        if (!objectToSelect.Equals(currentSelectedObject))
        {
            UnselectCurrentObject();
        }
        currentSelectedObject = objectToSelect;
        inspector.InspectObject(currentSelectedObject.transform);
        Renderer renderer = currentSelectedObject.GetComponentInChildren<Renderer>();
        List<Material> currentMaterials = renderer.sharedMaterials.ToList();
        if (!currentMaterials.Contains(outlineMaterial))
        {
            currentMaterials.Insert(0, outlineMaterial);
        }
        renderer.sharedMaterials = currentMaterials.ToArray();
    }

    private void UnselectCurrentObject()
    {
        if (currentSelectedObject == null) {
            return;
        }

        // Add outline material
        Renderer renderer = currentSelectedObject.GetComponentInChildren<Renderer>();
        List<Material> currentMaterials = renderer.sharedMaterials.ToList();
        for (int i = currentMaterials.Count - 1; i >= 0; i--)
        {
            if (currentMaterials[i].name == "outline")
            {
                currentMaterials.RemoveAt(i);
            }
        }
        renderer.sharedMaterials = currentMaterials.ToArray();

        currentSelectedObject = null;
        inspector.Clear();
    }

    private void PlayTest()
    {
        playerInstance.transform.position = transform.parent.position;
        playerInstance.SetActive(!playerInstance.activeInHierarchy);
    }

    #region Prefab Menu
    private void TogglePrefabMenu(ObjectTypeTab tab)
    {
        prefabScrollView.SetActive(true);

        foreach(LevelEditorPrefabButton button in prefabButtons)
        {
            switch (tab)
            {
                case ObjectTypeTab.Prefab:
                    button.gameObject.SetActive(button.tab == ObjectTypeTab.Prefab);
                    break;
                case ObjectTypeTab.Required:
                    button.gameObject.SetActive(button.tab == ObjectTypeTab.Required);
                    break;
            }
        }
    }

    private void PopulatePrefabMenu()
    {
        foreach(LevelPrefab levelPrefab in levelPrefabContainer.levelPrefabs)
        {
            GameObject levelButton = Instantiate(levelButtonPrefab, prefabScrollViewContent);
            LevelEditorPrefabButton newPrefabButton = levelButton.GetComponent<LevelEditorPrefabButton>();
            newPrefabButton.button.onClick.RemoveAllListeners();
            newPrefabButton.button.onClick.AddListener(() => PrefabButtonClicked(levelPrefab));
            newPrefabButton.image.sprite = levelPrefab.previewImage;
            newPrefabButton.tab = levelPrefab.isRequired ? ObjectTypeTab.Required : ObjectTypeTab.Prefab;
            newPrefabButton.text.text = levelPrefab.objectName;
            prefabButtons.Add(newPrefabButton);
        }
    }

    private void PrefabButtonClicked(LevelPrefab levelPrefab)
    {
        GameObject newObject = Instantiate(levelPrefab.prefab);
        // Set the object 10 units in front of the camera
        newObject.transform.position = levelEditorCamera.transform.position + (levelEditorCamera.transform.forward * 10);
        SelectObject(newObject);
        Save();
    }
    #endregion

    #region Workshop Level
    private void Save()
    {
        // Don't save if we don't have a level data object to save to
        // This happens when opening the scene manually
        if (GameManager.Instance != null)
        {
            LevelEditorLevel newLevel = new LevelEditorLevel();
            LevelEditorObject[] sceneObjects = FindObjectsOfType<LevelEditorObject>();
            foreach (LevelEditorObject sceneObject in sceneObjects)
            {
                newLevel.levelObjects.Add(sceneObject.GetObjectData());
            }

            string jsonData = JsonUtility.ToJson(newLevel, true);

        
            string filePath = GameManager.GetCurrentLevel().levelEditorScenePath;
            Debug.Log($"Saving level {GameManager.GetCurrentLevel().levelName} to {filePath}");
            File.WriteAllText(filePath, jsonData);
        }
    }

    public void LoadSceneData()
    {
        Level currentLevel = GameManager.GetCurrentLevel();

        string filePath = "";
        if(currentLevel.workshopFilePath != string.Empty && currentLevel.workshopFilePath != null)
        {
            DirectoryInfo fileInfo = new DirectoryInfo(currentLevel.workshopFilePath);
            string scenePath = fileInfo.EnumerateFiles().First().FullName;
            filePath = scenePath;
            SetupForWorkshopLevel();
        }
        else
        {
            if (currentLevel.levelEditorScenePath == string.Empty)
            {
                Debug.Log($"Trying to load level: {currentLevel.levelName} but it has not been saved");
                return;
            }


            if (!File.Exists(currentLevel.levelEditorScenePath))
            {
                Debug.Log($"Trying to load level: {currentLevel.levelName} from {currentLevel.levelEditorScenePath} but the save file has been deleted");
                return;
            }
            filePath = currentLevel.levelEditorScenePath;
        }

        string jsonData = File.ReadAllText(filePath);
        LevelEditorLevel levelToLoad = JsonUtility.FromJson<LevelEditorLevel>(jsonData);

        CreateObjects(levelToLoad.levelObjects);
    }

    private void CreateObjects(List<ObjectData> allObjects)
    {
        foreach (ObjectData objectData in allObjects)
        {
            LevelPrefab prefabOfType = levelPrefabContainer.levelPrefabs.ToList().Where(x => x.objectType == objectData.objectType).FirstOrDefault();
            if (prefabOfType == null)
            {
                return;
            }

            GameObject newObject = Instantiate(prefabOfType.prefab);

            //if (prefabOfType.objectType == ObjectType.Checkpoint)
            //{
            //    Checkpoint temp = newObject.GetComponent<Checkpoint>();
            //    if (temp != null)
            //    {
            //        temp.level = objectData.checkpointNumber;
            //    }
            //}


            newObject.transform.position = objectData.position;
            newObject.transform.rotation = Quaternion.Euler(objectData.rotation);
            newObject.transform.localScale = objectData.scale;
        }
    }

    private void SetupForWorkshopLevel()
    {
        isWorkshopLevel = true;
        playButton.gameObject.SetActive(false);
        saveButton.gameObject.SetActive(false);
        prefabViewToggleButton.gameObject.SetActive(false);
        playerInstance.transform.position = PlayerConstants.PlayerSpawnOffset;
        playerInstance.SetActive(true);
    }
    #endregion
}

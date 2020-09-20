﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelEditorHUD : MonoBehaviour
{
    public Button prefabViewToggleButton;
    public GameObject prefabScrollView;
    public Transform prefabScrollViewContent;
    public LevelPrefabContainer levelPrefabContainer;
    public GameObject levelButtonPrefab;
    public GameObject selectedObjectGizmo;

    public Inspector inspector;

    public Transform camera;
    public GameObject currentSelectedObject;
    public LayerMask selectionLayerMask;

    void Start()
    {
        camera = transform.parent;
        prefabViewToggleButton.onClick.RemoveAllListeners();
        prefabViewToggleButton.onClick.AddListener(() => TogglePrefabMenu());

        prefabScrollView.SetActive(false);
        selectedObjectGizmo.SetActive(false);
        inspector.gameObject.SetActive(false);
        PopulatePrefabMenu();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            if(Physics.Raycast(camera.position, camera.forward, out hit, 1000, selectionLayerMask))
            {
                currentSelectedObject = hit.collider.gameObject;
                inspector.InspectObject(currentSelectedObject.transform);
            }
            else
            {
                currentSelectedObject = null;
            }
        }

        ShowTransformGizmo();
        ShowInspector();
        
    }

    private void ShowTransformGizmo()
    {
        if(currentSelectedObject == null)
        {
            selectedObjectGizmo.SetActive(false);
            return;
        }
        selectedObjectGizmo.SetActive(true);
        selectedObjectGizmo.transform.position = currentSelectedObject.transform.position;
    }

    private void ShowInspector()
    {
        if (currentSelectedObject == null)
        {
            inspector.gameObject.SetActive(false);
        }
        else
        {
            inspector.gameObject.SetActive(true);
        }
    }

    private void TogglePrefabMenu()
    {
        prefabScrollView.SetActive(!prefabScrollView.activeInHierarchy);
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
        }
    }

    private void PrefabButtonClicked(LevelPrefab levelPrefab)
    {
        GameObject newObject = Instantiate(levelPrefab.prefab);
        newObject.transform.position = camera.position + (camera.forward * 10);
        currentSelectedObject = newObject;
        inspector.InspectObject(currentSelectedObject.transform);
    }
}

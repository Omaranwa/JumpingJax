﻿using UnityEditor;
using UnityEngine;

public class DeletePlayerPrefs
{    
    [MenuItem(itemName: "Tools/CaosCreations/Delete PlayerPrefs")]
    private static void DeletePrefs()
    {
        PlayerPrefs.DeleteAll();
    }
}

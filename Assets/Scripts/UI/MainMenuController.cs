using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // Assuming you are using TextMeshPro for text

public class MainMenuController : MonoBehaviour
{
    [Header("Level Select Setup")]
    public Transform levelGridContainer;    // Drag your LevelSelectionPanel here
    public GameObject levelButtonPrefab;    // Drag your LevelButtonPrefab here
    
    // In a real game, this might come from a SaveData file or ScriptableObjects
    public List<string> levelNames = new List<string> { "Level 1", "Level 2", "Level 3", "Boss Level" };

    [Header("Settings Setup")]
    public TMP_Dropdown qualityDropdown;    // Drag your Quality Dropdown here
    public Button exitButton;               // Drag your Exit Button here

    void Start()
    {
        PopulateLevelGrid();
        SetupQualitySettings();
        
        // Bind the exit button
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
    }

    private void PopulateLevelGrid()
    {
        // Clear any placeholder buttons left in the grid by the designer
        foreach (Transform child in levelGridContainer)
        {
            Destroy(child.gameObject);
        }

        // Generate the real buttons
        for (int i = 0; i < levelNames.Count; i++)
        {
            int levelIndex = i; // CRITICAL: Cache the index for the lambda closure!
            string levelName = levelNames[i];

            // Spawn the prefab inside the grid
            GameObject newButtonObj = Instantiate(levelButtonPrefab, levelGridContainer);
            
            // Set the text
            TMP_Text buttonText = newButtonObj.GetComponentInChildren<TMP_Text>();
            if (buttonText != null) buttonText.text = levelName;

            // Bind the click event dynamically
            Button buttonComponent = newButtonObj.GetComponent<Button>();
            buttonComponent.onClick.AddListener(() => LoadLevel(levelName));
        }
    }

    private void LoadLevel(string sceneName)
    {
        Debug.Log($"Saving selected level: {sceneName}");
        
        // 1. Save the data to our static bridge
        GameSession.SelectedLevelName = sceneName;

        // 2. Load the actual gameplay scene
        SceneManager.LoadScene("SampleScene"); 
    }

    private void SetupQualitySettings()
    {
        if (qualityDropdown == null) return;

        qualityDropdown.ClearOptions();

        // Dynamically pull the quality levels defined in Unity (Project Settings -> Quality)
        string[] qualityNames = QualitySettings.names;
        List<string> options = new List<string>(qualityNames);
        
        qualityDropdown.AddOptions(options);
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();

        // Bind the dropdown change event
        qualityDropdown.onValueChanged.AddListener(ChangeQualityLevel);
    }

    private void ChangeQualityLevel(int index)
    {
        QualitySettings.SetQualityLevel(index, true);
        Debug.Log($"Quality set to: {QualitySettings.names[index]}");
    }

    private void ExitGame()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();
        
        // Note: Application.Quit() doesn't do anything in the Unity Editor, 
        // so we add this line to stop the editor playback when testing.
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
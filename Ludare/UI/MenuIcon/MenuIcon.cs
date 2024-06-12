using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


public class MenuIcon : MonoBehaviour
{
    public UIDocument mainMenuDoc;

    public GameObject platformSelectPrefab;

    public GameObject ludareOnlyPrefab;

    void Start()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = mainMenuDoc.rootVisualElement;

        Button testButton = root.Query<Button>("MenuButton");
        testButton.RegisterCallback<ClickEvent>(OnButtonClicked);

    }

    void OnButtonClicked(ClickEvent Clicked)
    {
        GameObject spawnedPlatformSelect;
#if LUDARE_USE_LUDARE_LOGIN
        spawnedPlatformSelect = Instantiate(ludareOnlyPrefab, new Vector3(0, 0, 0), Quaternion.identity);
#else
        spawnedPlatformSelect = Instantiate(platformSelectPrefab, new Vector3(0, 0, 0), Quaternion.identity);
#endif
        spawnedPlatformSelect.transform.SetParent(this.transform.parent);
    }
}
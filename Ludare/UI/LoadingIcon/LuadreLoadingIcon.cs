using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class LuadreLoadingIcon : MonoBehaviour
{
    [SerializeField]
    public UIDocument menuDoc;

    [SerializeField]
    private VisualElement loadingIcon;

    public float rotationSpeed = 10;

    public void Start()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = menuDoc.rootVisualElement;

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        loadingIcon = root.Query<VisualElement>("LoadingIcon");
    }

    public void Update()
    {
        Vector3 addRotation = new Vector3(0, 0, 1) * Time.deltaTime * rotationSpeed;

        ITransform trans = loadingIcon.transform;
        Quaternion Q = trans.rotation;
        Q.eulerAngles = Q.eulerAngles + addRotation;
        trans.rotation = Q;

        //trans.eulerAngles = trans.eulerAngles + addRotation;
    }
}

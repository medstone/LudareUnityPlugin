using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using UnityEngine.Events;

public class SignInScreen : MonoBehaviour
{
    [SerializeField]
    private UIDocument signInDoc;

    private TextField passwordField;

    private TextField usernameField;

    public Label message;

    public LoginCloseEvent onLoginClose;

    public void Start()
    {
        onLoginClose = new LoginCloseEvent();

        // Each editor window contains a root VisualElement object
        VisualElement root = signInDoc.rootVisualElement;

        Button submitSelect = root.Query<Button>("Submit");
        Button exit = root.Query<Button>("ExitButton");
        usernameField = root.Query<TextField>("UsernameField");
        passwordField = root.Query<TextField>("PasswordField");
        message = root.Query<Label>("StatusText");

        submitSelect.RegisterCallback<ClickEvent>(OnSubmitSelected);
        exit.RegisterCallback<ClickEvent>(OnExitSelected);
    }

    void OnSubmitSelected(ClickEvent Clicked)
    {
        usernameField.focusable = false;

        LudareManager.onLogin.AddListener(OnLoginCallback);
        LudareManager.single.TryLoginLudare(usernameField.value, passwordField.value);
    }

    void OnExitSelected(ClickEvent Clicked)
    {
        Destroy(this.gameObject);
    }

    void OnLoginCallback(bool Success)
    {

        if (Success == true)
        {
            message.text = "Ludare Logged In Successfully";
            StartCoroutine(CloseAfterWait());
            message.style.color = new StyleColor(Color.green);
        }
        else
        {
            message.text = "Ludare Failed to Login";
            message.style.color = new StyleColor(Color.red);
        }
    }

    IEnumerator CloseAfterWait()
    {
        yield return new WaitForSeconds(5);

        onLoginClose.Invoke();

        Destroy(gameObject);

        yield return null;
    }
}
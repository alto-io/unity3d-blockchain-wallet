using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PasswordInputField : MonoBehaviour {

    public InputField confirmPasswordInputField;

    private bool m_passwordConfirmed = false;

    public bool passwordConfirmed()
    {
        return m_passwordConfirmed;
    }

    public string passwordString()
    {
        return gameObject.GetComponent<InputField>().text;
    }
}

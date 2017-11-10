using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PasswordInputField : MonoBehaviour {

    public InputField confirmPasswordInputField;
    public Button createWalletButton;

    private bool m_passwordConfirmed = false;

    public bool passwordConfirmed()
    {
        return m_passwordConfirmed;
    }

    public void resetFields()
    {
        gameObject.GetComponent<InputField>().text = "";
        confirmPasswordInputField.text = "";
    }

    public string passwordString()
    {
        return gameObject.GetComponent<InputField>().text;
    }

    public void validatePasswordConfirmation()
    {
        m_passwordConfirmed = passwordString().Equals(confirmPasswordInputField.text);
        createWalletButton.interactable = m_passwordConfirmed;
    }
}

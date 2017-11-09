using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogText : MonoBehaviour {

    public void Log(params string[] strings)
    {
        Text m_logText = gameObject.GetComponent<Text>();

        foreach (string s in strings)
        {
            m_logText.text += "\n" + s;
        }

    }
}

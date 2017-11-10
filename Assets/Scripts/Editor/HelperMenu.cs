using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class HelperMenu : MonoBehaviour
{
    [MenuItem("blockchain-wallet/Delete Wallet Data")]
    static void DeleteWalletData()
    {
        List<string> saveLocations = new List<string>
        {
            "walletcache.data"
        };

        if (EditorUtility.DisplayDialog("Delete Wallet Data?", "Are you sure? This cannot be reverted.", "Yes", "Cancel"))
        {
            foreach (string filename in saveLocations)
            {
                string s = Application.persistentDataPath + "/" + filename;
                if (File.Exists(s))
                {
                    File.Delete(s);
                    Debug.Log(s + " deleted");
                }
            }
        }
    }
}
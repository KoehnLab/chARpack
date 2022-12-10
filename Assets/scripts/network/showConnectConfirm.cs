using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine.SceneManagement;
using UnityEngine;

public class showConnectConfirm : MonoBehaviour
{
    [HideInInspector] public GameObject connectConfirmPrefab;

    [HideInInspector] public string ip;

    public void Start()
    {
        connectConfirmPrefab = (GameObject)Resources.Load("prefabs/confirmLoadDialog");
    }

    public void triggered()
    {
        var myDialog = Dialog.Open(connectConfirmPrefab, DialogButtonType.Yes | DialogButtonType.No, "Confirm Connect", $"Are you sure you want to connect to: \n{ip}", true);
        if (myDialog != null)
        {
            myDialog.OnClosed += OnClosedDialogEvent;
        }
    }

    private void OnClosedDialogEvent(DialogResult obj)
    {
        if (obj.Result == DialogButtonType.Yes)
        {
            LoginData.ip = ip;
            LoginData.normal_mode = false;
            SceneManager.LoadScene("MainScene");
        }
    }

}

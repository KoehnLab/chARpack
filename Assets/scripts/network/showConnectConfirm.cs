using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace chARpack
{
    public class showConnectConfirm : MonoBehaviour
    {
        [HideInInspector] public GameObject connectConfirmPrefab;
        [HideInInspector] public string ip;

        public void Start()
        {
            connectConfirmPrefab = (GameObject)Resources.Load("prefabs/confirmDialog");
        }

        /// <summary>
        /// Shows a dialog window asking the user to confirm their wish to connect to the selected server.
        /// </summary>
        public void triggered()
        {
            // hide server list
            ServerList.Singleton.gameObject.SetActive(false);
            var myDialog = Dialog.Open(connectConfirmPrefab, DialogButtonType.Yes | DialogButtonType.No, "Confirm Connect", $"Are you sure you want to connect to:\n{ip}", true);
            //make sure the dialog is rotated to the camera
            myDialog.transform.forward = Camera.main.transform.forward;
            myDialog.transform.position = Camera.main.transform.position + 0.01f * myDialog.transform.forward;

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
                LoginData.singlePlayer = false;
                SceneManager.LoadScene("MainScene");
            }
            else
            {
                // show server list again
                ServerList.Singleton.gameObject.SetActive(true);
                ServerList.Singleton.scrollUpdate();
            }

        }

    }
}

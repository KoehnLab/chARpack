using UnityEngine;

namespace chARpack
{
    public class VerticalProgressBar : MonoBehaviour
    {

        public GameObject FillerGameObject;

        public void setProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            FillerGameObject.transform.localScale = new Vector3(progress, FillerGameObject.transform.localScale.y, FillerGameObject.transform.localScale.z);
        }

    }
}

using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace chARpack
{
    public class initFollowMe : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            gameObject.GetComponent<FollowMeToggle>().SetFollowMeBehavior(true);
        }

    }
}

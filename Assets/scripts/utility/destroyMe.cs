using UnityEngine;

namespace chARpack
{
    public class destroyMe : MonoBehaviour
    {
        public void destroyGameObject()
        {
            Destroy(gameObject);
        }

        public void destroyParent()
        {
            Destroy(transform.parent.gameObject);
        }
    }
}

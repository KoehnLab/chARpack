using UnityEngine;

namespace chARpack
{
    public class UserPanel : MonoBehaviour
    {
        private static UserPanel _singleton;

        public static UserPanel Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                {
                    _singleton = value;
                }
                else if (_singleton != value)
                {
                    Debug.Log($"[{nameof(UserPanel)}] Instance already exists, destroying duplicate!");
                    Destroy(value);
                }

            }
        }

        private void Awake()
        {
            Singleton = this;
        }
    }
}

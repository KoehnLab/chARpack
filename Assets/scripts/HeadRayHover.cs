using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace chARpack
{
    public class HeadRayHover : MonoBehaviour
    {

        private static HeadRayHover _singleton;

        public static HeadRayHover Singleton
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
                    Debug.Log($"[{nameof(HeadRayHover)}] Instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        private void Awake()
        {
            Singleton = this;
        }

        Camera currentCam;
        void Start()
        {
            currentCam = Camera.main;
            if (!SettingsData.hoverGazeAsSelection)
            {
                enabled = false;
            }
        }

        void Update()
        {
            if (GlobalCtrl.Singleton != null)
            {
                List<Molecule> intersect_list = new List<Molecule>();
                foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
                {
                    var bounds = mol.GetComponent<myBoundingBox>().getCopyOfBounds();
                    var intersect = chARpackUtils.getHeadRayIntersection(bounds.center, currentCam);
                    if (bounds.Contains(intersect))
                    {
                        intersect_list.Add(mol);
                    }
                }
                if (intersect_list.Count > 0)
                {
                    Molecule closest_mol;
                    if (intersect_list.Count == 1)
                    {
                        closest_mol = intersect_list.First();
                    }
                    else
                    {
                        List<float> dist_list = new List<float>();
                        foreach (var inter_mol in intersect_list)
                        {
                            dist_list.Add(Vector3.Distance(currentCam.transform.position, inter_mol.transform.position)); // maybe use box center here?
                        }
                        float minVal = dist_list.Min();
                        int index = dist_list.IndexOf(minVal);
                        closest_mol = intersect_list[index];
                    }
                    foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
                    {
                        if (closest_mol == mol)
                        {
                            mol.Hover(true);
                        }
                        else
                        {
                            mol.Hover(false);
                        }
                    }
                }
                else
                {
                    foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
                    {
                        mol.Hover(false);
                    }
                }
            }
        }
    }
}

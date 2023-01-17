using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContextMenu : MonoBehaviour
{
    private static ContextMenu instance;
    public static ContextMenu Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ContextMenu>();
            }
            return instance;
        }
    }




    public TMP_Text titleName;
    public Text bondLength;
    public bool atom;
    public bool molecule;
    public bool bond;
    public Atom selectedAtom;
    public Molecule selectedMolecule;
    public Bond selectedBond;

    public GameObject atomMenu;
    public GameObject moleculeMenu;
    public GameObject bondMenu;

    public GameObject favMenu1;
    public GameObject favMenu2;
    public GameObject favMenu3;
    public GameObject favMenu4;
    public GameObject favMenu5;

    public List<GameObject> favoritesMenuGO = new List<GameObject>(5);

    // Start is called before the first frame update
    void Start()
    {
        atom = false;
        bond = false;
        molecule = false;


        favoritesMenuGO.Add(favMenu1);
        favoritesMenuGO.Add(favMenu2);
        favoritesMenuGO.Add(favMenu3);
        favoritesMenuGO.Add(favMenu4);
        favoritesMenuGO.Add(favMenu5);
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void setAtomOption(Atom a)
    {
        titleName.text = a.m_data.m_name;

        atomMenu.SetActive(true);
        bondMenu.SetActive(false);
        moleculeMenu.SetActive(false);

        atom = true;
        bond = false;
        molecule = false;
        selectedAtom = a;
        selectedBond = null;
        selectedMolecule = null;
    }

    public void setAtomFavorite(ushort i)
    {
        if(selectedAtom.m_data.m_abbre != "Dummy")
            GlobalCtrl.Singleton.setFavorite(i, selectedAtom.m_data.m_abbre, favoritesMenuGO);
    }

    public void ChangeAtomType(int i)
    {
        GlobalCtrl.Singleton.changeAtom(selectedAtom.m_molecule.m_id, selectedAtom.m_id, GlobalCtrl.Singleton.favorites[i - 1]);
        selectedAtom.markAtom(true);
        titleName.text = selectedAtom.m_data.m_name;
    }


    public void translateHybrid(ushort hybrid)
    {
        GlobalCtrl.Singleton.modifyHybrid(selectedAtom, hybrid);
    }

    public void setMoleculeOption(Molecule m)
    {
        titleName.text = "Molecule ";

        atomMenu.SetActive(false);
        bondMenu.SetActive(false);
        moleculeMenu.SetActive(true);

        atom = false;
        bond = false;
        molecule = true;
        selectedAtom = null;
        selectedBond = null;
        selectedMolecule = m;
    }

    public void saturateMolecule()
    {
        foreach (Atom a in selectedMolecule.atomList)
        {
            if (a.m_data.m_abbre == "Dummy")
            {
                GlobalCtrl.Singleton.changeAtom(a.m_molecule.m_id, a.m_id, "H");
                a.markAtom(true);
            }
        }
    }

    public void setBondOption(Bond b)
    {
        titleName.text = "Bond";

        atomMenu.SetActive(false);
        bondMenu.SetActive(true);
        moleculeMenu.SetActive(false);

        atom = false;
        bond = true;
        molecule = false;
        selectedAtom = null;
        selectedBond = b;
        selectedMolecule = null;

        bondLength.text = b.m_bondDistance.ToString();
    }

    public void setBondOrder(float num)
    {
        selectedBond.m_bondOrder = num;
        selectedBond.transform.localScale = new Vector3(num, num, selectedBond.transform.localScale.z);
    }

    public void setBondDistance(float num)
    {
        selectedBond.m_bondDistance += num;
        bondLength.text = selectedBond.m_bondDistance.ToString();
    }

}

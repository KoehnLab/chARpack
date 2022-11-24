using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System;
using UnityEngine;
using UnityEngine.UI;
using StructClass;

public class ForceField : MonoBehaviour
{

    public struct BondTerm
    {
        public int Atom1; public int Atom2; public float kBond; public float Req;
    }
    public List<BondTerm> bondList = new List<BondTerm>();

    struct AngleTerm
    {
        public int Atom1; public int Atom2; public int Atom3; public float kAngle; public float Aeq;
    }
    List<AngleTerm> angleList = new List<AngleTerm>();

    struct TorsionTerm
    {
        public int Atom1; public int Atom2; public int Atom3; public int Atom4; public float vk; public float phieq; public int nn;
    }
    List<TorsionTerm> torsionList = new List<TorsionTerm>();

    public struct HardSphereTerm
    {
        public int Atom1; public int Atom2; public float kH; public float Rcrit;
    }
    public List<HardSphereTerm> hsList = new List<HardSphereTerm>();

    List<int> atomList = new List<int>();
    List<int> atomListO;
    List<string> atomType = new List<string>();
    List<string> atomTypeO;
    List<int> atomHybridization = new List<int>();
    List<int> atomHybridizationO;
    List<int> nBondP = new List<int>(); //number of bonding partners
    List<float> atomMass = new List<float>();
    List<Vector3> position = new List<Vector3>();
    List<Vector3> forces = new List<Vector3>();
    public List<Vector3> movement = new List<Vector3>();
    int nAtoms;
    private int nDummies;
    private int nBond, nAngle, nnoBond, nTorsion;
    bool torsionActive = true;

    //float scalingFactor = 154 / (154 / GetComponent<GlobalCtrl>().scale); // with this, 154 pm are equivalent to 0.35 m in the model
    // note that the forcefield works in the atomic scale (i.e. all distances measure in pm)
    // we scale back when applying the movements to the actual objects
    float scalingfactor;
    int nTimeSteps = 5;  // number of time steps per FixedUpdate() for numerical integration of ODE
    float timeFactor; // timeFactor = totalTimePerFrame/nTimeSteps ... set in Start()
    /*
    const float k0 = 100f;         //between 100 - 5000
    const float kb = k0;           //bond force constant
    const float ka = kb*1430f;     //angle force constant
    const float kim = kb*70f;      //improper torsion force constant
    */

    const float k0 = 1000f;              //between 100 - 5000
    const float ka = k0;                //angle force constant
    const float kb = 7f * k0 / 10000f;    //bond force constant, "/ 10000f" because of caculating A^2 to pm^2
    const float kim = 0.02f * k0;         //improper torsion force constant 0.45 ; 0.045
   
    //float standardDistance = 154f; // integrate into new bondList
    const float alphaNull = 109.4712f; // integrate into new angleList
    // constants for hard-sphere terms, ca. 90% of van-der-Waals radius  .... have to set them even smaller
    const float fac = 0.9f;

    Dictionary<string, float> rhs = new Dictionary<string, float>();
    Dictionary<string, float[]> DREIDINGConst = new Dictionary<string, float[]> {
        { "Dummy_0", new[] {33f,180f} },
        { "H_0", new[] {33f,180f} },
        { "B_3", new[] {88f,109.471f} },
        { "B_2", new[] {79f,120f} },
        { "C_4", new[] {70f,120f} },
        { "C_3", new[] {77f,109.471f} },
        { "C_2", new[] {67f,120f} },
        { "C_1", new[] {60.2f,120f} },
        { "N_4", new[] {65f,120f} },
        { "N_3", new[] {70.2f,106.7f} },
        { "N_2", new[] {61.5f,120f} },
        { "N_1", new[] {55.6f,120f} },
        { "O_4", new[] {66f,120f} },
        { "O_3", new[] {66f,104.51f} },
        { "O_2", new[] {56f,120f} },
        { "O_1", new[] {52.8f,180f} },
        { "F_0", new[] {61.1f,180f} },
        { "Al_3", new[] {104.7f,109.471f} },
        { "Si_3", new[] {93.7f,109.471f} },
        { "P_3", new[] {89f,93.3f} },
        { "S_3", new[] {104.0f,92.1f} },
        { "Cl_0", new[] {99.7f,180f} },
        { "Ga_3", new[] {121.0f,109.471f} },
        { "Ge_3", new[] {121.0f,109.471f} },
        { "As_3", new[] {121.0f,92.1f} },
        { "Se_3", new[] {121.0f,90.6f} },
        { "Br_0", new[] {116.7f,180f} },
        { "In_3", new[] {139.0f,109.471f} },
        { "Sn_3", new[] {137.3f,109.471f} },
        { "Sb_3", new[] {143.2f,91.6f} },
        { "Te_3", new[] {128.0f,90.3f} },
        { "I_1", new[] {136f,180f} },
        { "Na_1", new[] {186f,90f} },
        { "Ca_1", new[] {194f,90f} },
        { "Fe_1", new[] {128.5f,90f} },
        { "Zn_1", new[] {133f, 109.471f } }
    };
    
    
    int frame = 0;  // counter for frames (debug only)

    // for Debugging; level = 100 only input coords + output movements
    //                level = 1000 more details on forces
    //                level = 10000 maximum detail level
    StreamWriter FFlog;
    const int LogLevel = 0;


    // Start is called before the first frame update
    void Start()
    {
        if (LogLevel > 0)
        {
            //ForceFieldConsole.Instance.statusOut(string.Format("WARNING: Debug logging active (I/O intensive), level = {0}",LogLevel));
            FFlog = File.CreateText("logfile.txt");
            FFlog.WriteLine("ForceField logfile");
            FFlog.WriteLine("Log starts at " + Time.time.ToString("f6"));
            FFlog.WriteLine("LogLevel = " + LogLevel);
        }
        ;
        //scalingfactor = GetComponent<GlobalCtrl>().scale / 154f;
        //conversion factor from atomic model to unity
        //scalingfactor = GetComponent<GlobalCtrl>().scale / GetComponent<GlobalCtrl>().u2pm;
        scalingfactor = GlobalCtrl.Instance.scale / GlobalCtrl.Instance.u2pm;
        timeFactor = (1.5f / (float)nTimeSteps);

        rhs.Clear();
        foreach (KeyValuePair<string, ElementData> pair in GlobalCtrl.Instance.Dic_ElementData)
        {
            rhs.Add(pair.Key, pair.Value.m_radius * fac);
        }

    }

    void OnApplicationQuit()
    {
        if (LogLevel > 0)
        {
            FFlog.WriteLine("Log ends at " + Time.time.ToString("f6"));
            FFlog.Close();
        }
    }

    // Update is called once per frame

    void FixedUpdate()
    {
        frame += 1;
        if (LogLevel >= 100 && GlobalCtrl.Instance.forceField)
        {
            FFlog.WriteLine("Current frame: " + frame);
            FFlog.WriteLine("Current time:  " + Time.time.ToString("f6") + "  Delta: " + Time.deltaTime.ToString("f6"));
        }
        // If the forcefield is active, update all connections and forces, else only update connections
        if (GlobalCtrl.Instance.forceField)
        {

            // generate the basic lists and check if atoms have been added/removed/changed
            bool changed = generateLists();

            //Debug.Log(string.Format("changed: {0}",changed));


            // if so, regenerate the force field
            if (changed) generateFF();

            applyFF();
            scaleConnections();
        }
        else
        {
            scaleConnections();
        }

    }

    /*
     * generation / update of the connection and angle lists.
     * Will be extended later with torsions and impropers etc.
     * maybe speed up update with "what is new"
     */
    bool generateLists()
    {
        // save previous lists for check
        atomListO = new List<int>(atomList);
        atomTypeO = new List<string>(atomType);
        atomHybridizationO = new List<int>(atomHybridization);

        bool listOfAtomsChanged = false;

        // init lists
        atomList.Clear();
        atomMass.Clear();
        atomType.Clear();
        atomHybridization.Clear();
        movement.Clear();
        forces.Clear();
        position.Clear();
        nAtoms = 0;
        nDummies = 0; // for statistics

        // Maybe use Dictionary of current Molecules in scene, each Molecule includes a list of all its atoms and all its bonds
        // Acces to Dictionary like 2 lines below; Dictionary accessible via GlobalCtrl.Instance.Dic_curMolecules
        // cycle Atoms
        foreach (Atom At in GlobalCtrl.Instance.List_curAtoms)
        //foreach (KeyValuePair<int, Atom> At in GlobalCtrl.Instance.Dic_curAtoms)
        {
            // NUllcheck
            if (At != null)
            {
                nAtoms++;
                atomList.Add(At.m_idInScene);

                if (At.isGrabbed)
                {
                    atomMass.Add(-1f);
                }
                else
                {
                    atomMass.Add(At.m_data.m_mass);
                }
                atomType.Add(At.m_data.m_abbre);
                atomHybridization.Add(At.m_data.m_hybridization);
                if (At.m_data.m_abbre == "Dummy") nDummies++;
                // Get atoms and convert from unity to atomic scale
                //            position.Add((At.transform.localPosition * (1f / scalingfactor)));
                // it has to be position, not localPosition, to get consistent interactions between different fragments
                position.Add(At.transform.position * (1f / scalingfactor));
                forces.Add(new Vector3(0.0f, 0.0f, 0.0f));
                movement.Add(new Vector3(0.0f, 0.0f, 0.0f));
            }
        }
        // TODO: when FF is not generated in each frame, we have to check that the atomList matches!
        if (LogLevel >= 100)
        {
            FFlog.WriteLine("Current positions:");
            for (int iAtom = 0; iAtom < nAtoms; iAtom++)
            {
                FFlog.WriteLine(string.Format(" {0,4:d}  -  {1,5:d}   {2,14:f6} {3,14:f6}  {4,14:f6}",
                    iAtom, atomList[iAtom],
                    position[iAtom].x, position[iAtom].y, position[iAtom].z));
            }
        }

        // compare to old List to see whether there was a change:
        if (atomList.Count == atomListO.Count && atomType.Count == atomTypeO.Count)
        {
            for (int iAtom = 0; iAtom < atomList.Count; iAtom++)
            {
                if (atomList[iAtom] != atomListO[iAtom] || atomType[iAtom] != atomTypeO[iAtom])
                {
                    listOfAtomsChanged = true;
                    break;
                }
                else if (atomHybridization[iAtom] != atomHybridizationO[iAtom])   //could be that this if has to be a level higher in future
                {
                    listOfAtomsChanged = true;
                    break;
                }
                
            }


        } else
        {
            listOfAtomsChanged = true;
        }

        atomListO.Clear();
        atomTypeO.Clear();
        atomHybridizationO.Clear();

        return (listOfAtomsChanged);
    }


    void generateFF()
    {
        nBond = 0;
        nAngle = 0;
        nnoBond = 0;
        nTorsion = 0;

        bondList.Clear();
        angleList.Clear();
        hsList.Clear();
        torsionList.Clear();

        // set topology array       
        bool[,] topo = new bool[nAtoms, nAtoms];
        for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        {
            for (int jAtom = 0; jAtom < nAtoms; jAtom++)
            {
                topo[iAtom, jAtom] = false;
            }
        }

        {
            int iAtom = 0;
            foreach (Atom At1 in GlobalCtrl.Instance.List_curAtoms)
            //foreach (KeyValuePair<int, Atom> At1 in GlobalCtrl.Instance.Dic_curAtoms)
            {
                if (At1 != null)
                { 
                    // cycle through connection points
                    // ConnectionStatus does not exist anymore, instead use Atom.connectedAtoms(); this returns a List of all directly connected Atoms
                    foreach (Atom conAtom in At1.connectedAtoms(At1))
                    {
                        // get current atom index by comparison to entries in atomList
                        int jAtom = -1;
                        for (int kAtom = 0; kAtom < nAtoms; kAtom++)
                        {
                            if (atomList[kAtom] == conAtom.m_idInScene)
                            {
                                jAtom = kAtom;
                                break;
                            }
                        }
                        if (jAtom >= 0)
                        {
                            topo[iAtom, jAtom] = true;
                            topo[jAtom, iAtom] = true;
                        }
                    }
                    iAtom++;
                    
                }
            }
        }

        nBondP.Clear();
        for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        {
            int nBondingPartner = 0;
            for (int jAtom = 0; jAtom < nAtoms; jAtom++)
            {
                if (topo[iAtom, jAtom]) nBondingPartner++;
            }
            nBondP.Add(nBondingPartner);    
        }
        //String hallo = "";
        //for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        //{
        //    hallo += String.Format("{0} ",nBondP[iAtom] );
        //}
        //    Debug.Log(String.Format("nBondingPartner: {0} ", hallo));


        // now set all FF terms
        // pairwise terms, run over unique atom pairs
        for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        {
            //print("At1.m_nBondP, bonding partner count:" + GlobalCtrl.Instance.List_curAtoms[iAtom].m_nBondP);
            for (int jAtom = 0; jAtom < iAtom; jAtom++)
            {
                if (topo[iAtom, jAtom])
                {
                    BondTerm newBond = new BondTerm();
                    newBond.Atom1 = jAtom;
                    newBond.Atom2 = iAtom;
                   
                    string key1 = string.Format("{0}_{1}", atomType[jAtom], atomHybridization[jAtom]);
                    string key2 = string.Format("{0}_{1}", atomType[iAtom], atomHybridization[iAtom]);
                    //Debug.Log(string.Format("key1, key2: '{0}' '{1}'", key1, key2));
                    float R01;
                    float R02;
                    float[] value;

                    if(DREIDINGConst.TryGetValue(key1, out value))
                    {
                        R01 = value[0];
                    }
                    else
                    {
                        R01 = 70f;
                        //ForceFieldConsole.Instance.statusOut(string.Format("Warning {0} : unknown atom or hybridization", key1));
                    }

                    if (DREIDINGConst.TryGetValue(key2, out value))
                    {
                        R02 = value[0];
                    }
                    else
                    {
                        R02 = 70f;
                        //ForceFieldConsole.Instance.statusOut(string.Format("Warning {0} : unknown atom or hybridization", key2));
                    }

                    newBond.Req = R01 + R02 - 1f;
                    newBond.kBond = kb;
                    bondList.Add(newBond);
                    nBond++;
                }
                else if (atomType[iAtom] != "Dummy" && atomType[jAtom] != "Dummy")  // avoid dummy terms right away
                {
                    bool avoid = false;
                    // check for next-nearest neighborhood (1-3 interaction)
                    for (int kAtom = 0; kAtom < nAtoms; kAtom++)
                    {
                        if (topo[iAtom, kAtom] && topo[jAtom, kAtom])
                        {
                            avoid = true; break;
                        }
                    }

                    if (!avoid)
                    {
                        HardSphereTerm newHS = new HardSphereTerm();
                        newHS.Atom1 = jAtom;
                        newHS.Atom2 = iAtom;
                        newHS.kH = 10f;
                        newHS.Rcrit = rhs[atomType[iAtom]] + rhs[atomType[jAtom]];
                        hsList.Add(newHS);
                        nnoBond++;
                    }
                }
            }

        }



        // angle terms
        // run over unique bond pairs
        foreach (BondTerm bond1 in bondList)
        {
            foreach (BondTerm bond2 in bondList)
            {
                // if we reached the same atom pair, we can skip
                if (bond1.Atom1 == bond2.Atom1 && bond1.Atom2 == bond2.Atom2) break;

                int idx = -1, jdx = -1, kdx = -1;
                if (bond1.Atom1 == bond2.Atom1)
                {
                    idx = bond1.Atom2; jdx = bond1.Atom1; kdx = bond2.Atom2;
                }
                else if (bond1.Atom1 == bond2.Atom2)
                {
                    idx = bond1.Atom2; jdx = bond1.Atom1; kdx = bond2.Atom1;
                }
                else if (bond1.Atom2 == bond2.Atom1)
                {
                    idx = bond1.Atom1; jdx = bond1.Atom2; kdx = bond2.Atom2;
                }
                else if (bond1.Atom2 == bond2.Atom2)
                {
                    idx = bond1.Atom1; jdx = bond1.Atom2; kdx = bond2.Atom1;
                }
                if (idx > -1) // if anything was found: set term
                {
                    AngleTerm newAngle = new AngleTerm();
                    newAngle.Atom1 = kdx;  // I put kdx->Atom1 and idx->Atom3 just for aesthetical reasons ;)
                    newAngle.Atom2 = jdx;
                    newAngle.Atom3 = idx;
                    float[] value;
                    string key = string.Format("{0}_{1}", atomType[jdx], atomHybridization[jdx]);
                    float phi0;
                    if (DREIDINGConst.TryGetValue(key, out value))
                    {
                        phi0 = value[1];
                    }
                    else
                    {
                        phi0 = alphaNull;
                        //ForceFieldConsole.Instance.statusOut(string.Format("Warning {0} : unknown atom or hybridization", key));
                    }

                    if (phi0 != 180f)
                    {
                        newAngle.kAngle = ka / (Mathf.Sin(phi0 * (Mathf.PI / 180f))* Mathf.Sin(phi0 * (Mathf.PI / 180f)));
                    }
                    else
                    {
                        newAngle.kAngle = ka;
                    } 

                    newAngle.Aeq = phi0;
                    angleList.Add(newAngle);
                    nAngle++;
                    //Debug.Log(string.Format(" newAngle {0} :  {1} {2} {3} {4,12:f3}  {5,12:f3}", nAngle, kdx, jdx, idx, newAngle.kAngle, newAngle.Aeq)); 
                }
            }
        }
        if (LogLevel >= 1000)
        {
            FFlog.WriteLine("Angle terms:");
            FFlog.WriteLine(" Atom1  Atom2  Atom3    kAngle           Aeq");
            foreach (AngleTerm angle in angleList)
            {
                FFlog.WriteLine(string.Format(" {0,4} - {1,4} - {2,4}  {3,12:f3}  {4,12:f3}",
                    angle.Atom1, angle.Atom2, angle.Atom3, angle.kAngle, angle.Aeq));
            }
        }


        if (torsionActive)
        {
            foreach (AngleTerm threebond1 in angleList)
            {
                //if (threebond1.Aeq == 180f)break; ??
                foreach (BondTerm bond2 in bondList)
                {
                    // if the bond is in our threebond we can skip
                    if (threebond1.Atom1 == bond2.Atom1 && threebond1.Atom2 == bond2.Atom2) continue; // break;
                    if (threebond1.Atom1 == bond2.Atom2 && threebond1.Atom2 == bond2.Atom1) continue; // break;
                    if (threebond1.Atom2 == bond2.Atom1 && threebond1.Atom3 == bond2.Atom2) continue; // break;
                    if (threebond1.Atom2 == bond2.Atom2 && threebond1.Atom3 == bond2.Atom1) continue; // break;

                    int idx = -1, jdx = -1, kdx = -1, ldx = -1;
                    bool improper = false;

                    if (threebond1.Atom3 == bond2.Atom1)
                    {
                        //new l atom connects to k
                        idx = threebond1.Atom1; jdx = threebond1.Atom2; kdx = threebond1.Atom3; ldx = bond2.Atom2;
                    }
                    else if (threebond1.Atom3 == bond2.Atom2)
                    {
                        //new l atom connects to k, but the other way around 
                        idx = threebond1.Atom1; jdx = threebond1.Atom2; kdx = threebond1.Atom3; ldx = bond2.Atom1;
                    }
                    else if (threebond1.Atom1 == bond2.Atom1)
                    {
                        // new l connects to i, new definition of i j k l, so that j and k are in the middle. i and j are the new j and k now
                        idx = bond2.Atom2; jdx = threebond1.Atom1; kdx = threebond1.Atom2; ldx = threebond1.Atom3;
                    }
                    else if (threebond1.Atom1 == bond2.Atom2)
                    {
                        // new l connects to i, new definition of i j k l, so that j and k are in the middle. i and j are the new j and k now
                        idx = bond2.Atom1; jdx = threebond1.Atom1; kdx = threebond1.Atom2; ldx = threebond1.Atom3;
                    }
                    // improper case, that means that all 3 atoms are connected to atom2
                    else if (threebond1.Atom2 == bond2.Atom1)
                    {
                        // j is Atom which connects to i k l 
                        idx = threebond1.Atom1; jdx = threebond1.Atom2; kdx = bond2.Atom2; ldx = threebond1.Atom3;
                        improper = true;
                    }
                    else if (threebond1.Atom2 == bond2.Atom2)
                    {
                        // j is Atom which connects to i k l 
                        idx = threebond1.Atom1; jdx = threebond1.Atom2; kdx = bond2.Atom1; ldx = threebond1.Atom3;
                        improper = true;
                    }
                    //if (improper) break;
                    if (ldx > -1) // if anything was found: set term
                    {
                        
                        TorsionTerm newTorsion = new TorsionTerm();
                        newTorsion.Atom1 = idx;
                        newTorsion.Atom2 = jdx;
                        newTorsion.Atom3 = kdx;
                        newTorsion.Atom4 = ldx;
                        if (!improper)
                        {
                            float nTorsTerm = Mathf.Max(1f, (nBondP[jdx]-1) * (nBondP[kdx]-1));
                            //Debug.Log(string.Format(" nTorsTerm  {1} {2} {3} {4} : {0} {5} {6} ", nTorsTerm, idx, jdx, kdx, ldx, nBondP[jdx], nBondP[kdx]));

                            if (atomHybridization[jdx] == 3 && atomHybridization[kdx] == 3) //two sp3 atoms
                            {
                                newTorsion.vk = 0.02f * k0 / nTorsTerm;
                                newTorsion.nn = 3;
                                newTorsion.phieq = 180f; // Mathf.PI;
                                //print("1. Case 2 sp3");
                            }
                            else if (atomHybridization[jdx] == 2 && atomHybridization[kdx] == 3 ||
                                     atomHybridization[jdx] == 3 && atomHybridization[kdx] == 2)
                            {
                                newTorsion.vk = 0.01f * k0 / nTorsTerm;
                                newTorsion.nn = 6;
                                newTorsion.phieq = 0;
                                //print("2. Case sp3 und sp2");
                            }
                            else if (atomHybridization[jdx] == 2 && atomHybridization[kdx] == 2)
                            {
                                //Bond bondJK = Bond.Instance.getBond(Atom.Instance.getAtomByID(jdx), Atom.Instance.getAtomByID(kdx)); //??
                                //print(bondJK.m_bondOrder);
                                if (false) //bondJK.m_bondOrder == 2.0f)
                                {
                                    newTorsion.vk = 0.45f * k0 / nTorsTerm;
                                    newTorsion.nn = 2;
                                    newTorsion.phieq = 180f; // Mathf.PI;
                                    //print("3. Case 2 sp2, Doppelbindung");
                                }
                                else //if(bondJK.m_bondOrder == 1.0f || bondJK.m_bondOrder == 1.5f)       
                                {
                                    newTorsion.vk = 0.05f * k0 / nTorsTerm;
                                    newTorsion.nn = 2;
                                    newTorsion.phieq = 180f; // Mathf.PI;
                                    //print("3. Case 2 sp2, singlebond or resonant atoms");
                                    /*if(exocyclic dihedral single bond involving two aromatic atoms)   //f) exception for exocyclic dihedral single bond involving two aromatic atoms ??
                                        {
                                             newTorsion.vk = 10f;
                                             newTorsion.nn = 2;
                                             newTorsion.phieq = 180f;
                                             print("exocyclic dihedral single bond involving two aromatic atoms");
                                        }
                                    */
                                }
                            }
                            else if (atomHybridization[jdx] == 4 && atomHybridization[kdx] == 4)
                            {
                                //print("resonance bond");
                                newTorsion.vk = 0.25f * k0 / nTorsTerm;
                                newTorsion.nn = 2;
                                newTorsion.phieq = 180f; // Mathf.PI;
                            }
                            else if (atomHybridization[jdx] == 1 || atomHybridization[kdx] == 1)
                            {
                                //print("4. Case 2 sp1");
                                newTorsion.vk = 0f;
                                newTorsion.nn = 0;
                                newTorsion.phieq = 180f; //Mathf.PI;
                            }
                            else // take default values
                            {
                                //print("DEFAULT Case");
                                newTorsion.vk = 0.1f * k0  / nTorsTerm;
                                newTorsion.nn = 3;
                                newTorsion.phieq = 180f; //Mathf.PI;
                            }
                        }
                        else //improper
                        {
                            /*
                            Vector3 rij = position[idx] - position[jdx];
                            Vector3 rkj = position[kdx] - position[jdx];
                            Vector3 rkl = position[kdx] - position[ldx];                            
                            Vector3 mNormalized = Vector3.Cross(rij, rkj).normalized;
                            Vector3 nNormalized = Vector3.Cross(rkj, rkl).normalized;

                            float cosAlpha = Mathf.Min(1.0f, Mathf.Max(-1.0f, (Vector3.Dot(nNormalized, mNormalized))));                      
                            float phi = Mathf.Sign(Vector3.Dot(rij, nNormalized)) * Mathf.Acos(cosAlpha);
                            */

                            // TRY:
                            float fImproper = 1f/12f; // usual case for 4 bond partners
                            if (nBondP[jdx]==3) fImproper = 1f/6f;
                            newTorsion.vk = 2 * kim * fImproper;

                            //newTorsion.vk = 2 * kim;
                            newTorsion.nn = 1;
                            if (atomHybridization[jdx] == 3)
                            {
                                newTorsion.nn = 3; // TRY:
                                // if (phi > 0f)
                                //{
                                    newTorsion.phieq = 120f;
                                // }
                                //else
                                //{
                                //    newTorsion.phieq = -120f;
                                //}
                            }
                            else if (atomHybridization[jdx] == 2)
                            {
                                newTorsion.phieq = 180f;
                            }
                            else
                            {
                                //ForceFieldConsole.Instance.statusOut(string.Format("Warning {0} : improper for unknown hybridization", jdx));
                                newTorsion.phieq = 90f;
                            }
                        }
                        torsionList.Add(newTorsion);
                        nTorsion++;
                        //Debug.Log(string.Format(" nTorsion {0} :  {1} {2} {3} {4}", nTorsion, idx, jdx, kdx, ldx));
                    }
                }
            }
            if (LogLevel >= 1000)
            {
                FFlog.WriteLine("Torsion terms:");
                FFlog.WriteLine(" Atom1  Atom2  Atom3  Atom4   vk           phieq");
                foreach (TorsionTerm torsion in torsionList)
                {
                    FFlog.WriteLine(string.Format(" {0,4} - {1,4} - {2,4} - {3,4}  {4,12:f3}  {5,12:f3}",
                        torsion.Atom1, torsion.Atom2, torsion.Atom3, torsion.Atom4, torsion.vk, torsion.phieq));
                }
            }
        }
    }


    // evaluate the ForceField and compute update of positions
    // to enhance stability, do more than one timestep for each actual update
    // in applyMovements, finally the actual objects are updated
    void applyFF()
    {
        for (int istep = 0; istep < nTimeSteps; istep++)
        {
            //Loop Bond List
            foreach (BondTerm bond in bondList)
            {
                calcBondForces(bond);
            }

            //Loop Angle List
            foreach (AngleTerm angle in angleList)
            {
                calcAngleForces(angle, istep == 0);
            }

            //Loop Torsion List
            
            foreach (TorsionTerm torsion in torsionList)
            {

                calcTorsionForces(torsion, istep == 0 && frame % 10 == 0);

            }
            
                                                       
            //Loop Bond List
            foreach (HardSphereTerm hsTerm in hsList)
            {
               calcRepForces(hsTerm);
            }                  
                                             
            calcMovements();               
            
        }
        applyMovements();

        // give some output:
        //ForceFieldConsole.Instance.statusOut(string.Format("Current RMS force : {0,14:f3}", RMSforce()));
        //ForceFieldConsole.Instance.statusOut(string.Format("Number of Atoms / Dummies :   {0,6} {1,6}", nAtoms - nDummies, nDummies));
        //ForceFieldConsole.Instance.statusOut(string.Format("Number of Bond / Angle / noBond terms :   {0,6} {1,6} {2,6}", nBond, nAngle, nnoBond));
        //ForceFieldConsole.Instance.statusOut(string.Format("Number of TorsionTerms :   {0,6}", nTorsion));

    }

    // calculate non-bonding repulsion forces
    void calcRepForces(HardSphereTerm hsTerm)
    {
        if (LogLevel >= 1000) FFlog.WriteLine("calcRepForces for {0} - {1}", hsTerm.Atom1, hsTerm.Atom2);
        //bond vector
        Vector3 rij = position[hsTerm.Atom1] - position[hsTerm.Atom2];
        float delta = rij.magnitude - hsTerm.Rcrit * GlobalCtrl.Instance.repulsionScale;
        //Debug.Log(string.Format("D nb term {0,4} {1,4}: rij = {2,14:f2}", hsTerm.Atom1, hsTerm.Atom2, rij.magnitude));
        if (delta<0.0f)
        {
            float frep = -hsTerm.kH * delta;
            //Debug.Log(string.Format("nb term {0,4} {1,4}: rij = {2,14:f2} crit = {3,14:f3}", hsTerm.Atom1, hsTerm.Atom2, rij.magnitude, hsTerm.Rcrit));
            forces[hsTerm.Atom1] += frep * rij.normalized;
            forces[hsTerm.Atom2] -= frep * rij.normalized;
        }

    }


    // calculate bond forces
    void calcBondForces(BondTerm bond)
    {
        if (LogLevel >= 1000) FFlog.WriteLine("calcBondForces for {0} - {1}", bond.Atom1, bond.Atom2);
        //bond vector
        Vector3 rb = position[bond.Atom1] - position[bond.Atom2];
        //force on this bond vector
        float delta = rb.magnitude - bond.Req;
        float fb = -bond.kBond * delta;
        if (LogLevel >= 1000) FFlog.WriteLine("dist: {0,12:f3}  dist0: {1,12:f3}  --  force = {2,14:f5} ", rb.magnitude, bond.Req, fb);
        //separate the forces on the two atoms
        //Vector3 fc1 = fb * (rb / Vector3.Magnitude(rb)); // could use rb.normalized
        //Vector3 fc2 = -fb * (rb / Vector3.Magnitude(rb));
        Vector3 fc1 = fb * rb.normalized;
        Vector3 fc2 = -fb * rb.normalized;

        if (LogLevel >= 10000)
        {
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", bond.Atom1, fc1.x, fc1.y, fc1.z));
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", bond.Atom2, fc2.x, fc2.y, fc2.z));
        }

        forces[bond.Atom1] += fc1;
        forces[bond.Atom2] += fc2;

        //if(bond.Atom1 == 1) print ("Bondforce 1:" + fc1);
        //if(bond.Atom2 == 1) print ("Bondforce 2:" + fc2);

        if (LogLevel >= 10000)
        {
            FFlog.WriteLine("Updated forces:");
            for (int iAtom = 0; iAtom < nAtoms; iAtom++)
            {
                FFlog.WriteLine(string.Format(" {0,4:d}  -  {1,5:d}   {2,14:f6} {3,14:f6}  {4,14:f6}",
                    iAtom, atomList[iAtom],
                    forces[iAtom].x, forces[iAtom].y, forces[iAtom].z));
            }
        }
    }


    // calculate angle forces
    void calcAngleForces(AngleTerm angle, bool debug)
    {
        if (LogLevel >= 1000) FFlog.WriteLine("calcAngleForces for {0} - {1} - {2}", angle.Atom1, angle.Atom2, angle.Atom3);
        Vector3 rb1 = position[angle.Atom1] - position[angle.Atom2];
        Vector3 rb2 = position[angle.Atom3] - position[angle.Atom2];

        float cosAlpha = (Vector3.Dot(rb1, rb2)) / (Vector3.Magnitude(rb1) * Vector3.Magnitude(rb2));
        /*  Alpha- dependency
            float mAlpha = angle.kAngle * (Mathf.Acos(cosAlpha) * (180.0f / Mathf.PI) - angle.Aeq);
      
            Vector3 fI = (mAlpha / (Vector3.Magnitude(rb1) * Mathf.Sqrt(1.0f - cosAlpha * cosAlpha))) * ((rb2 / Vector3.Magnitude(rb2)) - cosAlpha * (rb1 / Vector3.Magnitude(rb1)));
            Vector3 fK = (mAlpha / (Vector3.Magnitude(rb2) * Mathf.Sqrt(1.0f - cosAlpha * cosAlpha))) * ((rb1 / Vector3.Magnitude(rb1)) - cosAlpha * (rb2 / Vector3.Magnitude(rb2)));
            Vector3 fJ = -fI - fK;
        */
        float mAlpha;

        if (angle.Aeq != 180f)
        {
            mAlpha = angle.kAngle * (cosAlpha - Mathf.Cos(angle.Aeq * (Mathf.PI / 180.0f)));
        }
        else
        {
            mAlpha = angle.kAngle;
        }

        Vector3 fI = -mAlpha/ Vector3.Magnitude(rb1) * ((rb2 / Vector3.Magnitude(rb2)) - cosAlpha * (rb1 / Vector3.Magnitude(rb1)));
        Vector3 fK = -mAlpha/ Vector3.Magnitude(rb2) * ((rb1 / Vector3.Magnitude(rb1)) - cosAlpha * (rb2 / Vector3.Magnitude(rb2)));
        Vector3 fJ = -fI - fK;

        //if(debug) Debug.Log(string.Format("angle {0} - {1} - {2} : {3,12:f3}  angle0 {4,12:f3}  --  moment = {5,14:f5} ", angle.Atom1, angle.Atom2, angle.Atom3, Mathf.Acos(cosAlpha) * (180.0f / Mathf.PI), angle.Aeq, mAlpha)); //??!!

        if (LogLevel >= 1000) FFlog.WriteLine("angle: {0,12:f3}  angle0: {1,12:f3}  --  moment = {2,14:f5} ", Mathf.Acos(cosAlpha) * (180.0f / Mathf.PI), angle.Aeq, mAlpha);

        if (LogLevel >= 10000)
        {
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", angle.Atom1, fI.x, fI.y, fI.z));
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", angle.Atom2, fJ.x, fJ.y, fJ.z));
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", angle.Atom3, fK.x, fK.y, fK.z));
        }

        forces[angle.Atom1] += fI;
        forces[angle.Atom2] += fJ;
        forces[angle.Atom3] += fK;

        //if (angle.Atom3 == 1) print("angleforce 3:" + fK);
        //if (angle.Atom2 == 1) print("angleforce 2:" + fJ);
        //if (angle.Atom1 == 1) print("angleforce 1:" + fI);
       
        if (LogLevel >= 10000)
        {
            FFlog.WriteLine("Updated forces:");
            for (int iAtom = 0; iAtom < nAtoms; iAtom++)
            {
                FFlog.WriteLine(string.Format(" {0,4:d}  -  {1,5:d}   {2,14:f6} {3,14:f6}  {4,14:f6}",
                    iAtom, atomList[iAtom],
                    forces[iAtom].x, forces[iAtom].y, forces[iAtom].z));
            }
        }

    }


    //calculate orsion forces
    void calcTorsionForces(TorsionTerm torsion, bool debug)
     {
        
        //if (LogLevel >= 1000) FFlog.WriteLine("calcTorsionForces for {0} - {1} - {2} - {3}", torsion.Atom1, torsion.Atom2, torsion.Atom3, torsion.Atom4);
        Vector3 rij = position[torsion.Atom1] - position[torsion.Atom2];
        Vector3 rkj = position[torsion.Atom3] - position[torsion.Atom2];
        Vector3 rkl = position[torsion.Atom3] - position[torsion.Atom4];
        Vector3 mNormal = Vector3.Cross(rij, rkj);
        Vector3 mNormalized = Vector3.Cross(rij, rkj).normalized;
        Vector3 nNormal = Vector3.Cross(rkj, rkl);
        Vector3 nNormalized = Vector3.Cross(rkj, rkl).normalized;

        float cosAlpha = Mathf.Min(1.0f,Mathf.Max(-1.0f,(Vector3.Dot(nNormalized,mNormalized))));

        //phi0: position for minimum often with phi0=0
        float phi0 = torsion.phieq * Mathf.PI / 180f;
        //V0 not important, because we only need the forces d V / d phi
        //float V0 = 0;
        int nn = torsion.nn;
        float Vn = torsion.vk;
        float phi = Mathf.Sign(Vector3.Dot(rij,nNormal))*Mathf.Acos(cosAlpha);
        //float Vphi = V0 - Vn * Mathf.Cos(nn*(phi-phi0));

        if (nn == 1)    //improper
        {
         
        }

        float Fphi = Vn* nn * Mathf.Sin(nn*(phi-phi0));

        //if(debug ) Debug.Log(string.Format("torsion {0} - {1} - {2} - {3} : phi {4,12:f3}  --  Fphi = {5,14:f5} phi0 {6}", torsion.Atom1, torsion.Atom2, torsion.Atom3, torsion.Atom4, phi*180f/Mathf.PI , Fphi, phi0 * 180f / Mathf.PI));   //!! && torsion.Atom2 == 6

        Vector3 fti = -Fphi* rkj.magnitude / mNormal.magnitude * mNormalized;
        Vector3 ftl = Fphi * rkj.magnitude / nNormal.magnitude * nNormalized;
        Vector3 ftj = -fti + ((Vector3.Dot(rij,rkj))/Vector3.Dot(rkj,rkj))*fti - ((Vector3.Dot(rkl,rkj))/(Vector3.Dot(rkj,rkj)))*ftl;
        Vector3 ftk = -ftl - ((Vector3.Dot(rij,rkj))/Vector3.Dot(rkj,rkj))*fti + ((Vector3.Dot(rkl,rkj))/(Vector3.Dot(rkj,rkj)))*ftl;
            
        if (LogLevel >= 1000) FFlog.WriteLine("torsion: {0,12:f3}  {1,12:f3}  {2,12:f3} ", torsion.phieq, cosAlpha, phi);

        forces[torsion.Atom1] += fti;
        forces[torsion.Atom2] += ftj;
        forces[torsion.Atom3] += ftk;
        forces[torsion.Atom4] += ftl;

        //if (torsion.Atom1 == 1) print("torsionforce 1:" + fti);
        //if (torsion.Atom2 == 1) print("torsionforce 2:" + ftj);
        //if (torsion.Atom3 == 1) print("torsionforce 3:" + ftk); 
        //if (torsion.Atom4 == 1) print("torsionforce 4:" + ftl);

    }

    float RMSforce()
    {
        if (nAtoms == 0)
        {
            return 0.0f;
        }
        else
        {
            float sqsum = 0.0f;
            for (int iAtom = 0; iAtom < nAtoms; iAtom++)
            {
                sqsum += forces[iAtom].x * forces[iAtom].x;
                sqsum += forces[iAtom].y * forces[iAtom].y;
                sqsum += forces[iAtom].z * forces[iAtom].z;
            }
            sqsum = Mathf.Sqrt(sqsum) / nAtoms;
            return sqsum;
        }
    }

    // turn forces into movements and apply sanity checks 
    void calcMovements()
    {

        if (LogLevel >= 1000)
        {
            FFlog.WriteLine("Computed forces and applicable masses:");
            for (int iAtom = 0; iAtom < nAtoms; iAtom++)
            {
                FFlog.WriteLine(string.Format(" {0,4:d}  -  {1,5:d}   {2,14:f6} {3,14:f6}  {4,14:f6}    m = {5,9:f3} ",
                    iAtom, atomList[iAtom],
                    forces[iAtom].x, forces[iAtom].y, forces[iAtom].z, atomMass[iAtom]));
            }
        }


        // force -> momentum change: divide by mass
        // momentum change to position change: apply time factor
        for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        {
            // negative masses flag a fixed atom
            if (atomMass[iAtom] > 0.0f)
            {
                forces[iAtom] *= timeFactor / atomMass[iAtom];
            }
            else
            {
                forces[iAtom] = new Vector3(0.0f, 0.0f, 0.0f);
            }
        }


        // check for too long steps:
        float MaxMove = 10f;
        float moveMaxNorm = 0f; // norm of movement vector
        for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        {
            float moveNorm = Vector3.Magnitude(forces[iAtom]);
            moveMaxNorm = Mathf.Max(moveMaxNorm, moveNorm);
        }
        if (moveMaxNorm > MaxMove)
        {
            float scaleMove = MaxMove / moveMaxNorm;
            if (LogLevel >= 100) FFlog.WriteLine("moveMaxNorm was {0:f3} - scaling by {1:f10}", moveMaxNorm, scaleMove);

            for (int iAtom = 0; iAtom < nAtoms; iAtom++)
            {
                forces[iAtom] *= scaleMove;
            }
        }

        if (LogLevel >= 100)
        {
            FFlog.WriteLine("Computed movements:");
            for (int iAtom = 0; iAtom < nAtoms; iAtom++)
            {
                FFlog.WriteLine(string.Format(" {0,4:d}  -  {1,5:d}   {2,14:f6} {3,14:f6}  {4,14:f6}",
                    iAtom, atomList[iAtom],
                    forces[iAtom].x, forces[iAtom].y, forces[iAtom].z));
            }
        }

        // update position and total movement:
        for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        {
            movement[iAtom] += forces[iAtom];
            position[iAtom] += forces[iAtom];
        }

    }


    void applyMovements()
    {
        for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        {
            // get atom identified and update the actual object
            // scale to Unity's unit system
            int atID = atomList[iAtom];
            //Atom.Instance.getAtomByID(atID).transform.localPosition += movement[iAtom] * scalingfactor;
            Atom.Instance.getAtomByID(atID).transform.position += movement[iAtom] * scalingfactor;
        }
    }

    // connections between atoms get scaled new as soon as the position of an atom gets updated
    public void scaleConnections()
    {
        // For each Molecule in scene
        // For each Bond in Molecule
        // Calculate distance between atoms
        // Scale, transform position, LookAt
        
        foreach(Molecule mol in GlobalCtrl.Instance.List_curMolecules)
        //foreach(KeyValuePair<int, Molecule> mol in GlobalCtrl.Instance.Dic_curMolecules)
        {
            foreach(Bond bond in mol.bondList)
            {
                Atom a1 = Atom.Instance.getAtomByID(bond.atomID1);
                Atom a2 = Atom.Instance.getAtomByID(bond.atomID2);
                float distance = Vector3.Distance(a1.transform.position, a2.transform.position);
                bond.transform.localScale = new Vector3(bond.transform.localScale.x, bond.transform.localScale.y, distance);
                bond.transform.position = (a1.transform.position + a2.transform.position) / 2;
                bond.transform.LookAt(a2.transform.position);
            }
        }
    }
    
}


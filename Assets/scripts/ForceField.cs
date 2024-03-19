using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System;
using UnityEngine;
using UnityEngine.UI;
using chARpackStructs;
using System.Linq;
using System.Diagnostics;

public class ForceField : MonoBehaviour
{

    private static ForceField _singleton;
    public static ForceField Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                UnityEngine.Debug.Log($"[{nameof(ForceField)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    bool enableForceField = SettingsData.forceField;

    public struct BondTerm
    {
        public ushort Atom1; public ushort Atom2; public float kBond; public float eqDist; public float order;
    }
    public struct AngleTerm
    {
        public ushort Atom1; public ushort Atom2; public ushort Atom3; public float kAngle; public float eqAngle;
    }
    public struct TorsionTerm
    {
        public ushort Atom1; public ushort Atom2; public ushort Atom3; public ushort Atom4; public float vk; public float eqAngle; public ushort nn;
    }
    public struct HardSphereTerm
    {
        public ushort Atom1; public ushort Atom2; public float kH; public float Rcrit;
    }

    int nAtoms = 0;
    public static bool torsionActive = true;

    //float scalingFactor = 154 / (154 / GetComponent<GlobalCtrl>().scale); // with this, 154 pm are equivalent to 0.35 m in the model
    // note that the forcefield works in the atomic scale (i.e. all distances measure in pm)
    // we scale back when applying the movements to the actual objects
    public static float scalingfactor = GlobalCtrl.scale / GlobalCtrl.u2pm;
    private float _repulsionScale = SettingsData.repulsionScale;
    public float repulsionScale { get => _repulsionScale; set => _repulsionScale = value; }

    // Parameters of the integration methods
    public float EulerTimeFactor = 0.6f;
    public float SVtimeFactor = 0.75f;
    public float RKtimeFactor = 0.25f;
    public float MPtimeFactor = 0.2f;
    float RKstepMin = 0.05f;
    float RKstepMax = 0.25f;
    float RKc = 0.1f;
    private float alpha = 0.5f;
    public static float SDepsilon = 0.0001f;
    public static float SDdefaultLambda = 1f;
    public static float SDsimilarity = 0.9f;
    public static float SDstepReduction = 0.75f;
    public static float SDstepIncrease = 1.1f;


    // Stiffness of molecules/bonds
    ushort _stiffness;
    public ushort stiffness { get => _stiffness; set { _stiffness = value; if (_stiffness == 0) { integrationCycles = 1; } else { integrationCycles = (ushort)(5 * _stiffness); } } }
    ushort integrationCycles = 1;

    // convergence measurment threshold
    float threshold = 0.025f;
    int num_steps_to_convergence = 0;
    bool do_measurment = true;
    Stopwatch stopwatch;
    public enum Method
    {
        Euler,
        Verlet,
        RungeKutta,
        Heun,
        Ralston,
        MidPoint,
        SteepestDescent
    }
    public Method _currentMethod;

    public Method currentMethod { get => _currentMethod; 
        set {
            _currentMethod = value;
            if (_currentMethod == Method.Heun)
            {
                alpha = 1f;
            }
            else if (_currentMethod == Method.Ralston)
            {
                alpha = 2f / 3f;
            }
            else
            {
                alpha = 0.5f; // RK
            }
        }
    }


    /*
    const float k0 = 100f;         //between 100 - 5000
    const float kb = k0;           //bond force constant
    const float ka = kb*1430f;     //angle force constant
    const float kim = kb*70f;      //improper torsion force constant
    */
    public static float k0 = 3000f;               //between 100 - 5000
    public static float ka = 3f*k0;               //angle force constant
    public static float kb = 7f * k0 / 10000f;    //bond force constant, "/ 10000f" because of caculating A^2 to pm^2
    public static float kim = 0.02f * k0;         //improper torsion force constant 0.45 ; 0.045

    //float standardDistance = 154f; // integrate into new bondList
    public static float alphaNull = 109.4712f; // integrate into new angleList
    // constants for hard-sphere terms, ca. 90% of van-der-Waals radius  .... have to set them even smaller
    public static float fac = 0.9f;

    public static Dictionary<string, float> rhs = new Dictionary<string, float>();
    public static Dictionary<string, float[]> DREIDINGConst = new Dictionary<string, float[]> {
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
    public int LogLevel = 0;


    /// <summary>
    /// Create Singleton
    /// </summary>
    private void Awake()
    {
        Singleton = this;
    }

    public void toggleForceFieldUI()
    {
        toggleForceField();
        EventManager.Singleton.EnableForceField(enableForceField);
    }

    /// <summary>
    /// Toggles the force field during runtime
    /// </summary>
    public void toggleForceField()
    {
        enableForceField = !enableForceField;
        SettingsData.forceField = enableForceField;
    }

    public void enableForceFieldMethod(bool enable)
    {
        enableForceField = enable;
        SettingsData.forceField = enableForceField;
    }

    //TODO: This can probably be implemented more elegantly
    public void switchIntegrationMethodForward()
    {
        switch (currentMethod)
        {
            case Method.Euler: 
                currentMethod = Method.Verlet; 
                break;
            case Method.Verlet:
                currentMethod = Method.RungeKutta;
                break;
            case Method.RungeKutta:
                currentMethod = Method.Heun;
                break;
            case Method.Heun:
                currentMethod = Method.Ralston;
                break;
            case Method.Ralston:
                currentMethod = Method.SteepestDescent;
                break;
            case Method.SteepestDescent:
                currentMethod = Method.MidPoint;
                break;
            default:
                currentMethod = Method.Euler;
                break;
        }
    }
    public void switchIntegrationMethodBackward()
    {
        switch (currentMethod)
        {
            case Method.Euler:
                currentMethod = Method.MidPoint;
                break;
            case Method.Verlet:
                currentMethod = Method.Euler;
                break;
            case Method.RungeKutta:
                currentMethod = Method.Verlet;
                break;
            case Method.Heun:
                currentMethod = Method.RungeKutta;
                break;
            case Method.Ralston:
                currentMethod = Method.Heun;
                break;
            case Method.SteepestDescent:
                currentMethod = Method.Ralston;
                break;
            default:
                currentMethod = Method.SteepestDescent;
                break;
        }
    }

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
        // init integration variables
        stiffness = SettingsData.bondStiffness;
        currentMethod = SettingsData.integrationMethod;

        Dictionary<string, ElementData> element_dict = GlobalCtrl.Singleton.Dic_ElementData;
        if ( element_dict == null)
        {
            UnityEngine.Debug.LogError("[ForceField] Could not obtain element dictionary from globalCtrl instance.");
        }

        rhs.Clear();
        foreach (KeyValuePair<string, ElementData> pair in element_dict)
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
        if (LogLevel >= 100 && enableForceField)
        {
            FFlog.WriteLine("Current frame: " + frame);
            FFlog.WriteLine("Current time:  " + Time.time.ToString("f6") + "  Delta: " + Time.deltaTime.ToString("f6"));
        }
        // If the forcefield is active, update all connections and forces, else only update connections
        if (enableForceField)
        {
            clearLists();
            applyFF();
            //applyFF_onConvergence();
            scaleConnections();
        }
        else
        {
            scaleConnections();
        }
    }


    public void resetMeasurment()
    {
        num_steps_to_convergence = 0;
        do_measurment = true;
        stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Measures convergence based on size of movements due to force field.
    /// </summary>
    void measureConvergence()
    {
        bool converged = true;
        foreach (var mol in GlobalCtrl.Singleton.List_curMolecules)
        {
            foreach (var step in mol.FFmovement)
            {
                converged = converged && (step.magnitude < threshold);
            }
        }
        if (converged)
        {
            stopwatch?.Stop();
            UnityEngine.Debug.Log($"[ForceField:measureConvergence] Convergence reached after {num_steps_to_convergence} steps in {stopwatch?.ElapsedMilliseconds*1e-3} s. Method {currentMethod}");
            do_measurment = false;
        }
        else
        {
            num_steps_to_convergence++;
        }
    }

    bool isConverged()
    {
        bool converged = true;
        foreach (var mol in GlobalCtrl.Singleton.List_curMolecules)
        {
            for (int i = 0; i < mol.atomList.Count; i++)
            {
                converged = converged && (mol.FFmovement[i].magnitude < threshold);
            }
        }
        return converged;
    }


    void clearLists()
    {
        foreach (var mol in GlobalCtrl.Singleton.List_curMolecules)
        {
            mol.FFposition.Clear();
            mol.FFforces.Clear();
            mol.FFforces_pass2.Clear();
            mol.FFmovement.Clear();
            foreach(var a in mol.atomList)
            {
                // if chain atoms are grabbed they are in a different local coordiante systen
                // need to transform them back to local molecule coordnates
                //if (a.isGrabbed)
                //{
                    mol.FFposition.Add(mol.transform.InverseTransformPoint(a.transform.position) * (1f / scalingfactor));
                //}
                //else
                //{
                //    mol.FFposition.Add(a.transform.localPosition * (1f / scalingfactor));
                //}

                mol.FFforces.Add(Vector3.zero);
                mol.FFforces_pass2.Add(Vector3.zero);
                mol.FFmovement.Add(Vector3.zero);
            }
        }
    }

    // evaluate the ForceField and compute update of positions
    // to enhance stability, do more than one timestep for each actual update
    // in applyMovements, finally the actual objects are updated
    void applyFF()
    {
        for (int istep = 0; istep < integrationCycles; istep++)
        {
            // Forces Pass 1
            foreach (var mol in GlobalCtrl.Singleton.List_curMolecules)
            {
                //Loop Bond List
                foreach (BondTerm bond in mol.bondTerms)
                {
                    calcBondForces(bond, mol);
                }

                //Loop Angle List
                foreach (AngleTerm angle in mol.angleTerms)
                {
                    calcAngleForces(angle, mol);
                }

                //Loop Torsion List
                foreach (TorsionTerm torsion in mol.torsionTerms)
                {
                    calcTorsionForces(torsion, mol);
                }

                //Loop Bond List
                foreach (HardSphereTerm hsTerm in mol.hsTerms)
                {
                    calcRepForces(hsTerm, mol);
                }
            }
            // do second Force pass for these Methods
            if (currentMethod == Method.RungeKutta || currentMethod == Method.Heun || currentMethod == Method.Ralston)
            {
                // Forces Pass 2
                foreach (var mol in GlobalCtrl.Singleton.List_curMolecules)
                {
                    //Loop Bond List
                    foreach (BondTerm bond in mol.bondTerms)
                    {
                        calcBondForces(bond, mol, true);
                    }

                    //Loop Angle List
                    foreach (AngleTerm angle in mol.angleTerms)
                    {
                        calcAngleForces(angle, mol, true);
                    }

                    //Loop Torsion List
                    foreach (TorsionTerm torsion in mol.torsionTerms)
                    {
                        calcTorsionForces(torsion, mol, true);
                    }

                    //Loop Bond List
                    foreach (HardSphereTerm hsTerm in mol.hsTerms)
                    {
                        calcRepForces(hsTerm, mol, true);
                    }
                }
            }
            switch (currentMethod)
            {
                case Method.Euler:
                    eulerIntegration();
                    break;
                case Method.Verlet:
                    verletIntegration();
                    break;
                case Method.RungeKutta:
                case Method.Heun:
                case Method.Ralston:
                    RK_Integration();
                    break;
                case Method.MidPoint:
                    midpointIntegration();
                    break;
                case Method.SteepestDescent:
                    steepestDecent();
                    break;
            }
            applyMovements();
            if (do_measurment)
            {
                measureConvergence();
            }
        }
    }

    /// <summary>
    /// Applies the force field to all molecules, bonds, atoms etc.
    /// </summary>
    void applyFF_onConvergence()
    {
        bool notConverged = true;
        while (notConverged)
        {
            // Forces Pass 1
            foreach (var mol in GlobalCtrl.Singleton.List_curMolecules)
            {
                //Loop Bond List
                foreach (BondTerm bond in mol.bondTerms)
                {
                    calcBondForces(bond, mol);
                }

                //Loop Angle List
                foreach (AngleTerm angle in mol.angleTerms)
                {
                    calcAngleForces(angle, mol);
                }

                //Loop Torsion List
                foreach (TorsionTerm torsion in mol.torsionTerms)
                {
                    calcTorsionForces(torsion, mol);
                }

                //Loop Bond List
                foreach (HardSphereTerm hsTerm in mol.hsTerms)
                {
                    calcRepForces(hsTerm, mol);
                }
            }
            //eulerIntegration();
            verletIntegration();
            notConverged = !isConverged();
            applyMovements();
        }
        //UnityEngine.Debug.Log("[FF] Jump");
    }

    /// <summary>
    /// calculate non-bonding repulsion forces
    /// </summary>
    /// <param name="hsTerm"></param>
    /// <param name="mol"></param>
    /// <param name="second_pass"></param>
    void calcRepForces(HardSphereTerm hsTerm, Molecule mol, bool second_pass = false)
    {
        if (LogLevel >= 1000) FFlog.WriteLine("calcRepForces for {0} - {1}", hsTerm.Atom1, hsTerm.Atom2);
        //bond vector
        Vector3 rij;
        if (second_pass)
        {
            rij = (mol.FFposition[hsTerm.Atom1] + alpha * mol.FFforces[hsTerm.Atom1] * RKtimeFactor / mol.atomList[hsTerm.Atom1].m_data.m_mass) - (mol.FFposition[hsTerm.Atom2] + alpha * mol.FFforces[hsTerm.Atom2] * RKtimeFactor / mol.atomList[hsTerm.Atom2].m_data.m_mass);
        }
        else
        {
            rij = mol.FFposition[hsTerm.Atom1] - mol.FFposition[hsTerm.Atom2];
        }
        var rij_mag = rij.magnitude; //rij.magnitude / mol.transform.localScale.x;
        float delta = rij_mag - hsTerm.Rcrit * repulsionScale;
        //Debug.Log(string.Format("D nb term {0,4} {1,4}: rij = {2,14:f2}", hsTerm.Atom1, hsTerm.Atom2, rij.magnitude));
        //UnityEngine.Debug.Log($"[FF] HS delta {delta}; rj_mag {rij_mag}; Rcrit {hsTerm.Rcrit * repulsionScale}");
        if (delta < 0.0f)
        {
            float frep = -hsTerm.kH * delta;
            //UnityEngine.Debug.Log($"[FF] HS nb term {hsTerm.Atom1} {hsTerm.Atom2}: rij = {rij.magnitude} crit = {hsTerm.Rcrit}");
            mol.FFforces[hsTerm.Atom1] += frep * rij.normalized;
            mol.FFforces[hsTerm.Atom2] -= frep * rij.normalized;
        }
    }

    /// <summary>
    /// calculate bond forces
    /// </summary>
    /// <param name="bond"></param>
    /// <param name="mol"></param>
    /// <param name="second_pass"></param>
    void calcBondForces(BondTerm bond, Molecule mol, bool second_pass = false)
    {
        if (LogLevel >= 1000) FFlog.WriteLine("calcBondForces for {0} - {1}", bond.Atom1, bond.Atom2);
        //bond vector
        Vector3 rb;
        if (second_pass)
        {
            rb = (mol.FFposition[bond.Atom1] + alpha * mol.FFforces[bond.Atom1] * RKtimeFactor / mol.atomList[bond.Atom1].m_data.m_mass) - (mol.FFposition[bond.Atom2] + alpha * mol.FFforces[bond.Atom2] * RKtimeFactor / mol.atomList[bond.Atom2].m_data.m_mass);
        }
        else
        {
            rb = mol.FFposition[bond.Atom1] - mol.FFposition[bond.Atom2];
        }

        var rb_mag = rb.magnitude; //(rb.magnitude / mol.transform.localScale.x);
        //force on this bond vector
        float delta = rb_mag - bond.eqDist;
        float fb = -bond.kBond * delta;
        if (LogLevel >= 1000) FFlog.WriteLine("dist: {0,12:f3}  dist0: {1,12:f3}  --  force = {2,14:f5} ", rb.magnitude, bond.eqDist, fb);
        //separate the forces on the two atoms
        Vector3 fc1 = fb * rb.normalized;
        Vector3 fc2 = -fb * rb.normalized;

        if (LogLevel >= 10000)
        {
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", bond.Atom1, fc1.x, fc1.y, fc1.z));
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", bond.Atom2, fc2.x, fc2.y, fc2.z));
        }

        if (second_pass)
        {
            mol.FFforces_pass2[bond.Atom1] += fc1;
            mol.FFforces_pass2[bond.Atom2] += fc2;
        }
        else
        {
            mol.FFforces[bond.Atom1] += fc1;
            mol.FFforces[bond.Atom2] += fc2;
        }
    }

    /// <summary>
    /// calculate angle forces
    /// </summary>
    /// <param name="angle"></param>
    /// <param name="mol"></param>
    /// <param name="second_pass"></param>
    void calcAngleForces(AngleTerm angle, Molecule mol, bool second_pass = false)
    {
        if (LogLevel >= 1000) FFlog.WriteLine("calcAngleForces for {0} - {1} - {2}", angle.Atom1, angle.Atom2, angle.Atom3);
        Vector3 rb1;
        Vector3 rb2;
        if (second_pass)
        {
            rb1 = (mol.FFposition[angle.Atom1] + alpha * mol.FFforces[angle.Atom1] * RKtimeFactor / mol.atomList[angle.Atom1].m_data.m_mass) - (mol.FFposition[angle.Atom2] + alpha * mol.FFforces[angle.Atom2] * RKtimeFactor / mol.atomList[angle.Atom2].m_data.m_mass);
            rb2 = (mol.FFposition[angle.Atom3] + alpha * mol.FFforces[angle.Atom3] * RKtimeFactor / mol.atomList[angle.Atom3].m_data.m_mass) - (mol.FFposition[angle.Atom2] + alpha * mol.FFforces[angle.Atom2] * RKtimeFactor / mol.atomList[angle.Atom2].m_data.m_mass);
        }
        else
        {
            rb1 = mol.FFposition[angle.Atom1] - mol.FFposition[angle.Atom2];
            rb2 = mol.FFposition[angle.Atom3] - mol.FFposition[angle.Atom2];
        }

        float cosAlpha = (Vector3.Dot(rb1, rb2)) / (Vector3.Magnitude(rb1) * Vector3.Magnitude(rb2));
        /*  Alpha- dependency
            float mAlpha = angle.kAngle * (Mathf.Acos(cosAlpha) * (180.0f / Mathf.PI) - angle.Aeq);
      
            Vector3 fI = (mAlpha / (Vector3.Magnitude(rb1) * Mathf.Sqrt(1.0f - cosAlpha * cosAlpha))) * ((rb2 / Vector3.Magnitude(rb2)) - cosAlpha * (rb1 / Vector3.Magnitude(rb1)));
            Vector3 fK = (mAlpha / (Vector3.Magnitude(rb2) * Mathf.Sqrt(1.0f - cosAlpha * cosAlpha))) * ((rb1 / Vector3.Magnitude(rb1)) - cosAlpha * (rb2 / Vector3.Magnitude(rb2)));
            Vector3 fJ = -fI - fK;
        */
        float mAlpha;

        if (!Mathf.Approximately(angle.eqAngle, 180f))
        {
            mAlpha = angle.kAngle * (cosAlpha - Mathf.Cos(angle.eqAngle * (Mathf.PI / 180.0f)));
        }
        else
        {
            mAlpha = angle.kAngle;
        }

        Vector3 fI = -mAlpha/ Vector3.Magnitude(rb1) * ((rb2 / Vector3.Magnitude(rb2)) - cosAlpha * (rb1 / Vector3.Magnitude(rb1)));
        Vector3 fK = -mAlpha/ Vector3.Magnitude(rb2) * ((rb1 / Vector3.Magnitude(rb1)) - cosAlpha * (rb2 / Vector3.Magnitude(rb2)));
        Vector3 fJ = -fI - fK;

        //if(debug) Debug.Log(string.Format("angle {0} - {1} - {2} : {3,12:f3}  angle0 {4,12:f3}  --  moment = {5,14:f5} ", angle.Atom1, angle.Atom2, angle.Atom3, Mathf.Acos(cosAlpha) * (180.0f / Mathf.PI), angle.Aeq, mAlpha)); //??!!

        if (LogLevel >= 1000) FFlog.WriteLine("angle: {0,12:f3}  angle0: {1,12:f3}  --  moment = {2,14:f5} ", Mathf.Acos(cosAlpha) * (180.0f / Mathf.PI), angle.eqAngle, mAlpha);

        if (LogLevel >= 10000)
        {
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", angle.Atom1, fI.x, fI.y, fI.z));
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", angle.Atom2, fJ.x, fJ.y, fJ.z));
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", angle.Atom3, fK.x, fK.y, fK.z));
        }

        if (second_pass)
        {
            mol.FFforces_pass2[angle.Atom1] += fI;
            mol.FFforces_pass2[angle.Atom2] += fJ;
            mol.FFforces_pass2[angle.Atom3] += fK;
        }
        else
        {
            mol.FFforces[angle.Atom1] += fI;
            mol.FFforces[angle.Atom2] += fJ;
            mol.FFforces[angle.Atom3] += fK;
        }

    }


    /// <summary>
    /// calculate torsion forces
    /// </summary>
    /// <param name="torsion"></param>
    /// <param name="mol"></param>
    /// <param name="second_pass"></param>
    void calcTorsionForces(TorsionTerm torsion, Molecule mol, bool second_pass = false)
     {

        //if (LogLevel >= 1000) FFlog.WriteLine("calcTorsionForces for {0} - {1} - {2} - {3}", torsion.Atom1, torsion.Atom2, torsion.Atom3, torsion.Atom4);
        Vector3 rij;
        Vector3 rkj;
        Vector3 rkl; 
        if (second_pass)
        {
            rij = (mol.FFposition[torsion.Atom1] + alpha * mol.FFforces[torsion.Atom1] * RKtimeFactor / mol.atomList[torsion.Atom1].m_data.m_mass) - (mol.FFposition[torsion.Atom2] + alpha * mol.FFforces[torsion.Atom2] * RKtimeFactor / mol.atomList[torsion.Atom2].m_data.m_mass);
            rkj = (mol.FFposition[torsion.Atom3] + alpha * mol.FFforces[torsion.Atom3] * RKtimeFactor / mol.atomList[torsion.Atom3].m_data.m_mass) - (mol.FFposition[torsion.Atom2] + alpha * mol.FFforces[torsion.Atom2] * RKtimeFactor / mol.atomList[torsion.Atom2].m_data.m_mass);
            rkl = (mol.FFposition[torsion.Atom3] + alpha * mol.FFforces[torsion.Atom3] * RKtimeFactor / mol.atomList[torsion.Atom3].m_data.m_mass) - (mol.FFposition[torsion.Atom4] + alpha * mol.FFforces[torsion.Atom4] * RKtimeFactor / mol.atomList[torsion.Atom4].m_data.m_mass);
        }
        else
        {
            rij = mol.FFposition[torsion.Atom1] - mol.FFposition[torsion.Atom2];
            rkj = mol.FFposition[torsion.Atom3] - mol.FFposition[torsion.Atom2];
            rkl = mol.FFposition[torsion.Atom3] - mol.FFposition[torsion.Atom4];
        }

        Vector3 mNormal = Vector3.Cross(rij, rkj);
        Vector3 mNormalized = Vector3.Cross(rij, rkj).normalized;
        Vector3 nNormal = Vector3.Cross(rkj, rkl);
        Vector3 nNormalized = Vector3.Cross(rkj, rkl).normalized;

        float cosAlpha = Mathf.Min(1.0f,Mathf.Max(-1.0f,(Vector3.Dot(nNormalized,mNormalized))));

        //phi0: position for minimum often with phi0=0
        float phi0 = torsion.eqAngle * Mathf.PI / 180f;
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
            
        if (LogLevel >= 1000) FFlog.WriteLine("torsion: {0,12:f3}  {1,12:f3}  {2,12:f3} ", torsion.eqAngle, cosAlpha, phi);

        if (second_pass)
        {
            mol.FFforces_pass2[torsion.Atom1] += fti;
            mol.FFforces_pass2[torsion.Atom2] += ftj;
            mol.FFforces_pass2[torsion.Atom3] += ftk;
            mol.FFforces_pass2[torsion.Atom4] += ftl;
        }
        else
        {
            mol.FFforces[torsion.Atom1] += fti;
            mol.FFforces[torsion.Atom2] += ftj;
            mol.FFforces[torsion.Atom3] += ftk;
            mol.FFforces[torsion.Atom4] += ftl;
        }


    }

    /// <summary>
    /// Computes the root mean square force in a molecule.
    /// </summary>
    /// <param name="mol"></param>
    /// <returns>the root mean square force in a molecule</returns>
    float RMSforce(Molecule mol)
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
                sqsum += mol.FFforces[iAtom].x * mol.FFforces[iAtom].x;
                sqsum += mol.FFforces[iAtom].y * mol.FFforces[iAtom].y;
                sqsum += mol.FFforces[iAtom].z * mol.FFforces[iAtom].z;
            }
            sqsum = Mathf.Sqrt(sqsum) / nAtoms;
            return sqsum;
        }
    }
    /// <summary>
    /// Turn forces into movements and apply sanity checks.
    /// Uses the Euler method for integration.
    /// </summary>
    void eulerIntegration()
    {

        // force -> momentum change: divide by mass
        // momentum change to position change: apply time factor
        foreach (var mol in GlobalCtrl.Singleton.List_curMolecules)
        {

            // check for too long steps:
            float MaxMove = 10f;
            float moveMaxNorm = 0f; // norm of movement vector
            for (int iAtom = 0; iAtom < mol.atomList.Count; iAtom++)
            {
                // negative masses flag a fixed atom
                if (!mol.atomList[iAtom].isGrabbed && mol.atomList[iAtom].m_data.m_mass > 0)
                {
                    mol.FFforces[iAtom] *= EulerTimeFactor / mol.atomList[iAtom].m_data.m_mass;
                    float moveNorm = Vector3.Magnitude(mol.FFforces[iAtom]);
                    moveMaxNorm = Mathf.Max(moveMaxNorm, moveNorm);
                    //Debug.Log($"[ForceField] Current Force {mol.FFforces[iAtom].x} {mol.FFforces[iAtom].y} {mol.FFforces[iAtom].z}");
                }
                else
                {
                    mol.FFforces[iAtom] = Vector3.zero;
                }
            }

            float scaleMove = 1f;
            if (moveMaxNorm > MaxMove)
            {
                scaleMove = MaxMove / moveMaxNorm;
                if (LogLevel >= 100) FFlog.WriteLine("moveMaxNorm was {0:f3} - scaling by {1:f10}", moveMaxNorm, scaleMove);
            }

            // update position and total movement:
            for (int iAtom = 0; iAtom < mol.atomList.Count; iAtom++)
            {
                mol.FFmovement[iAtom] += mol.FFforces[iAtom] * scaleMove;
                mol.FFposition[iAtom] += mol.FFforces[iAtom] * scaleMove;
            }
        }
    }

    void verletIntegration()
    {
        foreach (var mol in GlobalCtrl.Singleton.List_curMolecules)
        {
            // update position and total movement:
            for (int iAtom = 0; iAtom < mol.atomList.Count; iAtom++)
            {
                // negative masses flag a fixed atom
                if (!mol.atomList[iAtom].isGrabbed && mol.atomList[iAtom].m_data.m_mass > 0)
                {
                    var current_pos = mol.FFposition[iAtom];
                    mol.FFposition[iAtom] = 2.0f * current_pos - mol.FFlastPosition[iAtom] + mol.FFforces[iAtom] * Mathf.Pow(SVtimeFactor, 2.0f) / mol.atomList[iAtom].m_data.m_mass;
                    mol.FFmovement[iAtom] += mol.FFposition[iAtom] - current_pos;
                    mol.FFlastPosition[iAtom] = current_pos;
                }
                else
                {
                    mol.FFmovement[iAtom] = Vector3.zero;
                }
            }
        }
    }

    void RK_Integration()
    {
        foreach (var mol in GlobalCtrl.Singleton.List_curMolecules)
        {
            // update position and total movement:
            for (int iAtom = 0; iAtom < mol.atomList.Count; iAtom++)
            {
                // negative masses flag a fixed atom
                if (!mol.atomList[iAtom].isGrabbed && mol.atomList[iAtom].m_data.m_mass > 0)
                {
                    var current_pos = mol.FFposition[iAtom];
                    mol.FFposition[iAtom] = current_pos + ((1f - 1f/(2f*alpha)) * mol.FFforces[iAtom] * RKtimeFactor)/ mol.atomList[iAtom].m_data.m_mass + (mol.FFforces_pass2[iAtom] * RKtimeFactor)/ (2f*alpha*mol.atomList[iAtom].m_data.m_mass);

                    mol.FFmovement[iAtom] += mol.FFposition[iAtom] - current_pos;
                    mol.FFlastPosition[iAtom] = current_pos;
                }
                else
                {
                    mol.FFmovement[iAtom] = Vector3.zero;
                }
            }
        }
    }

    void midpointIntegration()
    {
        foreach (var mol in GlobalCtrl.Singleton.List_curMolecules)
        {

            // update position and total movement:
            for (int iAtom = 0; iAtom < mol.atomList.Count; iAtom++)
            {
                // negative masses flag a fixed atom
                if (!mol.atomList[iAtom].isGrabbed && mol.atomList[iAtom].m_data.m_mass > 0)
                {
                    var current_pos = mol.FFposition[iAtom];

                    mol.FFposition[iAtom] = (mol.FFforces[iAtom] * 2f * MPtimeFactor) / mol.atomList[iAtom].m_data.m_mass + mol.FFlastlastPosition[iAtom];

                    mol.FFmovement[iAtom] += mol.FFposition[iAtom] - current_pos;
                    mol.FFlastlastPosition[iAtom] = mol.FFlastPosition[iAtom];
                    mol.FFlastPosition[iAtom] = current_pos;
                }
                else
                {
                    mol.FFmovement[iAtom] = Vector3.zero;
                }
            }
        }
    }

    void steepestDecent()
    {
        foreach (var mol in GlobalCtrl.Singleton.List_curMolecules)
        {

            //    // update position and total movement:
            for (int iAtom = 0; iAtom < mol.atomList.Count; iAtom++)
            {
                // negative masses flag a fixed atom
                if (!mol.atomList[iAtom].isGrabbed && mol.atomList[iAtom].m_data.m_mass > 0 && mol.FFforces[iAtom].magnitude > SDepsilon)
                {
                    if (Vector3.Dot(mol.FFforces[iAtom], mol.FFlastForces[iAtom]) < 0)
                    {
                        mol.FFlambda[iAtom] *= SDstepReduction;
                    }
                    if (chARpackExtensions.CalculateCosineSimilarity(mol.FFforces[iAtom], mol.FFlastForces[iAtom]) > SDsimilarity)
                    {
                        mol.FFlambda[iAtom] *= SDstepIncrease;
                    }
                    var current_pos = mol.FFposition[iAtom];

                    mol.FFposition[iAtom] = current_pos + mol.FFlambda[iAtom] * mol.FFforces[iAtom];

                    mol.FFmovement[iAtom] += mol.FFposition[iAtom] - current_pos;
                    mol.FFlastForces[iAtom] = mol.FFforces[iAtom];
                }
                else
                {
                    mol.FFmovement[iAtom] = Vector3.zero;
                    mol.FFlambda[iAtom] = SDdefaultLambda;
                }
            }

        // update position and total movement:
        //for (int iAtom = 0; iAtom < mol.atomList.Count; iAtom++)
        //{
        //    // negative masses flag a fixed atom
        //    if (!mol.atomList[iAtom].isGrabbed && mol.atomList[iAtom].m_data.m_mass > 0 && mol.FFforces[iAtom].magnitude > SDepsilon)
        //    {
        //        var current_pos = mol.FFposition[iAtom];

        //        var pos_diff = mol.FFposDiff[iAtom];
        //        var f_diff = mol.FFforces[iAtom] - mol.FFlastForces[iAtom];

        //        var lambda = 0.001f* 0.5f * (Vector3.Dot(pos_diff, f_diff) / f_diff.magnitude + pos_diff.magnitude / Vector3.Dot(pos_diff, f_diff));

        //        if (float.IsNaN(lambda)) lambda = 0.001f;
        //        //var lambda = Vector3.Dot(pos_diff, f_diff) / Vector3.Dot(f_diff, f_diff);
        //        //var lambda = Vector3.Dot(pos_diff, pos_diff) / Vector3.Dot(pos_diff, f_diff);

        //        mol.FFposition[iAtom] = current_pos - lambda * mol.FFforces[iAtom];

        //        mol.FFmovement[iAtom] += mol.FFposition[iAtom] - current_pos;
        //        mol.FFlastForces[iAtom] = mol.FFforces[iAtom];
        //        mol.FFposDiff[iAtom] = mol.FFposition[iAtom] - current_pos;
        //    }
        //    else
        //    {
        //        mol.FFmovement[iAtom] = Vector3.zero;
        //        mol.FFlambda[iAtom] = SDdefaultLambda;
        //    }
        //}
        }
    }


    void RKAS_Integration_1D()
    {
        foreach (var mol in GlobalCtrl.Singleton.List_curMolecules)
        {

            // update position and total movement:
            for (int iAtom = 0; iAtom < mol.atomList.Count; iAtom++)
            {
                // negative masses flag a fixed atom
                if (!mol.atomList[iAtom].isGrabbed && mol.atomList[iAtom].m_data.m_mass > 0)
                {
                    var current_pos = mol.FFposition[iAtom];

                    var rk_result = current_pos + mol.FFforces_pass2[iAtom] * mol.FFtimeStep[iAtom].x;
                    var euler_result = current_pos + mol.FFforces[iAtom] * mol.FFtimeStep[iAtom].x;

                    var error = Mathf.Sqrt(RKc * Mathf.Abs(Vector3.Distance(euler_result, rk_result)));
                    if (iAtom == 0) UnityEngine.Debug.Log($"[Error estimate 1D] {error}");

                    mol.FFtimeStep[iAtom] = Vector3.one * Mathf.Min(RKstepMax, Mathf.Max(error, RKstepMin));
                    mol.FFposition[iAtom] = current_pos + mol.FFforces_pass2[iAtom] * mol.FFtimeStep[iAtom].x;

                    mol.FFmovement[iAtom] += mol.FFposition[iAtom] - current_pos;
                    mol.FFlastPosition[iAtom] = current_pos;
                }
                else
                {
                    mol.FFmovement[iAtom] = Vector3.zero;
                }
            }
        }
    }

    void RKAS_Integration_3D()
    {
        foreach (var mol in GlobalCtrl.Singleton.List_curMolecules)
        {

            // update position and total movement:
            for (int iAtom = 0; iAtom < mol.atomList.Count; iAtom++)
            {
                // negative masses flag a fixed atom
                if (!mol.atomList[iAtom].isGrabbed && mol.atomList[iAtom].m_data.m_mass > 0)
                {
                    var current_pos = mol.FFposition[iAtom];

                    var rk_result = current_pos + (mol.FFforces[iAtom].multiply(mol.FFtimeStep[iAtom].pow(2.0f))) / (2.0f * mol.atomList[iAtom].m_data.m_mass);
                    var euler_result = current_pos + (mol.FFforces[iAtom].multiply(mol.FFtimeStep[iAtom])) / mol.atomList[iAtom].m_data.m_mass;
                    var error = RKc * (euler_result - rk_result).abs().sqrt(); // constant c for adjustments

                    if (iAtom == 0) UnityEngine.Debug.Log($"[Error estimate 3D] {error}");

                    mol.FFtimeStep[iAtom] = error.max(RKstepMin).min(RKstepMax);
                    mol.FFposition[iAtom] = current_pos + (mol.FFforces[iAtom].multiply(mol.FFtimeStep[iAtom].pow(2.0f))) / (2.0f * mol.atomList[iAtom].m_data.m_mass);

                    mol.FFmovement[iAtom] = mol.FFposition[iAtom] - current_pos;
                    mol.FFlastPosition[iAtom] = current_pos;
                }
                else
                {
                    mol.FFmovement[iAtom] = Vector3.zero;
                }
            }
        }
    }

    /// <summary>
    /// Applies the movements calculated from the force field to all molecules.
    /// </summary>
    void applyMovements()
    {
        // momentum change to position change: apply time factor
        foreach (var mol in GlobalCtrl.Singleton.List_curMolecules)
        {
            for (int iAtom = 0; iAtom < mol.atomList.Count; iAtom++)
            {
                if (float.IsFinite(mol.FFmovement[iAtom].x)) {
                    var atom = mol.atomList.ElementAtOrDefault(iAtom);
                    if (atom.isGrabbed)
                    {
                        atom.transform.localPosition += (mol.FFmovement[iAtom] * scalingfactor) * atom.transform.localScale.x;
                    }
                    else
                    {
                        atom.transform.localPosition += mol.FFmovement[iAtom] * scalingfactor;
                    }
                }
                else
                {
                    //do small random moves
                    mol.atomList.ElementAtOrDefault(iAtom).transform.localPosition += new Vector3(UnityEngine.Random.Range(-0.01f, 0.01f), UnityEngine.Random.Range(-0.01f, 0.01f), UnityEngine.Random.Range(-0.01f, 0.01f));
                }
                mol.FFposition[iAtom] = mol.transform.InverseTransformPoint(mol.atomList.ElementAtOrDefault(iAtom).transform.position) * (1f / scalingfactor);
                mol.FFmovement[iAtom] = Vector3.zero;
                mol.FFforces[iAtom] = Vector3.zero;
                mol.FFforces_pass2[iAtom] = Vector3.zero;
            }
        }
    }

    // connections between atoms get scaled new as soon as the position of an atom gets updated
    public void scaleConnections()
    {
        // For each Molecule in scene
        // For each Bond in Molecule
        // Calculate distance between atoms
        // Scale, transform position, LookAt
        
        foreach(Molecule mol in GlobalCtrl.Singleton.List_curMolecules)
        {
            foreach(Bond bond in mol.bondList)
            {
                Atom a1 = mol.atomList.ElementAtOrDefault(bond.atomID1);
                Atom a2 = mol.atomList.ElementAtOrDefault(bond.atomID2);
                float offset1 = a1.m_data.m_radius * scalingfactor*GlobalCtrl.atomScale*GlobalCtrl.scale  * 0.8f * mol.transform.localScale.x;
                float offset2 = a2.m_data.m_radius * scalingfactor*GlobalCtrl.atomScale*GlobalCtrl.scale  * 0.8f * mol.transform.localScale.x;
                float distance = (Vector3.Distance(a1.transform.position, a2.transform.position)- offset1 - offset2) / mol.transform.localScale.x;
                bond.transform.localScale = new Vector3(bond.transform.localScale.x, bond.transform.localScale.y, distance);
                Vector3 pos1 = Vector3.MoveTowards(a1.transform.position, a2.transform.position, offset1);
                Vector3 pos2 = Vector3.MoveTowards(a2.transform.position, a1.transform.position, offset2);
                bond.transform.position = (pos1 + pos2) / 2;
                bond.transform.LookAt(a2.transform.position);
            }
        }
    }
    
}


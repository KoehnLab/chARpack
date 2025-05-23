using chARpack.Types;
#if CHARPACK_DEBUG_CONSOLE
using IngameDebugConsole;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace chARpack
{
    public class Morph : MonoBehaviour
    {
        private static Morph _singleton;
        public static Morph Singleton
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
                    Debug.Log($"[{nameof(Morph)}] Instance already exists, destroying duplicate!");
                    Destroy(value.gameObject);
                }

            }
        }

        private void Awake()
        {
            Singleton = this;
        }

        bool opacitySeparate = false;
        //Dictionary<Molecule, List<Tuple<Atom, Vector3>>> orig3Dpositions = new Dictionary<Molecule, List<Tuple<Atom, Vector3>>>();
        private void Start()
        {
#if CHARPACK_DEBUG_CONSOLE
            DebugLogConsole.AddCommand("morphSF3D", "Morphs all Molecule2D", morphSFto3D);
            DebugLogConsole.AddCommand("morphSF2D", "Morphs all Molecule2D", morphSFto2D);
            DebugLogConsole.AddCommand("morphMol2D", "Morphs all Molecule2D", morphMolTo2D);
            DebugLogConsole.AddCommand("morphMol3D", "Morphs all Molecule2D", morphMolTo3D);
            DebugLogConsole.AddCommand("morph_option_stepped", "First sets position then opacity", setOptionStepped);
            DebugLogConsole.AddCommand("morph_option_continous", "Position and Opacity at the same time", SetOptionContinous);
            DebugLogConsole.AddCommand("debug_morph_control", "Debug function for ", debugMorphControl);
#endif
        }

        public void setOptionStepped()
        {
            opacitySeparate = true;
            EventManager.Singleton.SetInterpolationState(true);
        }

        public void SetOptionContinous()
        {
            opacitySeparate = false;
            EventManager.Singleton.SetInterpolationState(false);
        }

        public void debugMorphControl()
        {
            StartCoroutine(debugMorphControlSubroutine());
        }

        public void morphSFto3D()
        {
            StartCoroutine(morphSFto3DSubroutine());
        }

        public void morphSFto2D()
        {
            StartCoroutine(morphSFto2DSubroutine());
        }

        public void morphMolTo2D()
        {
            StartCoroutine(morphMolTo2DSubroutine());
        }

        public void morphMolTo3D()
        {
            StartCoroutine(morphMolTo3DSubroutine());
        }

        private IEnumerator debugMorphControlSubroutine()
        {
            var duration = SettingsData.transitionAnimationDuration * 5f;
            foreach (var mol2d in Molecule2D.molecules)
            {
                float elapsedTime = 0f;
                float amount = 0f;
                while (elapsedTime < duration)
                {
                    elapsedTime += Time.fixedDeltaTime;
                    amount = elapsedTime / duration;
                    controlMolMorph(mol2d.molReference, mol2d, amount);
                    yield return null; // wait for next frame
                }

                elapsedTime = 0f;
                amount = 0f;
                while (elapsedTime < duration)
                {
                    elapsedTime += Time.fixedDeltaTime;
                    amount = 1f - elapsedTime / duration;
                    controlMolMorph(mol2d.molReference, mol2d, amount);
                    yield return null; // wait for next frame
                }
            }
        }

        private IEnumerator morphSFto3DSubroutine()
        {
            AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            var duration = SettingsData.transitionAnimationDuration * 5f;
            foreach (var mol2d in Molecule2D.molecules)
            {

                // make real molecule transparent
                mol2d.molReference.setOpacity(0f);
                var list = new List<Triple<Atom2D, Vector3, Vector3>>();
                foreach (var a in mol2d.Atoms)
                {
                    list.Add(new Triple<Atom2D, Vector3, Vector3>(a, a.transform.position, a.atomReference.transform.position));
                }


                float elapsedTime = 0f;
                while (elapsedTime < duration)
                {
                    // Increment elapsed time
                    elapsedTime += Time.fixedDeltaTime;
                    foreach (var entry in list)
                    {


                        // Calculate the normalized time (0 to 1)
                        float normalizedTime = Mathf.Clamp01(elapsedTime / duration);

                        // Evaluate the curve at the normalized time
                        float curveValue = animationCurve.Evaluate(normalizedTime);

                        // Interpolate between start and end points
                        entry.Item1.transform.position = Vector3.Lerp(entry.Item2, entry.Item3, curveValue);
                    }
                    yield return null; // wait for next frame
                }

                elapsedTime = 0f;
                while (elapsedTime < duration)
                {
                    // Increment elapsed time
                    elapsedTime += Time.fixedDeltaTime;

                    // Calculate the normalized time (0 to 1)
                    float normalizedTime = Mathf.Clamp01(elapsedTime / duration);

                    // Evaluate the curve at the normalized time
                    float curveValue = animationCurve.Evaluate(normalizedTime);

                    // Interpolate between start and end points
                    var opacity = Mathf.Lerp(0f, 1f, curveValue);
                    mol2d.setOpacity(1f - opacity);
                    mol2d.molReference.setOpacity(opacity);

                    yield return null; // wait for next frame
                }
            }
        }

        private IEnumerator morphSFto2DSubroutine()
        {
            AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            var duration = SettingsData.transitionAnimationDuration * 5f;
            foreach (var mol2d in Molecule2D.molecules)
            {

                // make real molecule transparent
                mol2d.molReference.setOpacity(1f);
                var list = new List<Triple<Atom2D, Vector3, Vector3>>();
                foreach (var a in mol2d.Atoms)
                {
                    list.Add(new Triple<Atom2D, Vector3, Vector3>(a, a.transform.localPosition, a.atomReference.structure_coords));
                }

                float elapsedTime = 0f;
                if (opacitySeparate)
                {
                    while (elapsedTime < duration)
                    {
                        // Increment elapsed time
                        elapsedTime += Time.fixedDeltaTime;

                        // Calculate the normalized time (0 to 1)
                        float normalizedTime = Mathf.Clamp01(elapsedTime / duration);

                        // Evaluate the curve at the normalized time
                        float curveValue = animationCurve.Evaluate(normalizedTime);

                        // Interpolate between start and end points
                        var opacity = Mathf.Lerp(0f, 1f, curveValue);
                        mol2d.setOpacity(opacity);
                        mol2d.molReference.setOpacity(1f - opacity);

                        yield return null; // wait for next frame
                    }
                }


                elapsedTime = 0f;
                while (elapsedTime < duration)
                {
                    // Increment elapsed time
                    elapsedTime += Time.fixedDeltaTime;
                    // Calculate the normalized time (0 to 1)
                    float normalizedTime = Mathf.Clamp01(elapsedTime / duration);

                    // Evaluate the curve at the normalized time
                    float curveValue = animationCurve.Evaluate(normalizedTime);
                    foreach (var entry in list)
                    {
                        // Interpolate between start and end points
                        entry.Item1.transform.localPosition = Vector3.Lerp(entry.Item2, entry.Item3, curveValue);
                    }
                    if (!opacitySeparate)
                    {
                        var opacity = Mathf.Lerp(0f, 1f, curveValue);
                        mol2d.setOpacity(opacity);
                        mol2d.molReference.setOpacity(1f - opacity);
                    }
                    yield return null; // wait for next frame
                }
            }
        }

        public void set2Dactive(Molecule mol, Molecule2D mol2d)
        {
            ForceField.Singleton.enableForceFieldMethod(false);
            //saveOriginalPosition(mol);

            mol2d.setOpacity(1f);
            mol.setOpacity(0f);
            reset2Dpositions(mol2d);

            foreach (var a in mol2d.Atoms)
            {
                // Interpolate between start and end points
                var target_pos = mol2d.molReference.transform.InverseTransformPoint(mol2d.transform.TransformPoint(a.initialLocalPosition));
                a.atomReference.transform.localPosition = target_pos;
            }
        }

        public void set3Dactive(Molecule mol, Molecule2D mol2d)
        {
            mol2d.setOpacity(0f);
            mol.setOpacity(1f);
            reset3Dpositions(mol, mol2d);

            ForceField.Singleton.enableForceFieldMethod(true);
        }

        public void controlMolMorph(Molecule mol, Molecule2D mol2d, float value)
        {
            Assert.IsNotNull(mol);
            Assert.IsNotNull(mol2d);
            ForceField.Singleton.enableForceFieldMethod(false);
            AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

            float posCurveValue;
            float opaCurveValue;

            if (opacitySeparate)
            {
                value = 1f - value;
                posCurveValue = value > 0.5f ? animationCurve.Evaluate(2f * (value - 0.5f)) : 0f;
                opaCurveValue = value < 0.5f ? Mathf.Lerp(0f, 1f, 2f * value) : 1f;
            }
            else
            {
                value = 1f - value;
                posCurveValue = animationCurve.Evaluate(value);
                opaCurveValue = Mathf.Lerp(0f, 1f, value);
            }

            foreach (var a in mol2d.Atoms)
            {
                // Interpolate between start and end points
                var orig_pos = a.atomReference.originalPosition.Value;
                var target_pos = mol2d.molReference.transform.InverseTransformPoint(mol2d.transform.TransformPoint(a.initialLocalPosition));
                a.atomReference.transform.localPosition = Vector3.Lerp(orig_pos, target_pos, posCurveValue);
            }

            // inverted interpolatio, so the representations morph into each other
            mol2d.setOpacity(opaCurveValue);
            mol.setOpacity(1f - opaCurveValue);
        }

        private IEnumerator morphMolTo2DSubroutine()
        {
            ForceField.Singleton.enableForceFieldMethod(false);
            //saveOriginalPositions();
            AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            var duration = SettingsData.transitionAnimationDuration * 5f;
            foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
            {
                var mol2d = Molecule2D.molecules.Find(m => m.molReference == mol);
                if (mol2d != null)
                {

                    // make real molecule transparent
                    mol2d.setOpacity(0f);
                    mol.setOpacity(1f);
                    reset2Dpositions(mol2d);

                    float elapsedTime = 0f;
                    if (opacitySeparate)
                    {
                        while (elapsedTime < duration)
                        {
                            // Increment elapsed time
                            elapsedTime += Time.fixedDeltaTime;

                            // Calculate the normalized time (0 to 1)
                            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);

                            // Evaluate the curve at the normalized time
                            float curveValue = animationCurve.Evaluate(normalizedTime);

                            // Interpolate between start and end points
                            var opacity = Mathf.Lerp(0f, 1f, curveValue);
                            mol2d.setOpacity(opacity);
                            mol.setOpacity(1f - opacity);

                            yield return null; // wait for next frame
                        }
                    }

                    elapsedTime = 0f;
                    while (elapsedTime < duration)
                    {
                        // Increment elapsed time
                        elapsedTime += Time.fixedDeltaTime;
                        // Calculate the normalized time (0 to 1)
                        float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
                        // Evaluate the curve at the normalized time
                        float curveValue = animationCurve.Evaluate(normalizedTime);
                        foreach (var a in mol2d.Atoms)
                        {
                            // Interpolate between start and end points
                            //var orig_pos = orig3Dpositions[mol].Find(e => e.Item1 == a.atomReference);
                            var orig_pos = a.atomReference.originalPosition.Value;
                            var target_pos = mol2d.molReference.transform.InverseTransformPoint(mol2d.transform.TransformPoint(a.initialLocalPosition));
                            a.atomReference.transform.localPosition = Vector3.Lerp(orig_pos, target_pos, curveValue);
                        }
                        if (!opacitySeparate)
                        {
                            // Interpolate between start and end points
                            var opacity = Mathf.Lerp(0f, 1f, curveValue);
                            mol2d.setOpacity(opacity);
                            mol.setOpacity(1f - opacity);
                        }
                        yield return null; // wait for next frame
                    }
                }
            }
        }

        private IEnumerator morphMolTo3DSubroutine()
        {
            ForceField.Singleton.enableForceFieldMethod(false);
            AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            var duration = SettingsData.transitionAnimationDuration * 5f;
            foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
            {
                var mol2d = Molecule2D.molecules.Find(m => m.molReference == mol);
                if (mol2d != null)
                {

                    // make real molecule transparent
                    mol2d.setOpacity(1f);
                    mol.setOpacity(0f);

                    float elapsedTime = 0f;
                    while (elapsedTime < duration)
                    {
                        // Increment elapsed time
                        elapsedTime += Time.fixedDeltaTime;
                        // Calculate the normalized time (0 to 1)
                        float normalizedTime = Mathf.Clamp01(elapsedTime / duration);

                        // Evaluate the curve at the normalized time
                        float curveValue = animationCurve.Evaluate(normalizedTime);
                        foreach (var a in mol2d.Atoms)
                        {
                            // Interpolate between start and end points
                            //var target_pos = orig3Dpositions[mol].Find(e => e.Item1 == a.atomReference);
                            var target_pos = a.atomReference.originalPosition.Value;
                            var current_pos = mol2d.molReference.transform.InverseTransformPoint(mol2d.transform.TransformPoint(a.initialLocalPosition));
                            a.atomReference.transform.localPosition = Vector3.Lerp(current_pos, target_pos, curveValue);
                        }
                        if (!opacitySeparate)
                        {
                            var opacity = Mathf.Lerp(0f, 1f, curveValue);
                            mol.setOpacity(opacity);
                            mol2d.setOpacity(1f - opacity);
                        }
                        yield return null; // wait for next frame
                    }

                    elapsedTime = 0f;
                    if (opacitySeparate)
                    {
                        while (elapsedTime < duration)
                        {
                            // Increment elapsed time
                            elapsedTime += Time.fixedDeltaTime;

                            // Calculate the normalized time (0 to 1)
                            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);

                            // Evaluate the curve at the normalized time
                            float curveValue = animationCurve.Evaluate(normalizedTime);

                            // Interpolate between start and end points
                            var opacity = Mathf.Lerp(0f, 1f, curveValue);
                            mol.setOpacity(opacity);
                            mol2d.setOpacity(1f - opacity);

                            yield return null; // wait for next frame
                        }
                    }
                }
            }
        }

        //private void saveOriginalPositions()
        //{

        //    foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
        //    {
        //        if (!orig3Dpositions.ContainsKey(mol))
        //        {
        //            var tmp = new List<Tuple<Atom, Vector3>>();
        //            foreach (var atom in mol.atomList)
        //            {
        //                tmp.Add(new Tuple<Atom, Vector3>(atom, atom.transform.localPosition));
        //            }
        //            orig3Dpositions.Add(mol, tmp);
        //        }
        //    }
        //}

        //private void saveOriginalPosition(Molecule mol)
        //{
        //    if (!orig3Dpositions.ContainsKey(mol))
        //    {
        //        var tmp = new List<Tuple<Atom, Vector3>>();
        //        foreach (var atom in mol.atomList)
        //        {
        //            tmp.Add(new Tuple<Atom, Vector3>(atom, atom.transform.localPosition));
        //        }
        //        orig3Dpositions.Add(mol, tmp);
        //    }
        //}

        private void reset2Dpositions(Molecule2D mol2d)
        {
            foreach (var a in mol2d.Atoms)
            {
                a.transform.localPosition = a.atomReference.structure_coords;
            }
        }

        private void reset3Dpositions(Molecule mol, Molecule2D mol2d)
        {
            foreach (var a in mol2d.Atoms)
            {
                //var target_pos = orig3Dpositions[mol].Find(e => e.Item1 == a.atomReference);
                var target_pos = a.atomReference.originalPosition.Value;
                a.atomReference.transform.localPosition = target_pos;
            }
        }
    }
}

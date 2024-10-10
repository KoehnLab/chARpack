using chARpack.Types;
using IngameDebugConsole;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace chARpack
{
    public class Morph : MonoBehaviour
    {
        bool opacityAfter = false;
        Dictionary<Molecule, List<Tuple<Atom, Vector3>>> orig3Dpositions = new Dictionary<Molecule, List<Tuple<Atom, Vector3>>>();
        private void Start()
        {
            DebugLogConsole.AddCommand("morph3D", "Morphs all Molecule2D", morph3D);
            DebugLogConsole.AddCommand("morph2D", "Morphs all Molecule2D", morph2D);
            DebugLogConsole.AddCommand("morphMol", "Morphs all Molecule2D", morphMolTo2D);
        }

        public void morph3D()
        {
            StartCoroutine(morphSFto3DSubroutine());
        }

        public void morph2D()
        {
            StartCoroutine(morphSFto2DSubroutine());
        }

        public void morphMolTo2D()
        {
            StartCoroutine(morphMolTo2DSubroutine());
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
                foreach (var a in mol2d.atoms)
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
                foreach (var a in mol2d.atoms)
                {
                    list.Add(new Triple<Atom2D, Vector3, Vector3>(a, a.transform.localPosition, a.atomReference.structure_coords));
                }

                float elapsedTime = 0f;
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

                elapsedTime = 0f;
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
                        entry.Item1.transform.localPosition = Vector3.Lerp(entry.Item2, entry.Item3, curveValue);
                    }
                    yield return null; // wait for next frame
                }
            }
        }

        private IEnumerator morphMolTo2DSubroutine()
        {
            ForceField.Singleton.enableForceFieldMethod(false);
            saveOriginalPositions();
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
                    while (elapsedTime < duration)
                    {
                        // Increment elapsed time
                        elapsedTime += Time.fixedDeltaTime;
                        foreach (var a in mol2d.atoms)
                        {
                            // Calculate the normalized time (0 to 1)
                            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);

                            // Evaluate the curve at the normalized time
                            float curveValue = animationCurve.Evaluate(normalizedTime);

                            // Interpolate between start and end points
                            var orig_pos = orig3Dpositions[mol].Find(e => e.Item1 == a.atomReference);
                            a.atomReference.transform.position = Vector3.Lerp(orig_pos.Item2, a.transform.position, curveValue);
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
                        mol2d.setOpacity(opacity);
                        mol.setOpacity(1f - opacity);

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

                        // Interpolate between start and end points
                        var opacity = Mathf.Lerp(0f, 1f, curveValue);
                        mol.setOpacity(opacity);
                        mol2d.setOpacity(1f - opacity);

                        yield return null; // wait for next frame
                    }


                    elapsedTime = 0f;
                    while (elapsedTime < duration)
                    {
                        // Increment elapsed time
                        elapsedTime += Time.fixedDeltaTime;
                        foreach (var a in mol2d.atoms)
                        {
                            // Calculate the normalized time (0 to 1)
                            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);

                            // Evaluate the curve at the normalized time
                            float curveValue = animationCurve.Evaluate(normalizedTime);

                            // Interpolate between start and end points
                            var orig_pos = orig3Dpositions[mol].Find(e => e.Item1 == a.atomReference);
                            a.atomReference.transform.position = Vector3.Lerp(a.transform.position, orig_pos.Item2, curveValue);
                        }
                        yield return null; // wait for next frame
                    }
                }
            }
        }

        private void saveOriginalPositions()
        {

            foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
            {
                if (!orig3Dpositions.ContainsKey(mol))
                {
                    var tmp = new List<Tuple<Atom, Vector3>>();
                    foreach (var atom in mol.atomList)
                    {
                        tmp.Add(new Tuple<Atom, Vector3>(atom, atom.transform.position));
                    }
                    orig3Dpositions.Add(mol, tmp);
                }
            }
        }

        private void reset2Dpositions(Molecule2D mol2d)
        {
            foreach (var a in mol2d.atoms)
            {
                a.transform.localPosition = a.atomReference.structure_coords;
            }
        }
    }
}

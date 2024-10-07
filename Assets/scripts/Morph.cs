using chARpack.Types;
using IngameDebugConsole;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace chARpack
{
    public class Morph : MonoBehaviour
    {
        private void Start()
        {
            DebugLogConsole.AddCommand("morph3D", "Morphs all Molecule2D", morph3D);
            DebugLogConsole.AddCommand("morph2D", "Morphs all Molecule2D", morph2D);
        }

        public void morph3D()
        {
            StartCoroutine(morph3DSubroutine());
        }

        public void morph2D()
        {
            StartCoroutine(morph2DSubroutine());
        }

        private static IEnumerator morph3DSubroutine()
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

        private static IEnumerator morph2DSubroutine()
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
    }
}

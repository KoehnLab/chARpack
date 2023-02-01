using System.Threading;
using System.Threading.Tasks;
using OpenBabel;

public static class OpenBabelForceField
{
    /// <summary>
    /// Minimise a structure using OpenBabel for a given number of steps
    /// </summary>
    public static OBMol MinimiseStructure(OBMol mol,
                                          int steps,
                                          string forcefieldName = "mmff94s")
    {
        var forcefield = GetForceField(forcefieldName, mol);

        forcefield.SteepestDescent(steps);

        if (!forcefield.GetCoordinates(mol))
            OpenBabelSetup.ThrowOpenBabelException("Failed to get conformer");

        return mol;
    }

    /// <summary>
    /// Minimise a structure continuously, calling onUpdate every 10 steps. Cancelling
    /// the task will finish the minimisation.
    /// </summary>
    //public static async Task<BondsAndParticles> ContinuousMinimiseAsync(
    //    BondsAndParticles system,
    //    string forcefieldName,
    //    Action<BondsAndParticles> onUpdate,
    //    CancellationToken token)
    //{
    //    if (!OpenBabelSetup.IsOpenBabelAvailable)
    //        OpenBabelSetup.ThrowOpenBabelNotFoundError();

    //    var mol = system.AsOBMol();
    //    var forcefield = GetForceField(forcefieldName, mol);

    //    // A mutex to ensure that only one thread uses the forcefield at once
    //    var semaphore = new SemaphoreSlim(1, 1);

    //    // A thread which repeatedly calls SteepestDescent on the forcefield
    //    var thread = new Thread(() =>
    //    {
    //        while (!token.IsCancellationRequested)
    //        {
    //            try
    //            {
    //                semaphore.Wait(token);
    //                forcefield.SteepestDescent(1);
    //            }
    //            catch (OperationCanceledException)
    //            {
    //                break;
    //            }
    //            finally
    //            {
    //                semaphore.Release();
    //            }
    //            Thread.Sleep(1);
    //        }
    //    });
    //    thread.Start();

    //    // A task which waits for the forcefield to be free, and copies the latest coordinates
    //    // to mol
    //    async Task ReadUpdatedParticle()
    //    {
    //        await semaphore.WaitAsync();
    //        try
    //        {
    //            if (!forcefield.GetCoordinates(mol))
    //                OpenBabelSetup.ThrowOpenBabelException("Failed to get conformer");
    //        }
    //        finally
    //        {
    //            semaphore.Release();
    //        }
    //    }

    //    try
    //    {
    //        // As long as the task continues, try getting regular updates
    //        // on the current minimised structures
    //        while (!token.IsCancellationRequested)
    //        {
    //            try
    //            {
    //                await Task.Delay(50, token);
    //            }
    //            catch (TaskCanceledException cancelled)
    //            {
    //                break;
    //            }

    //            await ReadUpdatedParticle();
    //            onUpdate(mol.AsBondsAndParticles());
    //        }

    //        // Get the final result
    //        await ReadUpdatedParticle();
    //        return mol.AsBondsAndParticles();
    //    }
    //    finally
    //    {
    //        thread.Abort();
    //    }
    //}

    /// <summary>
    /// Attempt to find and setup the given forcefield for a given molecule.
    /// </summary>
    public static OBForceField GetForceField(string name, OBMol mol)
    {
        var forcefield = OBForceField.FindForceField(name);
        if (forcefield == null)
            OpenBabelSetup.ThrowOpenBabelException("Failed to load force field");
        if (!forcefield.Setup(mol))
            OpenBabelSetup.ThrowOpenBabelException("Failed to setup force field");
        return forcefield;
    }
}


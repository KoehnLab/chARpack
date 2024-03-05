using chARpackStructs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadMoleculeAction : IUndoableAction
{
    private List<cmlData> loadData;

    public LoadMoleculeAction(List<cmlData> cmlData)
    {
        this.loadData = cmlData;
    }
    public void Execute()
    {

    }

    public void Undo()
    {
        foreach (var mol in loadData) {
            GlobalCtrl.Singleton.deleteMolecule(mol.moleID, false);
        }
    }
}

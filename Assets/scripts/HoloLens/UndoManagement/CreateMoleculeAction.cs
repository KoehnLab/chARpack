using StructClass;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateMoleculeAction : IUndoableAction
{
    private cmlData cmlData;
    private ushort m_id;

    public CreateMoleculeAction(cmlData cmlData)
    {
        this.cmlData = cmlData;
    }
    public void Execute()
    {
        m_id = GlobalCtrl.Singleton.BuildMoleculeFromCML(cmlData);
    }

    public void Undo()
    {
        GlobalCtrl.Singleton.deleteMolecule(m_id, false);
        EventManager.Singleton.Undo();
    }
}

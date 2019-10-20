using UnityEngine;
using XNode;
using PlanetaryTerrain.Noise;
using PlanetaryTerrain;
using System.IO;

public class FromSavedNode : PTNode
{

    [Output] public ModuleWrapper output;

    public string path;
    public override object GetValue(XNode.NodePort port)
    {
        return new ModuleWrapper(GetModule());
    }

    internal override Module GetModule()
    {

        try
        {
            FileStream stream = new FileStream(path, FileMode.Open);
            var module = Utils.DeserializeModule(stream);
            stream.Dispose();
            return module;
        }
        catch
        {
            return new Const(0f);
        }
    }

}



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IATK;

[ExecuteInEditMode]
public class MapHValues : MonoBehaviour
{
    public DataSource myDataSource;
    public bool updateData;

    private void Update()
    {
        if (updateData)
        {
            setBigMeshChannel();
            print("Set BigMesh h channel.");
        }
        updateData = false;
    }

    void setBigMeshChannel()
    {
        var bm = GetComponentInChildren<View>().BigMesh;
        bm.MapUVChannel(0, (int)AbstractVisualisation.NormalChannel.Custom, myDataSource["h"].Data);
        print(myDataSource["h"].Data[100]);
        print(bm.GetUVs(3));
    }
}

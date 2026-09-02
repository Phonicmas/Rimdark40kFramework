using UnityEngine;
using Verse;

namespace Core40k;

public class DecorationDrawData : IExposable
{
    public RotationalData defaultData = new(Rot4.Invalid);
    
    public RotationalData dataNorth = new(Rot4.North);
    
    public RotationalData dataEast = new(Rot4.East);
    
    public RotationalData dataSouth = new(Rot4.South);

    public RotationalData dataWest = new(Rot4.West);

    public ref RotationalData GetData(Rot4 rotation)
    {
        if (rotation == Rot4.North)
        {
            return ref dataNorth;
        }
        if (rotation == Rot4.South)
        {
            return ref dataSouth;
        }
        if (rotation == Rot4.East)
        {
            return ref dataEast;
        }
        if (rotation == Rot4.West)
        {
            return ref dataWest;
        }
        
        return ref defaultData;
    }

    public string GetDataForXml()
    {
        var text = string.Empty;
        
        if (defaultData.DifferentFromDefault(out var dText))
        {
            text += $"<defaultData>{dText}</defaultData>";
        }
        if (dataNorth.DifferentFromDefault(out var nText))
        {
            text += $"<dataNorth>{nText}</dataNorth>";
        }
        if (dataSouth.DifferentFromDefault(out var sText))
        {
            text += $"<dataSouth>{sText}</dataSouth>";
        }
        if (dataEast.DifferentFromDefault(out var eText))
        {
            text += $"<dataEast>{eText}</dataEast>";
        }
        if (dataWest.DifferentFromDefault(out var wText))
        {
            text += $"<dataWest>{wText}</dataWest>";
        }

        return text;
    }

    public void CopyFrom(DecorationDrawData data)
    {
        InternalCopy(ref defaultData, data.defaultData);
        InternalCopy(ref dataNorth, data.dataNorth);
        InternalCopy(ref dataSouth, data.dataSouth);
        InternalCopy(ref dataEast, data.dataEast);
        InternalCopy(ref dataWest, data.dataWest);
    }

    private static void InternalCopy(ref RotationalData data, RotationalData targetData)
    {
        data.layer = targetData.layer;
        data.scale = targetData.scale;
        data.offset = targetData.offset;
        data.rotation = targetData.rotation;
    }
    
    public void ExposeData()
    {
        Scribe_Deep.Look(ref dataNorth, "dataNorth");
        Scribe_Deep.Look(ref dataEast, "dataEast");
        Scribe_Deep.Look(ref dataSouth, "dataSouth");
        Scribe_Deep.Look(ref dataWest, "dataWest");
        Scribe_Deep.Look(ref defaultData, "defaultData");
    }
    
    public class RotationalData : IExposable
    {
        public RotationalData()
        {
            
        }

        public RotationalData(Rot4 rotation)
        {
            this.rotation = rotation;
        }
        
        public Rot4 rotation = Rot4.Invalid;

        public Vector3 offset = Vector3.zero;

        public float layer = 0;
        
        public float scale = 1f;

        public RotationalData GetCopy()
        {
            var data = new RotationalData(rotation)
            {
                layer = layer,
                offset = offset,
                scale = scale
            };

            return data;
        }
        
        public bool DifferentFromDefault(out string text)
        {
            text = string.Empty;
            var any = false;

            if (offset != Vector3.zero)
            {
                text += $"<offset>{offset}</offset>";
                any = true;
            }
            if (layer != 0)
            {
                text += $"<layer>{layer}</layer>";
                any = true;
            }
            if (!Mathf.Approximately(scale, 1f))
            {
                text += $"<scale>{scale}</scale>";
                any = true;
            }
            
            return any;
        }
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref scale, "scale");
            Scribe_Values.Look(ref offset, "offset");
            Scribe_Values.Look(ref layer, "layer");
            Scribe_Values.Look(ref rotation, "rotation");
        }
    }
}
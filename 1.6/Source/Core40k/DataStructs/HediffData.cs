using System.Xml;
using Verse;

namespace Core40k;

public class HediffData
{
    public HediffDef hediffDef = null;
    public BodyPartDef bodyPartDef = null;
    public float initialSeverity = 0f;

    public HediffData()
    {
        
    }
    
    public HediffData(HediffDef hediffDef, BodyPartDef bodyPartDef, float initialSeverity)
    {
        this.hediffDef = hediffDef;
        this.bodyPartDef = bodyPartDef;
        this.initialSeverity = initialSeverity;
    }
    
    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "hediffDef", xmlRoot.Name, null, null, typeof(HediffDef));
        foreach (XmlNode xmlNode in xmlRoot.ChildNodes)
        {
            if (xmlNode is not XmlElement childNode)
            {
                continue;
            }

            switch (childNode.Name)
            {
                case "bodyPartDef":
                    DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "bodyPartDef", childNode.InnerText);
                    break;
                case "initialSeverity":
                    initialSeverity = ParseHelper.FromString<float>(childNode.FirstChild.Value);
                    break;
                default:
                    Log.Warning("Error in HediffData, " + childNode.Name + " not recognized as a valid field.");
                    break;
            }
        }
    }
}
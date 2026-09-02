using System.Xml;
using Verse;

namespace Core40k;

public class RankData
{
    public RankDef rankDef;

    public float daysAs = 0f;

    public RankData()
    {
    }

    public RankData(RankDef rankDef, int daysAs)
    {
        this.rankDef = rankDef;
        this.daysAs = daysAs;
    }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "rankDef", xmlRoot.Name, null, null, typeof(RankDef));
        if (xmlRoot.FirstChild != null)
        {
            daysAs = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
        }
    }
}
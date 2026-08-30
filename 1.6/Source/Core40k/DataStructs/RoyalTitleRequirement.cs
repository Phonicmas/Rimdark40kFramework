using System.Xml;
using RimWorld;
using Verse;

namespace Core40k;

public class RoyalTitleRequirement
{
    public RoyalTitleDef title;

    //Restrict the requirement to a single factions titles. Null means any faction.
    public FactionDef faction;

    //False: this title, or anything more senior, fulfills the requirement.
    //True: the pawn has to hold exactly this title.
    public bool exactTitle = false;

    //Only look at titles that are currently in effect,
    //ignoring titles suspended by faction relations.
    public bool inEffectOnly = true;

    public bool MetBy(Pawn pawn)
    {
        if (title == null)
        {
            return true;
        }

        var royalty = pawn?.royalty;
        if (royalty == null)
        {
            return false;
        }

        var heldTitles = inEffectOnly ? royalty.AllTitlesInEffectForReading : royalty.AllTitlesForReading;
        if (heldTitles.NullOrEmpty())
        {
            return false;
        }

        foreach (var heldTitle in heldTitles)
        {
            if (heldTitle?.def == null)
            {
                continue;
            }

            if (faction != null && heldTitle.faction?.def != faction)
            {
                continue;
            }

            if (exactTitle ? heldTitle.def == title : heldTitle.def.seniority >= title.seniority)
            {
                return true;
            }
        }

        return false;
    }

    public string Label
    {
        get
        {
            if (title == null)
            {
                return "";
            }

            var label = title.GetLabelCapForBothGenders();

            if (!exactTitle)
            {
                label = "BEWH.Framework.RankSystem.TitleOrHigher".Translate(label);
            }

            if (faction != null)
            {
                label = "BEWH.Framework.RankSystem.TitleOfFaction".Translate(label, faction.LabelCap);
            }

            return label;
        }
    }

    //Supports both the shorthand <li>Knight</li> and the full node form.
    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        var mayRequire = xmlRoot.Attributes?["MayRequire"]?.Value;

        if (xmlRoot.ChildNodes.Count == 1 && xmlRoot.FirstChild.NodeType == XmlNodeType.Text)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "title", xmlRoot.FirstChild.Value, mayRequire);
            return;
        }

        foreach (XmlNode node in xmlRoot.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (node.Name)
            {
                case "title":
                    DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "title", node.InnerText, mayRequire);
                    break;
                case "faction":
                    DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "faction", node.InnerText, mayRequire);
                    break;
                case "exactTitle":
                    exactTitle = ParseHelper.FromString<bool>(node.InnerText);
                    break;
                case "inEffectOnly":
                    inEffectOnly = ParseHelper.FromString<bool>(node.InnerText);
                    break;
                default:
                    Log.Error("Unknown field " + node.Name + " in RoyalTitleRequirement.");
                    break;
            }
        }
    }
}

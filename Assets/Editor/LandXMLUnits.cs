using System;
using System.Xml;

public class LandXMLUnits
{
    public string AreaUnit { get; set; }
    public string LinearUnit { get; set; }
    public string VolumeUnit { get; set; }
    public string TemperatureUnit { get; set; }
    public string PressureUnit { get; set; }
    public string DiameterUnit { get; set; }
    public string AngularUnit { get; set; }
    public string DirectionUnit { get; set; }
    public string UnitSystem { get; set; } // "Imperial" or "Metric"

    public static LandXMLUnits Parse(XmlReader reader)
    {
        if (reader == null || reader.NodeType != XmlNodeType.Element || reader.Name != "Units")
            return null;

        // Read until we find either Imperial or Metric element
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && 
                (reader.Name == "Imperial" || reader.Name == "Metric"))
            {
                var units = new LandXMLUnits
                {
                    UnitSystem = reader.Name,  // "Imperial" or "Metric"
                    AreaUnit = reader.GetAttribute("areaUnit"),
                    LinearUnit = reader.GetAttribute("linearUnit"),
                    VolumeUnit = reader.GetAttribute("volumeUnit"),
                    TemperatureUnit = reader.GetAttribute("temperatureUnit"),
                    PressureUnit = reader.GetAttribute("pressureUnit"),
                    DiameterUnit = reader.GetAttribute("diameterUnit"),
                    AngularUnit = reader.GetAttribute("angularUnit"),
                    DirectionUnit = reader.GetAttribute("directionUnit")
                };

                // Skip to the end of Units element
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Units")
                        break;
                }

                return units;
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Units")
            {
                break;
            }
        }

        return null;
    }

    public float ConvertLinearValue(float value)
    {
        // Convert to meters (Unity's default unit)
        switch (LinearUnit?.ToLowerInvariant())
        {
            case "foot":
                return value * 0.3048f;
            case "meter":
                return value;
            case "millimeter":
                return value * 0.001f;
            case "kilometer":
                return value * 1000f;
            case "yard":
                return value * 0.9144f;
            case "inch":
                return value * 0.0254f;
            default:
                return value; // Return as-is if unit is unknown
        }
    }

    public override string ToString()
    {
        return $"Units System: {UnitSystem}\n" +
               $"Linear Unit: {LinearUnit}\n" +
               $"Area Unit: {AreaUnit}\n" +
               $"Volume Unit: {VolumeUnit}\n" +
               $"Temperature Unit: {TemperatureUnit}\n" +
               $"Pressure Unit: {PressureUnit}\n" +
               $"Diameter Unit: {DiameterUnit}\n" +
               $"Angular Unit: {AngularUnit}\n" +
               $"Direction Unit: {DirectionUnit}";
    }
}

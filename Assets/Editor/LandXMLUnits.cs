using System;
using System.Xml.Linq;

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

    public static LandXMLUnits Parse(XElement unitsElement)
    {
        if (unitsElement == null)
            return null;

        // Check for Imperial or Metric system
        var imperialElement = unitsElement.Element(XName.Get("Imperial"));
        var metricElement = unitsElement.Element(XName.Get("Metric"));
        
        XElement targetElement = imperialElement ?? metricElement;
        if (targetElement == null)
            return null;

        var units = new LandXMLUnits
        {
            UnitSystem = imperialElement != null ? "Imperial" : "Metric",
            AreaUnit = targetElement.Attribute("areaUnit")?.Value,
            LinearUnit = targetElement.Attribute("linearUnit")?.Value,
            VolumeUnit = targetElement.Attribute("volumeUnit")?.Value,
            TemperatureUnit = targetElement.Attribute("temperatureUnit")?.Value,
            PressureUnit = targetElement.Attribute("pressureUnit")?.Value,
            DiameterUnit = targetElement.Attribute("diameterUnit")?.Value,
            AngularUnit = targetElement.Attribute("angularUnit")?.Value,
            DirectionUnit = targetElement.Attribute("directionUnit")?.Value
        };

        return units;
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

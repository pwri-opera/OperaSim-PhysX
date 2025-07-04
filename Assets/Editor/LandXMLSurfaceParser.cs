using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Globalization;

public class Point3D
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public string Id { get; set; }

    public override string ToString()
    {
        return $"Point {Id}: ({X}, {Y}, {Z})";
    }
}

public class Face
{
    public List<int> VertexIndices { get; set; }
    public string Neighbors { get; set; }

    public override string ToString()
    {
        return $"Face: {string.Join(", ", VertexIndices)} (Neighbors: {Neighbors})";
    }
}

public class Surface
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<Point3D> Points { get; set; }
    public List<Face> Faces { get; set; }
    public double Area2D { get; set; }
    public double Area3D { get; set; }
    public double MaxElevation { get; set; }
    public double MinElevation { get; set; }

    public Surface()
    {
        Points = new List<Point3D>();
        Faces = new List<Face>();
    }
}

public class LandXMLSurfaceParser
{
    public static LandXMLUnits Units { get; private set; }

    public static List<Surface> ParseSurfaces(string xmlFilePath)
    {
        var surfaces = new List<Surface>();
        
        // Load the XML file
        XDocument doc = XDocument.Load(xmlFilePath);
        XNamespace ns = "http://www.landxml.org/schema/LandXML-1.1";

        // Parse Units first
        var unitsElement = doc.Root?.Element(ns + "Units");
        Units = LandXMLUnits.Parse(unitsElement);
        if (Units == null)
        {
            Debug.LogWarning("No units information found in LandXML file. Using raw values.");
        }

        // Find all Surface elements
        var surfaceElements = doc.Descendants(ns + "Surface");

        foreach (var surfaceElement in surfaceElements)
        {
            var surface = new Surface
            {
                Name = surfaceElement.Attribute("name")?.Value ?? "",
                Description = surfaceElement.Attribute("desc")?.Value ?? ""
            };

            // Get the Definition element
            var definition = surfaceElement.Element(ns + "Definition");
            if (definition != null)
            {
                surface.Area2D = double.Parse(definition.Attribute("area2DSurf")?.Value ?? "0", CultureInfo.InvariantCulture);
                surface.Area3D = double.Parse(definition.Attribute("area3DSurf")?.Value ?? "0", CultureInfo.InvariantCulture);
                surface.MaxElevation = double.Parse(definition.Attribute("elevMax")?.Value ?? "0", CultureInfo.InvariantCulture);
                surface.MinElevation = double.Parse(definition.Attribute("elevMin")?.Value ?? "0", CultureInfo.InvariantCulture);

                // Parse Points
                var pointsElement = definition.Element(ns + "Pnts");
                if (pointsElement != null)
                {
                    foreach (var p in pointsElement.Elements(ns + "P"))
                    {
                        var coordinates = p.Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(c => double.Parse(c, CultureInfo.InvariantCulture))
                                         .ToList();

                        if (coordinates.Count >= 3)
                        {
                            float x = (float)coordinates[0];
                            float y = (float)coordinates[1];
                            float z = (float)coordinates[2];

                            // Convert to meters if units are available
                            if (Units != null)
                            {
                                x = Units.ConvertLinearValue(x);
                                y = Units.ConvertLinearValue(y);
                                z = Units.ConvertLinearValue(z);
                            }

                            surface.Points.Add(new Point3D
                            {
                                Id = p.Attribute("id")?.Value ?? "",
                                X = x,
                                Y = y,
                                Z = z
                            });
                        }
                    }
                }

                // Parse Faces
                var facesElement = definition.Element(ns + "Faces");
                if (facesElement != null)
                {
                    foreach (var f in facesElement.Elements(ns + "F"))
                    {
                        var indices = f.Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(int.Parse)
                                     .ToList();

                        surface.Faces.Add(new Face
                        {
                            VertexIndices = indices,
                            Neighbors = f.Attribute("n")?.Value ?? ""
                        });
                    }
                }
            }

            surfaces.Add(surface);
        }

        return surfaces;
    }

    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Please provide the path to the XML file as an argument.");
            return;
        }

        string xmlFilePath = args[0];
        try
        {
            var surfaces = ParseSurfaces(xmlFilePath);
            foreach (var surface in surfaces)
            {
                Console.WriteLine($"\nSurface: {surface.Name}");
                Console.WriteLine($"Description: {surface.Description}");
                Console.WriteLine($"2D Area: {surface.Area2D:F2}");
                Console.WriteLine($"3D Area: {surface.Area3D:F2}");
                Console.WriteLine($"Elevation Range: {surface.MinElevation:F2} to {surface.MaxElevation:F2}");
                Console.WriteLine($"Number of Points: {surface.Points.Count}");
                Console.WriteLine($"Number of Faces: {surface.Faces.Count}");
                
                // Print first few points and faces as examples
                Console.WriteLine("\nFirst 5 Points:");
                foreach (var point in surface.Points.Take(5))
                {
                    Console.WriteLine(point);
                }
                
                Console.WriteLine("\nFirst 5 Faces:");
                foreach (var face in surface.Faces.Take(5))
                {
                    Console.WriteLine(face);
                }
                Console.WriteLine("\n" + new string('-', 80));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing the XML file: {ex.Message}");
        }
    }
}

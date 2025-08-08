using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Globalization;
using System.IO;

public class Point3D
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public int Id { get; set; }

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

    public static List<Surface> ParseSurfaces(string xmlFilePath, Action<float> progressCallback)
    {
        var surfaces = new List<Surface>();
        Surface currentSurface = null;

        using (var reader = XmlReader.Create(xmlFilePath))
        {
            string xmlns = null;

            // Find the root element and get the namespace
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "LandXML")
                {
                    xmlns = reader.GetAttribute("xmlns");
                    break;
                }
            }

            while (reader.Read())
            {
                // progress is remaining stream length / total stream length
                float progress = 0f; //(float)(reader.Length - reader.Position) / reader.Length;
                //progressCallback(progress);

                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Units":
                            Units = LandXMLUnits.Parse(reader);
                            if (Units == null)
                            {
                                Debug.LogWarning("No units information found in LandXML file. Using raw values.");
                            }
                            break;

                        case "Surface":
                            currentSurface = new Surface
                            {
                                Name = reader.GetAttribute("name") ?? "",
                                Description = reader.GetAttribute("desc") ?? ""
                            };
                            break;

                        case "Definition":
                            if (currentSurface != null)
                            {
                                currentSurface.Area2D = double.Parse(reader.GetAttribute("area2DSurf") ?? "0", CultureInfo.InvariantCulture);
                                currentSurface.Area3D = double.Parse(reader.GetAttribute("area3DSurf") ?? "0", CultureInfo.InvariantCulture);
                                currentSurface.MaxElevation = double.Parse(reader.GetAttribute("elevMax") ?? "0", CultureInfo.InvariantCulture);
                                currentSurface.MinElevation = double.Parse(reader.GetAttribute("elevMin") ?? "0", CultureInfo.InvariantCulture);
                            }
                            break;

                        case "P":
                            if (currentSurface != null)
                            {
                                int id = int.Parse(reader.GetAttribute("id") ?? "0");
                                string pointData = reader.ReadElementContentAsString();
                                var coordinates = pointData.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                                         .Select(c => double.Parse(c, CultureInfo.InvariantCulture))
                                                         .ToList();

                                if (coordinates.Count >= 3)
                                {
                                    float x = (float)coordinates[0];
                                    float y = (float)coordinates[1];
                                    float z = (float)coordinates[2];

                                    if (Units != null)
                                    {
                                        x = Units.ConvertLinearValue(x);
                                        y = Units.ConvertLinearValue(y);
                                        z = Units.ConvertLinearValue(z);
                                    }

                                    currentSurface.Points.Add(new Point3D
                                    {
                                        Id = id,
                                        X = x,
                                        Y = y,
                                        Z = z
                                    });
                                }
                            }
                            break;

                        case "F":
                            if (currentSurface != null)
                            {
                                string faceData = reader.ReadElementContentAsString();
                                var indices = faceData.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(int.Parse)
                                                   .ToList();

                                currentSurface.Faces.Add(new Face
                                {
                                    VertexIndices = indices,
                                    Neighbors = reader.GetAttribute("n") ?? ""
                                });
                            }
                            break;
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Surface")
                {
                    if (currentSurface != null)
                    {
                        surfaces.Add(currentSurface);
                        currentSurface = null;
                    }
                }
            }
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
            var surfaces = ParseSurfaces(xmlFilePath, (progress) => {
                Console.WriteLine($"Progress: {progress}");
            });
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

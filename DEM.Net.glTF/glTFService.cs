﻿using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.glTF
{
    public class glTFService : IglTFService
    {
        public void Export(Model model, string outputFolder, string modelName)
        {
            try
            {
                Stopwatch.StartNew();

                Directory.CreateDirectory(outputFolder);
                var jsonSerializer = new Newtonsoft.Json.JsonSerializer
                {
                    Formatting = Newtonsoft.Json.Formatting.Indented,
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                };


                var filename = $"{modelName}.gltf";

                using (var data = new Data($"{modelName}.bin"))
                {
                    // Passes the desired properties to the runtime layer, which then coverts that data into
                    // a gltf loader object, ready to create the model
                    var converter = new GLTFConverter { CreateInstanceOverride = model.CreateSchemaInstance };
                    var gltf = converter.ConvertRuntimeToSchema(model.GLTF, data);

                    // Makes last second changes to the model that bypass the runtime layer
                    // in order to add features that don't really exist otherwise
                    model.PostRuntimeChanges(gltf);

                    // Creates the .gltf file and writes the model's data to it
                    var assetFile = Path.Combine(outputFolder, filename);
                    glTFLoader.Interface.SaveModel(gltf, assetFile);

                    // Creates the .bin file and writes the model's data to it
                    var dataFile = Path.Combine(outputFolder, data.Name);
                    File.WriteAllBytes(dataFile, data.ToArray());
                }


                Console.WriteLine("Model Creation Complete!");
                Console.WriteLine("Completed in : " + TimeSpan.FromTicks(Stopwatch.GetTimestamp()).ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                throw;
            }
        }

        public Model GenerateModel(MeshPrimitive meshPrimitive, string name)
        {
            // Create the gltf object
            Model model = new Model
            {
                GLTF = new GLTF
                {
                    Asset = new Asset
                    {
                        Generator = "glTF Asset Generator",
                        Version = "2.0",
                    },
                    Scenes = new List<Scene>
                    {
                        new Scene {
                        Nodes = new List<Node>
                        {
                            new Node
                            {
                                Mesh = new Mesh
                                {
                                    MeshPrimitives = new List<MeshPrimitive>
                                    {
                                        meshPrimitive
                                    }
                                },
                            },
                        },
                        }
                    }
                }
            };

            return model;
        }

        public MeshPrimitive GenerateTriangleMesh(HeightMap heightMap)
        {
            MeshPrimitive mesh = null;
            const int TRIANGULATION_MODE = 1; // 2
            try
            {
                if (heightMap == null || heightMap.Coordinates == null || heightMap.Coordinates.Count == 0)
                {
                    Logger.Warning("Height map is empty.");
                }
                else
                {
                    List<Vector3> positions = new List<Vector3>(heightMap.Coordinates.Select(pt => ToVector3(pt)));
                    // Basic mesh declaration
                    mesh = new MeshPrimitive()
                    {
                        Colors = heightMap.Coordinates.Select(c => new Vector4(1, 1, 1, 0))
                        ,
                        ColorComponentType = MeshPrimitive.ColorComponentTypeEnum.FLOAT
                        ,
                        ColorType = MeshPrimitive.ColorTypeEnum.VEC3
                        ,
                        Mode = MeshPrimitive.ModeEnum.TRIANGLES
                        ,
                        Positions = positions
                        ,
                        Material=new Material()
                    };

                    int capacity = ((heightMap.Width - 1) * 6) * (heightMap.Height - 1);
                    List<int> indices = new List<int>(capacity);
                    // Triangulate mesh -- anti clockwise winding
                    for (int y = 0; y < heightMap.Height; y++)
                    {
                        for (int x = 0; x < heightMap.Width; x++)
                        {
                            //Vector3 pt = ToVector3(heightMap.Coordinates[x + y * heightMap.Width]);
                            //pt.z -= mindepth;
                            //cout << x + y * stride << "-> " << pt << endl;
                            //mesh.addVertex(pt);

                            if (x < (heightMap.Width - 1) && y < (heightMap.Height - 1))
                            {
                                if (TRIANGULATION_MODE == 1)
                                {
                                    // Triangulation 1
                                    indices.Add((x + 0) + (y + 0) * heightMap.Width);
                                    indices.Add((x + 0) + (y + 1) * heightMap.Width);
                                    indices.Add((x + 1) + (y + 0) * heightMap.Width);

                                    indices.Add((x + 1) + (y + 0) * heightMap.Width);
                                    indices.Add((x + 0) + (y + 1) * heightMap.Width);
                                    indices.Add((x + 1) + (y + 1) * heightMap.Width);
                                }
                                else
                                {

                                    // Triangulation 2
                                    indices.Add((x + 0) + (y + 0) * heightMap.Width);
                                    indices.Add((x + 0) + (y + 1) * heightMap.Width);
                                    indices.Add((x + 1) + (y + 1) * heightMap.Width);

                                    indices.Add((x + 0) + (y + 0) * heightMap.Width);
                                    indices.Add((x + 1) + (y + 1) * heightMap.Width);
                                    indices.Add((x + 1) + (y + 0) * heightMap.Width);
                                }
                            }
                        }
                    }
                    mesh.Indices = indices;

                    //The number of the vertices
                    int nV = heightMap.Coordinates.Count;
                    //The number of the triangles
                    int nT = indices.Count / 3;

                    Vector3[] norm = new Vector3[nV]; //Array for the normals
                                               //Scan all the triangles. For each triangle add its
                                               //normal to norm's vectors of triangle's vertices
                    for (int t = 0; t < nT; t++)
                    {
                        //Get indices of the triangle t
                        int i1 = indices[3 * t];
                        int i2 = indices[3 * t + 1];
                        int i3 = indices[3 * t + 2];
                        //Get vertices of the triangle
                        Vector3 v1 = positions[i1];
                        Vector3 v2 = positions[i2];
                        Vector3 v3 = positions[i3];
                        //Compute the triangle's normal
                        Vector3 dir = Vector3.Normalize(Vector3.Cross(v2 - v1,v3 - v1));
                        //Accumulate it to norm array for i1, i2, i3
                        norm[i1] += dir;
                        norm[i2] += dir;
                        norm[i3] += dir;
                    }
                    //Normalize the normal's length
                    for (int i = 0; i < nV; i++)
                    {
                        norm[i] = Vector3.Normalize(norm[i]);
                    }
                    mesh.Normals = norm;

                    Debug.Assert(indices.Count == capacity);
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                throw;
            }
            return mesh;
        }

        private IEnumerable<int> Triangulate(HeightMap heightMap)
        {
            throw new NotImplementedException();
        }

        private Vector3 ToVector3(GeoPoint geoPoint)
        {
            return new Vector3((float)geoPoint.Longitude, (float)geoPoint.Latitude, (float)geoPoint.Elevation);
        }
    }
}
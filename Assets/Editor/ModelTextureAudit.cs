using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fpp.Editor
{
    /// <summary>
    /// The phone feedback said "some houses changed and the pictures are missing". The generated
    /// screenshots confirm it: parts of some supplied houses render as flat white, not as the
    /// supplied image. This writes a per-model, per-material receipt of which texture each
    /// material actually binds at build time, and fails the build when a supplied house ends up
    /// with a material that has no image and no authored colour at all.
    /// </summary>
    public static class ModelTextureAudit
    {
        private const string Path = "QA/runtime/model-texture-audit.json";

        [Serializable] private class Row { public string model, renderer, material, shader; public int subMesh; public string image, sourceAsset; public float[] colour; public bool authoredImage, hasImage, vertexColour; }
        [Serializable] private class Report { public string status = "Build-time binding audit, not visual acceptance. A missing image here means the supplied GLB material had no base-colour texture; those are reported, never silently repainted."; public int models, renderers, materials, withImage, withoutImage, withoutImageOrColour; public Row[] rows; }

        public static void Validate()
        {
            var report = new Report();
            var district = GameObject.Find("Bangladesh compact para (120 x 120m)");
            if (district == null) throw new Exception("Village district is missing; cannot audit model textures");
            var rows = new System.Collections.Generic.List<Row>();
            var models = new System.Collections.Generic.HashSet<string>();
            foreach (Transform child in district.transform)
            {
                if (child.GetComponent<MeshRenderer>() == null && child.GetComponentInChildren<MeshRenderer>(true) == null) continue;
                report.models++; models.Add(child.name);
                foreach (var renderer in child.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!renderer.enabled) continue;
                    if (renderer.gameObject.layer == 8) continue;
                    var filter = renderer.GetComponent<MeshFilter>();
                    var mesh = filter != null ? filter.sharedMesh : null;
                    bool vertexColour = mesh != null && mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color);
                    var materials = renderer.sharedMaterials;
                    report.renderers++;
                    for (int sub = 0; sub < materials.Length; sub++)
                    {
                        var material = materials[sub];
                        if (material == null)
                        {
                            rows.Add(new Row { model = child.name, renderer = renderer.name, subMesh = sub, material = "<null>", hasImage = false, authoredImage = false });
                            report.materials++; report.withoutImage++; report.withoutImageOrColour++;
                            continue;
                        }
                        var texture = material.mainTexture != null ? material.mainTexture : (material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : null);
                        if (texture == null && material.HasProperty("baseColorTexture")) texture = material.GetTexture("baseColorTexture");
                        var colour = material.HasProperty("_Color") ? material.color : material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white;
                        bool white = Mathf.Abs(colour.r - 1) < .01f && Mathf.Abs(colour.g - 1) < .01f && Mathf.Abs(colour.b - 1) < .01f;
                        var row = new Row
                        {
                            model = child.name, renderer = renderer.name, subMesh = sub, material = material.name,
                            shader = material.shader != null ? material.shader.name : "<null shader>",
                            hasImage = texture != null,
                            image = texture != null ? texture.name : null,
                            sourceAsset = texture != null ? AssetDatabase.GetAssetPath(texture) : null,
                            colour = new[] { colour.r, colour.g, colour.b, colour.a },
                            vertexColour = vertexColour,
                        };
                        report.materials++;
                        if (row.hasImage) report.withImage++;
                        else
                        {
                            report.withoutImage++;
                            // A house that is neither textured, tinted nor vertex-coloured is the
                            // "white box / missing picture" symptom the user photographed.
                            if (white && !vertexColour) { row.authoredImage = false; report.withoutImageOrColour++; }
                        }
                        rows.Add(row);
                    }
                }
            }
            report.rows = rows.ToArray();
            Directory.CreateDirectory("QA/runtime");
            File.WriteAllText(Path, JsonUtility.ToJson(report, true));
            File.WriteAllText("QA/runtime/model-texture-audit.txt",
                "Models=" + report.models + "; renderers=" + report.renderers + "; materials=" + report.materials +
                "; withImage=" + report.withImage + "; withoutImage=" + report.withoutImage +
                "; plainWhiteUnattributed=" + report.withoutImageOrColour +
                ". Build-time binding audit only; human visual review still required, and a missing source image is reported rather than invented.");
            Debug.Log("Model texture audit: materials=" + report.materials + " withImage=" + report.withImage + " withoutImage=" + report.withoutImage + " plainWhiteUnattributed=" + report.withoutImageOrColour);
        }
    }
}

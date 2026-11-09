using System.Collections.Generic;
using UnityEngine;
namespace Fpp
{
    // One-shot, generated VISUAL LOD derivatives only. Never alter source/collision/FPV meshes.
    public sealed class WorldLodMeshResidency : MonoBehaviour
    {
        public static int ReleasedMeshCount {get;private set;}
        private void Start()
        {
            ReleasedMeshCount=0;var collision=new HashSet<Mesh>();var done=new HashSet<Mesh>();
            foreach(var collider in FindObjectsOfType<MeshCollider>())if(collider.sharedMesh!=null)collision.Add(collider.sharedMesh);
            foreach(var group in FindObjectsOfType<LODGroup>())
            {
                var levels=group.GetLODs();
                for(int level=0;level<levels.Length;level++)foreach(var renderer in levels[level].renderers)
                {
                    if(renderer==null||renderer.gameObject.layer==8||(!renderer.name.StartsWith("Generated distance",System.StringComparison.Ordinal)&&!renderer.name.StartsWith("Batched geometry",System.StringComparison.Ordinal)&&renderer.GetComponent<WorldVisualSource>()==null))continue;
                    var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
                    var mesh=filter.sharedMesh;if(collision.Contains(mesh)||!done.Add(mesh)||!mesh.isReadable)continue;
                    mesh.UploadMeshData(true);ReleasedMeshCount++;
                }
            }
            // No Update/LateUpdate and no ongoing object scan.
            Destroy(this);
        }
    }
}

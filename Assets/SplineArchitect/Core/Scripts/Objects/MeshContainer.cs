// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MeshContainer.cs
//
// Author: Mikael Danielsson
// Date Created: 06-04-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEngine;
using UnityEngine.SceneManagement;

using SplineArchitect.Utility;

namespace SplineArchitect.Objects
{
    [Serializable]
    public partial class MeshContainer
    {
        public const int dataUsage = 24 + 
                                     8 + 8 + 8 + 8 + 64 + 64 + 4;

        //Stored data
        [SerializeField]
        private MeshFilter meshFilter;
        [SerializeField]
        private MeshCollider meshCollider;
        [SerializeField]
        private Mesh originMesh;
        [SerializeField]
        private long timestamp;

        //Runetime data
        [NonSerialized]
        private string resourceKey;
        [NonSerialized]
        private string resourceKeyShort;
        [NonSerialized]
        private int oldTransformsInstanceId;

        public MeshContainer(Component component)
        {
            MeshFilter meshFilter = component as MeshFilter;
            MeshCollider meshCollider = component as MeshCollider;

            if (meshFilter == null && meshCollider == null)
                throw new InvalidOperationException($"Both MeshFilter and MeshCollider cant be null.");
            else if (meshFilter != null && meshCollider != null)
                throw new InvalidOperationException($"Can't contain a valid MeshCollider and MeshFilter. Can only contain one of them.");

            this.meshFilter = meshFilter;
            this.meshCollider = meshCollider;

            if (meshFilter != null)
                originMesh = meshFilter.sharedMesh;

            if (meshCollider != null)
                originMesh = meshCollider.sharedMesh;

#if UNITY_EDITOR
            TryUpdateTimestamp();
#endif
        }

        public void SetInstanceMesh(Mesh instanceMesh)
        {
#if UNITY_EDITOR
            if (originMesh == null)
                return;

            if (instanceMesh == originMesh)
            {
                Debug.LogError("[Spline Architect] InstanceMesh and OriginMesh is the same!");
                return;
            }

            instanceMesh.name = GetResourceKey();
#endif

            if (meshFilter != null) 
                meshFilter.sharedMesh = instanceMesh;
            else if (meshCollider != null) 
                meshCollider.sharedMesh = instanceMesh;
            else
                Debug.LogError($"[Spline Architect] Could not find MeshFilter or MeshCollider for: {instanceMesh.name}");
        }

        public void SetOriginMesh(Mesh originMesh)
        {
#if UNITY_EDITOR
            string path = GeneralUtility.GetAssetPath(originMesh);
            if (path == "")
            {
                Debug.LogError($"[Spline Architect] Can't set origin mesh! {originMesh.name} does not have an asset path!");
                return;
            }
#endif

            this.originMesh = originMesh;
        }

        public Mesh GetInstanceMesh()
        {
            if (meshCollider != null && meshCollider.sharedMesh != null)
                return meshCollider.sharedMesh;
            else if (meshFilter != null && meshFilter.sharedMesh != null)
                return meshFilter.sharedMesh;
            else 
                return null;
        }

        public Mesh GetOriginMesh()
        {
            return originMesh;
        }

        public void SetInstanceMeshToOriginMesh()
        {
            if (meshFilter != null)
                meshFilter.sharedMesh = originMesh;
            else if (meshCollider != null)
                meshCollider.sharedMesh = originMesh;
            else
                Debug.LogError($"[Spline Architect] Could not find MeshFilter or MeshCollider for: {originMesh.name}");
        }

        public Component GetMeshContainerComponent()
        {
            if (meshFilter != null) 
                return meshFilter;
            else 
                return meshCollider;
        }

        public bool IsMeshFilter()
        {
            if (meshFilter != null) return true;
            return false;
        }

        public bool Contains(Component component)
        {
            if (component == null)
                return false;

            if (meshFilter == component) return true;
            if (meshCollider == component) return true;
            return false;
        }

        public bool MeshContainerExist()
        {
            if (meshCollider != null) return true;
            if (meshFilter != null) return true;
            return false;
        }

        public string GetResourceKey()
        {
            if (string.IsNullOrEmpty(resourceKey) || oldTransformsInstanceId != GetMeshContainerComponent().transform.GetInstanceID())
                UpdateResourceKey();

            return resourceKey;
        }

        public string GetResourceKeyShort()
        {
            if (string.IsNullOrEmpty(resourceKeyShort))
                UpdateResourceKey();

            return resourceKeyShort;
        }

        public void UpdateResourceKey()
        {
            oldTransformsInstanceId = GetMeshContainerComponent().transform.GetInstanceID();
            resourceKey = $"{GetMeshContainerComponent().transform.GetInstanceID()}*{originMesh.GetInstanceID()}*{timestamp}";
            resourceKeyShort = $"{GetScene().name}*{originMesh.GetInstanceID()}*{timestamp}";
        }

        public Scene GetScene()
        {
            return GetMeshContainerComponent().gameObject.scene;
        }

#if UNITY_EDITOR
        //Runtime data
        [NonSerialized]
        private int meshData = 0;
        [NonSerialized]
        private string resourceKeyCheck = null;

        public void UpdateInstanceMeshName()
        {
            Mesh instanceMesh = GetInstanceMesh();

            if (instanceMesh == null)
                return;

            if (instanceMesh == originMesh)
                return;

            instanceMesh.name = resourceKey;
        }

        public bool HasReadabilityDif()
        {
            if (originMesh.isReadable != GetInstanceMesh().isReadable)
                return true;

            return false;
        }

        public bool TryUpdateTimestamp()
        {
            if(originMesh == null)
                return false;

            string path = GeneralUtility.GetAssetPath(originMesh, true);

            if (path == "")
            {
                Debug.LogError($"[Spline Architect] Can't set timestamp! {originMesh.name} does not have an asset path!");
                return false;
            }

            long check = timestamp;
            timestamp = System.IO.File.GetLastWriteTime(path).Ticks;

            if (check != 0 && timestamp != check)
                return true;

            return false;
        }

        public float GetDataUsage()
        {
            if (!string.IsNullOrEmpty(resourceKey) && resourceKeyCheck == GetResourceKey())
                return meshData;

            Mesh mesh = GetInstanceMesh();

            if (mesh == null)
                return 0;

            meshData += mesh.vertexCount * 12;
            meshData += mesh.triangles.Length * (mesh.indexFormat == UnityEngine.Rendering.IndexFormat.UInt16 ? 2 : 4);
            meshData += mesh.normals.Length * 12;
            meshData += mesh.tangents.Length * 16;
            meshData += mesh.uv.Length * 8;
            meshData += mesh.uv2.Length * 8;
            meshData += mesh.uv3.Length * 8;
            meshData += mesh.uv4.Length * 8;
            meshData += mesh.uv5.Length * 8;
            meshData += mesh.uv6.Length * 8;
            meshData += mesh.uv7.Length * 8;
            meshData += mesh.uv8.Length * 8;
            meshData += mesh.colors.Length * 16;
            meshData += mesh.name.Length * 2;
            meshData += 24;

            resourceKeyCheck = GetResourceKey();

            return meshData;
        }

#endif
    }
}
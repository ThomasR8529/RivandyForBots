//RealToon - Smear Effect [Helper]
//MJQStudioWorks
//©2025

using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace RealToon.Script
{

    [ExecuteAlways]
    [AddComponentMenu("RealToon/Tools/Smear Effect - Helper")]

    public class SmearEffectHelper : MonoBehaviour
    {
        Queue<Vector3> recentPositions = new Queue<Vector3>();

        [HideInInspector]
        [SerializeField]
        Transform[] SubTran;

        [HideInInspector]
        [SerializeField]
        Material[] Mat;

        [HideInInspector]
        [SerializeField]
        Transform[] attac;

        [Header("Note: Smear Effect feature will be automatically enable\nOn the materials of the object/model that uses RealToon Shader.")]

        [Space(25)]

        [SerializeField]
        [Tooltip("An object to control the smear effect.")]
        public Transform SmearController;

        [Space(10)]

        [SerializeField]
        [Tooltip("How long the distorted line trails stays on the previous position.")]
        int Delay = 15;

        [SerializeField]
        [Tooltip("How large/small the trailing noise.")]
        float NoiseSize = 100;

        [SerializeField]
        [Tooltip("How tall/short the trailing noise.")]
        float TrailSize = 1.5f;

        [SerializeField]
        [Tooltip("Global multiplier to accentuate the smear trail thickness/variance.")]
        float StrengthMultiplier = 1.5f;

        [SerializeField]
        [Tooltip("Higher = reacts faster (shorter trail memory). 1 keeps the previous behavior.")]
        float Responsiveness = 1f;

        [SerializeField]
        [Tooltip("Stretch the previous position further behind to amplify smear length (>=1).")]
        float StretchMultiplier = 1.35f;

        [SerializeField]
        [Tooltip("Ignore tiny movements to avoid jitter on the smear.")]
        float MinStretchDistance = 0.01f;

        [Space(10)]

        [SerializeField]
        [Tooltip("Pause the current smear effect.")]
        bool PauseSmear = false;

        int coun_obj_wi_ralsha = 0;
        int coun_obj_mat = 0;
        int coun_obj_mat_arr = 0;

        [HideInInspector]
        [SerializeField]
        bool checkstart = true;

        bool initialized;

        string RT_Sha_Nam_URP = "Universal Render Pipeline/RealToon/Version 5/Default/Default";
        string RT_Sha_Nam_HDRP = "HDRP/RealToon/Version 5/Default";

        void EnsureInitialized()
        {
            if (checkstart)
            {
                InitStart();
                checkstart = false;
                initialized = true;
            }
            else if (!initialized && Mat != null && Mat.Length > 0)
            {
                initialized = true;
            }
        }

        public void RebuildTargets()
        {
            ClearSmearData();
            checkstart = true;
            coun_obj_wi_ralsha = 0;
            coun_obj_mat = 0;
            coun_obj_mat_arr = 0;
            attac = null;
            InitStart();
            checkstart = false;
            initialized = true;
        }

        public void ApplySettings(int delay, float noiseSize, float trailSize, float strengthMultiplier, float responsiveness, float stretchMultiplier, float minStretchDistance)
        {
            Delay = delay;
            NoiseSize = noiseSize;
            TrailSize = trailSize;
            StrengthMultiplier = strengthMultiplier;
            Responsiveness = responsiveness;
            StretchMultiplier = stretchMultiplier;
            MinStretchDistance = minStretchDistance;
        }

        void ClearSmearData()
        {
            recentPositions.Clear();

            if (Mat == null)
                return;

            foreach (Material mate in Mat)
            {
                if (mate != null)
                {
                    mate.SetVector("_ObjPosi", Vector4.zero);
                    mate.SetVector("_PrevPosition", Vector4.zero);
                }
            }
        }

        public void EnableSmear()
        {
            EnsureInitialized();
            PauseSmear = false;
        }

        public void DisableSmear(bool clearVectors = false)
        {
            EnsureInitialized();
            PauseSmear = true;
            if (clearVectors)
            {
                ClearSmearData();
            }
        }
        public IEnumerator DisableSmearInSeconds(float delay)
        {
            yield return new WaitForSeconds(delay);
            PauseSmear = true;
            ClearSmearData();
        }

        void Start()
        {
            if (checkstart == true)
            {
                InitStart();
                checkstart = false;
                initialized = true;
            }
        }

        void LateUpdate()
        {
            EnsureInitialized();

            if (SmearController != null)
            {
                if (PauseSmear != true)
                {
                    if (Mat != null)
                    {
                        Vector3 currentPos = SmearController.position;
                        recentPositions.Enqueue(currentPos);

                        int effectiveDelay = Mathf.Max(1, Mathf.RoundToInt(Delay / Mathf.Max(0.1f, Responsiveness)));

                        Vector3 basePrev = currentPos;
                        if (recentPositions.Count > effectiveDelay)
                        {
                            basePrev = recentPositions.Dequeue();
                        }
                        else if (recentPositions.Count > 1)
                        {
                            basePrev = recentPositions.Peek();
                        }

                        Vector3 prevPos = basePrev;
                        Vector3 delta = currentPos - basePrev;
                        float minDistSq = MinStretchDistance * MinStretchDistance;
                        if (delta.sqrMagnitude > minDistSq)
                        {
                            float stretch = Mathf.Max(1f, StretchMultiplier);
                            prevPos = currentPos - delta * stretch;
                        }

                        foreach (Material mate in Mat)
                        {
                            if (mate != null)
                            {
                                mate.SetVector("_ObjPosi", currentPos);
                                mate.SetVector("_PrevPosition", prevPos);
                                Set_Shad_Prop(mate);

                            }

                        }
                    }
                }
            }
        }
        void Reset()
        {
            ClearSmearData();
            checkstart = true;
            coun_obj_wi_ralsha = 0;
            coun_obj_mat = 0;
            coun_obj_mat_arr = 0;
            Res_Shad_Prop();
            attac = null;
            InitStart();
            checkstart = false;
        }

        void OnDisable()
        {
            DisableSmear(true);
        }

        /* Remove Later
        void OnDestroy()
        {
            recentPositions.Clear();
            Res_Shad_Prop();
            foreach (Material Mate in Mat)
            {
                if (Mate != null)
                {
                    if (Mate.shader.name == RT_Sha_Nam_URP || Mate.shader.name == RT_Sha_Nam_HDRP)
                    {
                        Mate.SetVector("_ObjPosi", new Vector4(0, 0, 0, 0));
                        Mate.SetVector("_PrevPosition", new Vector4(0, 0, 0, 0));
                        Mate.SetFloat("_N_F_SE", 0.0f);
                        Mate.DisableKeyword("N_F_SE_ON");
                    }
                }
            }
        }
        */

        #region Init

        void InitStart()
        {
            if (attac == null || attac.Length == 0)
            {
                attac = this.gameObject.GetComponentsInChildren<Transform>(true);
            }

            if (SmearController == null)
            {
                SmearController = this.gameObject.transform;
            }

            int x = 0;
            foreach (Transform Trans in attac)
            {

                if (Trans.GetComponent<SkinnedMeshRenderer>() == true)
                {
                    if (Trans.GetComponent<SkinnedMeshRenderer>().sharedMaterial != null)
                    {
                        if (Trans.GetComponent<SkinnedMeshRenderer>().sharedMaterial.shader.name == RT_Sha_Nam_URP || Trans.GetComponent<SkinnedMeshRenderer>().sharedMaterial.shader.name == RT_Sha_Nam_HDRP)
                        {
                            coun_obj_wi_ralsha++;
                            coun_obj_mat += Trans.GetComponent<SkinnedMeshRenderer>().sharedMaterials.Length;
                        }
                    }

                }

                if (Trans.GetComponent<MeshRenderer>() == true)
                {
                    if (Trans.GetComponent<MeshRenderer>().sharedMaterial != null)
                    {
                        if (Trans.GetComponent<MeshRenderer>().sharedMaterial.shader.name == RT_Sha_Nam_URP || Trans.GetComponent<MeshRenderer>().sharedMaterial.shader.name == RT_Sha_Nam_HDRP)
                        {
                            coun_obj_wi_ralsha++;
                            coun_obj_mat += Trans.GetComponent<MeshRenderer>().sharedMaterials.Length;
                        }

                    }
                }

            }

            SubTran = new Transform[coun_obj_wi_ralsha];

            foreach (Transform Trans in attac)
            {
                if (Trans.GetComponent<SkinnedMeshRenderer>() == true)
                {
                    if (Trans.GetComponent<SkinnedMeshRenderer>().sharedMaterial != null)
                    {
                        if (Trans.GetComponent<SkinnedMeshRenderer>().sharedMaterial.shader.name == RT_Sha_Nam_URP || Trans.GetComponent<SkinnedMeshRenderer>().sharedMaterial.shader.name == RT_Sha_Nam_HDRP)
                        {
                            SubTran[x] = Trans;
                            x++;
                        }

                    }

                }

                if (Trans.GetComponent<MeshRenderer>() == true)
                {
                    if (Trans.GetComponent<MeshRenderer>().sharedMaterial != null)
                    {
                        if (Trans.GetComponent<MeshRenderer>().sharedMaterial.shader.name == RT_Sha_Nam_URP || Trans.GetComponent<MeshRenderer>().sharedMaterial.shader.name == RT_Sha_Nam_HDRP)
                        {
                            SubTran[x] = Trans;
                            x++;
                        }
                    }
                }
            }


            Mat = new Material[coun_obj_mat];


            foreach (Transform Trans in SubTran)
            {
                if (Trans.GetComponent<SkinnedMeshRenderer>() == true)
                {
                    if (Trans.GetComponent<SkinnedMeshRenderer>().sharedMaterial != null)
                    {
                        if (Trans.GetComponent<SkinnedMeshRenderer>().sharedMaterial.shader.name == RT_Sha_Nam_URP || Trans.GetComponent<SkinnedMeshRenderer>().sharedMaterial.shader.name == RT_Sha_Nam_HDRP)
                        {
                            for (int i = 0; i < Trans.GetComponent<SkinnedMeshRenderer>().sharedMaterials.Length; i++)
                            {
                                Mat[coun_obj_mat_arr] = Trans.GetComponent<SkinnedMeshRenderer>().sharedMaterials[i];
                                coun_obj_mat_arr++;
                            }
                        }

                    }

                }

                if (Trans.GetComponent<MeshRenderer>() == true)
                {
                    if (Trans.GetComponent<MeshRenderer>().sharedMaterial != null)
                    {
                        if (Trans.GetComponent<MeshRenderer>().sharedMaterial.shader.name == RT_Sha_Nam_URP || Trans.GetComponent<MeshRenderer>().sharedMaterial.shader.name == RT_Sha_Nam_HDRP)
                        {

                            for (int i = 0; i < Trans.GetComponent<MeshRenderer>().sharedMaterials.Length; i++)
                            {
                                Mat[coun_obj_mat_arr] = Trans.GetComponent<MeshRenderer>().sharedMaterials[i];
                                coun_obj_mat_arr++;
                            }
                        }
                    }
                }

            }

            foreach (Material Mat in Mat)
            {
                if (Mat != null)
                {
                    Set_Shad_Prop(Mat);
                }
            }
            initialized = true;
        }

        #endregion

        void Set_Shad_Prop(Material Mat)
        {
            if (Mat.IsKeywordEnabled("N_F_SE_ON") == true)
            {
                Mat.SetFloat("_NoiseSize", NoiseSize * StrengthMultiplier);
                Mat.SetFloat("_TrailSize", TrailSize * StrengthMultiplier);
            }
            else if (Mat.IsKeywordEnabled("N_F_SE_ON") != true)
            {
                Mat.EnableKeyword("N_F_SE_ON");
                Mat.SetInt("_N_F_SE", 1);
            }
        }
        void Res_Shad_Prop()
        {
            NoiseSize = 100;
            TrailSize = 1.5f;
            Delay = 15;
            StrengthMultiplier = 1.5f;
            Responsiveness = 1f;
            StretchMultiplier = 1.35f;
            MinStretchDistance = 0.01f;
        }

    }

}

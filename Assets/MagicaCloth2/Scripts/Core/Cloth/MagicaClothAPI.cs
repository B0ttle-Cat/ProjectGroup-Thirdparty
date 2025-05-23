﻿// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class MagicaCloth
    {
        /// <summary>
        /// シリアライズデータ２の取得
        /// SerializeData2クラスはシステムが利用するパラメータクラスです。
        /// そのためユーザーによる変更は推奨されていません。
        /// 
        /// Acquisition of SerializedData2.
        /// The SerializeData2 class is a parameter class used by the system.
        /// Therefore, user modification is not recommended.
        /// </summary>
        /// <returns></returns>
        public ClothSerializeData2 GetSerializeData2()
        {
            return serializeData2;
        }

        /// <summary>
        /// クロスデータ構築完了後イベント
        /// Event after completion of cloth data construction.
        /// (true = Success, false = Failure)
        /// </summary>
        public Action<MagicaCloth, bool> OnBuildComplete;

        /// <summary>
        /// レンダラーメッシュ変更後イベント
        /// Renderer mesh change event.
        /// (true = Change to custom mesh, false = Change to original mesh)
        /// </summary>
        public Action<MagicaCloth, Renderer, bool> OnRendererMeshChange;

        /// <summary>
        /// 初期化を実行します
        /// すでに初期化済みの場合は何もしません。
        /// perform initialization.
        /// If already initialized, do nothing.
        /// </summary>
        public void Initialize()
        {
            if (Application.isPlaying == false)
                return;

            Process.Init();
        }

        /// <summary>
        /// コンポーネントのStart()で実行される自動ビルドを無効にします
        /// Disable automatic builds that run on the component's Start().
        /// </summary>
        public void DisableAutoBuild()
        {
            if (Application.isPlaying == false)
                return;

            Process.SetState(ClothProcess.State_DisableAutoBuild, true);
        }

        /// <summary>
        /// コンポーネントを構築し実行します
        /// すべてのデータをセットアップしたあとに呼び出す必要があります
        /// build and run the component.
        /// Must be called after setting up all data.
        /// </summary>
        /// <returns>true=start build. false=build failed.</returns>
        public bool BuildAndRun()
        {
            bool ret = false;
            bool buildComplate = true;

            try
            {
                if (Application.isPlaying == false)
                    throw new MagicaClothProcessingException();

                DisableAutoBuild();

                if (Process.IsState(ClothProcess.State_Build))
                {
                    Develop.LogError($"Already built.:{this.name}");
                    throw new MagicaClothProcessingException();
                }

                // initialize generated data.
                if (Process.GenerateInitialization() == false)
                    throw new MagicaClothProcessingException();

                // check Pre-Build
                bool usePreBuildData = serializeData2.preBuildData.UsePreBuild();

                if (usePreBuildData == false)
                {
                    // Runtime Build.
                    // setting by type.
                    switch (serializeData.clothType)
                    {
                        case ClothProcess.ClothType.BoneCloth:
                        case ClothProcess.ClothType.BoneSpring:
                            // BoneCloth用のセレクションデータの作成
                            // ただしセレクションデータが存在し、かつユーザー定義されている場合は作成しない
                            var nowSelection = serializeData2.selectionData;
                            if (nowSelection == null || nowSelection.IsValid() == false || nowSelection.IsUserEdit() == false)
                            {
                                if (Process.GenerateBoneClothSelection() == false)
                                    throw new MagicaClothProcessingException();
                            }
                            break;
                    }

                    // build and run.
                    ret = Process.StartRuntimeBuild();
                    if (ret)
                        buildComplate = false; // OnBuildCompleteはランタイム構築後に呼ばれる
                }
                else
                {
                    // pre-build
                    ret = Process.PreBuildDataConstruction();
                }
            }
            catch (MagicaClothProcessingException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                // ビルド完了イベント
                if (buildComplate)
                    OnBuildComplete?.Invoke(this, ret);
            }

            return ret;
        }

        /// <summary>
        /// コンポーネントが保持するトランスフォームを置換します。
        /// 置換先のトランスフォーム名をキーとした辞書を渡します。
        /// Replaces a component's transform.
        /// Passes a dictionary keyed by the name of the transform to be replaced.
        /// </summary>
        /// <param name="targetTransformDict">Dictionary keyed by the name of the transform to be replaced.</param>
        public void ReplaceTransform(Dictionary<string, Transform> targetTransformDict)
        {
            // コンポーネントが利用しているすべてのTransformを取得します
            var useTransformSet = new HashSet<Transform>();
            Process.GetUsedTransform(useTransformSet);

            // 置換処理用の辞書を作成
            // key:置換対象トランスフォームのインスタンスID
            // value:入れ替えるトランスフォーム
            var replaceDict = new Dictionary<int, Transform>();
            foreach (var t in useTransformSet)
            {
                if (t && targetTransformDict.ContainsKey(t.name))
                {
                    replaceDict.Add(t.GetInstanceID(), targetTransformDict[t.name]);
                }
            }

            // 置換する
            Process.ReplaceTransform(replaceDict);
        }

        /// <summary>
        /// コンポーネントが保持するすべてのトランスフォームを取得します。
        /// Gets all the transforms held by the component.
        /// </summary>
        /// <returns></returns>
        public HashSet<Transform> GetUsedTransform()
        {
            var useTransformSet = new HashSet<Transform>();
            Process.GetUsedTransform(useTransformSet);
            return useTransformSet;
        }


        /// <summary>
        /// パラメータの変更を通知
        /// 実行中にパラメータを変更した場合はこの関数を呼ぶ必要があります
        /// You should call this function if you changed parameters during execution.
        /// </summary>
        public void SetParameterChange()
        {
            if (IsValid())
            {
                Process.DataUpdate();
            }
        }

        /// <summary>
        /// タイムスケールを変更します
        /// Change the time scale.
        /// </summary>
        /// <param name="timeScale">0.0-1.0</param>
        public void SetTimeScale(float timeScale)
        {
            if (IsValid())
            {
                ref var tdata = ref MagicaManager.Team.GetTeamDataRef(Process.TeamId);
                tdata.timeScale = Mathf.Clamp01(timeScale);
            }
        }

        /// <summary>
        /// タイムスケールを取得します
        /// Get the time scale.
        /// </summary>
        /// <returns></returns>
        public float GetTimeScale()
        {
            if (IsValid())
            {
                ref var tdata = ref MagicaManager.Team.GetTeamDataRef(Process.TeamId);
                return tdata.timeScale;
            }
            else
                return 1.0f;
        }

        /// <summary>
        /// シミュレーションを初期状態にリセットします
        /// Reset the simulation to its initial state.
        /// </summary>
        /// <param name="keepPose">If true, resume while maintaining posture.</param>
        public void ResetCloth(bool keepPose = false)
        {
            if (IsValid())
            {
                ref var tdata = ref MagicaManager.Team.GetTeamDataRef(Process.TeamId);
                if (keepPose)
                {
                    // Keep
                    tdata.flag.SetBits(TeamManager.Flag_KeepTeleport, true);
                }
                else
                {
                    // Reset
                    tdata.flag.SetBits(TeamManager.Flag_Reset, true);
                    tdata.flag.SetBits(TeamManager.Flag_TimeReset, true);
                    tdata.flag.SetBits(TeamManager.Flag_CameraCullingKeep, false);
                    Process.SetState(ClothProcess.State_CameraCullingKeep, false);
                    Process.UpdateRendererUse();
                }
            }
        }

        /// <summary>
        /// 慣性の中心座標を取得します
        /// Get the center of inertia position.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetCenterPosition()
        {
            if (IsValid())
            {
                ref var cdata = ref MagicaManager.Team.GetCenterDataRef(Process.TeamId);
                return ClothTransform.TransformPoint(cdata.frameLocalPosition);
            }
            else
                return Vector3.zero;
        }

        /// <summary>
        /// 外力を加えます
        /// Add external force.
        /// </summary>
        /// <param name="forceDirection"></param>
        /// <param name="forceVelocity">(m/s)</param>
        /// <param name="fmode"></param>
        public void AddForce(Vector3 forceDirection, float forceVelocity, ClothForceMode fmode = ClothForceMode.VelocityAdd)
        {
            if (IsValid() && forceDirection.magnitude > 0.0f && forceVelocity > 0.0f && fmode != ClothForceMode.None)
            {
                ref var tdata = ref MagicaManager.Team.GetTeamDataRef(Process.TeamId);
                tdata.forceMode = fmode;
                tdata.impactForce = forceDirection.normalized * forceVelocity;
            }
        }

        /// <summary>
        /// TransformおよびMeshへの書き込みを禁止または許可します
        /// この機能を使うことでストップモーションを実装することが可能です
        /// Prevent or allow writing to Transform and Mesh.
        /// By using this function, it is possible to implement stop motion.
        /// </summary>
        /// <param name="sw">true=write disabled, false=write enabled</param>
        public void SetSkipWriting(bool sw)
        {
            if (IsValid())
            {
                Process.SetSkipWriting(sw);
            }
        }

        private RenderData GetRenderData(Renderer ren)
        {
            if (IsValid() == false || ren == null)
                return null;
            int handle = ren.GetInstanceID();
            return MagicaManager.Render.GetRendererData(handle);
        }

        /// <summary>
        /// MeshClothのオリジナルメッシュを取得します
        /// Get the original mesh of MeshCloth.
        /// </summary>
        /// <param name="ren"></param>
        /// <returns>null if not found</returns>
        public Mesh GetOriginalMesh(Renderer ren)
        {
            return GetRenderData(ren)?.originalMesh ?? null;
        }

        /// <summary>
        /// MeshClothのカスタムメッシュを取得します
        /// Get the custom mesh for MeshCloth.
        /// </summary>
        /// <param name="ren"></param>
        /// <returns>null if not found</returns>
        public Mesh GetCustomMesh(Renderer ren)
        {
            return GetRenderData(ren)?.customMesh ?? null;
        }

        /// <summary>
        /// MeshClothのSkinnedMeshRendererに設定されているカスタムボーンリストを取得します
        /// カスタムボーンリストはオリジナルのBonesからスキニングに不要なTransformをnullに設定し、
        /// また最後にレンダラーのTransformが追加されるなど加工されているので注意してください。
        /// Gets the custom bone list set for the SkinnedMeshRenderer of MeshCloth.
        /// Please note that the custom bone list has been processed by setting Transforms 
        /// that are not necessary for skinning to null from the original Bones, and adding the renderer Transform at the end.
        /// </summary>
        /// <param name="ren"></param>
        /// <returns>null if not found</returns>
        public List<Transform> GetCustomBones(Renderer ren)
        {
            return GetRenderData(ren)?.transformList ?? null;
        }
    }
}

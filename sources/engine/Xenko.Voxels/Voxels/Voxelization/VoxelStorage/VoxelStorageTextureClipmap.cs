﻿using System;
using System.Collections.Generic;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Rendering.Shadows;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    public class VoxelStorageTextureClipmap : IVoxelStorageTexture
    {
        public Vector3 ClipMapResolution;
        public int ClipMapCount;
        public int LayoutSize;
        public float VoxelSize;
        public Int3 VolumeTranslation;

        public bool DownsampleFinerClipMaps;

        public Xenko.Graphics.Texture ClipMaps = null;
        public Xenko.Graphics.Texture MipMaps = null;
        public Xenko.Graphics.Texture[] TempMipMaps = null;

        public Vector4[] PerMapOffsetScale = new Vector4[20];
        public Vector4[] PerMapOffsetScaleCurrent = new Vector4[20];
        public Vector3[] MippingOffset = new Vector3[20];

        ShaderClassSource sampler = new ShaderClassSource("VoxelStorageTextureClipmapShader");

        public void UpdateVoxelizationLayout(string compositionName)
        {

        }
        public void ApplyVoxelizationParameters(ObjectParameterKey<Texture> MainKey, ParameterCollection parameters)
        {
            parameters.Set(MainKey, ClipMaps);
        }
        string curMipMapShader = "";
        Xenko.Rendering.ComputeEffect.ComputeEffectShader VoxelMipmapSimple;
        //Memory leaks if the ThreadGroupCounts/Numbers changes (I suppose due to recompiles...?)
        //so instead cache them as seperate shaders.
        Xenko.Rendering.ComputeEffect.ComputeEffectShader[] VoxelMipmapSimpleGroups;

        Int3 ToInt3(Vector3 v)
        {
            return new Int3((int)v.X, (int)v.Y, (int)v.Z);
        }

        float Mod(float v, float m)
        {
            return ((v % m) + m) % m;//Proper modulo
        }
        Vector3 Mod(Vector3 v, Vector3 m)
        {
            return new Vector3(Mod(v.X, m.X), Mod(v.Y, m.Y), Mod(v.Z, m.Z));
        }

        public void PostProcess(RenderDrawContext drawContext, string MipMapShader)
        {
            if (VoxelMipmapSimple == null || curMipMapShader != MipMapShader)
            {
                if (VoxelMipmapSimple != null)
                    VoxelMipmapSimple.Dispose();
                VoxelMipmapSimple = new Xenko.Rendering.ComputeEffect.ComputeEffectShader(drawContext.RenderContext) { ShaderSourceName = MipMapShader };
            }
            if (VoxelMipmapSimpleGroups == null || VoxelMipmapSimpleGroups.Length != TempMipMaps.Length || curMipMapShader != MipMapShader)
            {
                if (VoxelMipmapSimpleGroups != null)
                {
                    foreach (var shader in VoxelMipmapSimpleGroups)
                    {
                        shader.Dispose();
                    }
                }
                VoxelMipmapSimpleGroups = new Xenko.Rendering.ComputeEffect.ComputeEffectShader[TempMipMaps.Length];
                for (int i = 0; i < VoxelMipmapSimpleGroups.Length; i++)
                {
                    VoxelMipmapSimpleGroups[i] = new Xenko.Rendering.ComputeEffect.ComputeEffectShader(drawContext.RenderContext) { ShaderSourceName = MipMapShader };
                }
            }
            curMipMapShader = MipMapShader;

            int offsetIndex = 0;
            //Mipmap detailed clipmaps into less detailed ones
            Vector3 totalResolution = ClipMapResolution * new Vector3(1,LayoutSize,1);
            Int3 threadGroupCounts = new Int3(32, 32, 32);
            if (DownsampleFinerClipMaps)
            {
                for (int i = 0; i < ClipMapCount - 1; i++)
                {
                    Vector3 Offset = MippingOffset[offsetIndex];
                    VoxelMipmapSimple.ThreadGroupCounts = threadGroupCounts;
                    VoxelMipmapSimple.ThreadNumbers = new Int3((int)totalResolution.X / threadGroupCounts.X, (int)totalResolution.Y / threadGroupCounts.Y, (int)totalResolution.Z / threadGroupCounts.Z);

                    VoxelMipmapSimple.Parameters.Set(VoxelMipmapSimpleKeys.ReadTex, ClipMaps);
                    VoxelMipmapSimple.Parameters.Set(VoxelMipmapSimpleKeys.WriteTex, TempMipMaps[0]);
                    VoxelMipmapSimple.Parameters.Set(VoxelMipmapSimpleKeys.ReadOffset, -(Mod(Offset,new Vector3(2))) + new Vector3(0, (int)totalResolution.Y * i, 0));
                    ((RendererBase)VoxelMipmapSimple).Draw(drawContext);

                    Offset -= Mod(Offset, new Vector3(2));
                    //Copy each axis, ignoring the top and bottom plane
                    for (int axis = 0; axis < LayoutSize; axis++)
                    {
                        int axisOffset = axis * (int)ClipMapResolution.Y;

                        Int3 CopySize = new Int3((int)ClipMapResolution.X / 2 - 2, (int)ClipMapResolution.Y / 2 - 2, (int)ClipMapResolution.Z / 2 - 2);


                        Int3 DstMinBound = new Int3((int)ClipMapResolution.X / 4 + (int)Offset.X / 2 + 1, (int)totalResolution.Y * (i + 1) + axisOffset + (int)ClipMapResolution.Y / 4 + 1 + (int)Offset.Y / 2, (int)ClipMapResolution.Z / 4 + (int)Offset.Z / 2 + 1);
                        Int3 DstMaxBound = DstMinBound + CopySize;

                        DstMaxBound = Int3.Min(DstMaxBound, new Int3((int)totalResolution.X, (int)totalResolution.Y * (i + 2), (int)totalResolution.Z));
                        DstMinBound = Int3.Min(DstMinBound, new Int3((int)totalResolution.X, (int)totalResolution.Y * (i + 2), (int)totalResolution.Z));
                        DstMaxBound = Int3.Max(DstMaxBound, new Int3(0, (int)totalResolution.Y * (i + 1), 0));
                        DstMinBound = Int3.Max(DstMinBound, new Int3(0, (int)totalResolution.Y * (i + 1), 0));

                        Int3 SizeBound = DstMaxBound - DstMinBound;

                        Int3 SrcMinBound = new Int3(1, axisOffset / 2 + 1, 1);
                        Int3 SrcMaxBound = SrcMinBound + SizeBound;

                        if (SizeBound.X > 0 && SizeBound.Y > 0 && SizeBound.Z > 0)
                        {
                            drawContext.CommandList.CopyRegion(TempMipMaps[0], 0,
                                new ResourceRegion(
                                    SrcMinBound.X, SrcMinBound.Y, SrcMinBound.Z,
                                    SrcMaxBound.X, SrcMaxBound.Y, SrcMaxBound.Z
                                ),
                                ClipMaps, 0,
                                DstMinBound.X, DstMinBound.Y, DstMinBound.Z);
                        }
                    }
                    offsetIndex++;
                }
            }
            Vector3 resolution = ClipMapResolution;
            resolution.Y *= LayoutSize;
            offsetIndex = ClipMapCount-1;
            //Mipmaps for the largest clipmap
            for (int i = 0; i < TempMipMaps.Length - 1; i++)
            {
                Vector3 Offset = MippingOffset[offsetIndex];
                var mipmapShader = VoxelMipmapSimpleGroups[i];
                resolution /= 2;

                Vector3 threadNums = Vector3.Min(resolution, new Vector3(8));
                mipmapShader.ThreadNumbers = ToInt3(threadNums);
                mipmapShader.ThreadGroupCounts = ToInt3(resolution / threadNums);

                if (i == 0)
                {
                    mipmapShader.Parameters.Set(VoxelMipmapSimpleKeys.ReadTex, ClipMaps);
                    mipmapShader.Parameters.Set(VoxelMipmapSimpleKeys.ReadOffset, -Offset + new Vector3(0, (int)ClipMapResolution.Y * LayoutSize * (ClipMapCount - 1), 0));
                }
                else
                {
                    mipmapShader.Parameters.Set(VoxelMipmapSimpleKeys.ReadTex, TempMipMaps[i - 1]);
                    mipmapShader.Parameters.Set(VoxelMipmapSimpleKeys.ReadOffset, -Offset + new Vector3(0, 0, 0));
                }
                mipmapShader.Parameters.Set(VoxelMipmapSimpleKeys.WriteTex, TempMipMaps[i]);
                ((RendererBase)mipmapShader).Draw(drawContext);
                //Don't seem to be able to read and write to the same texture, even if the views
                //point to different mipmaps.
                drawContext.CommandList.CopyRegion(TempMipMaps[i], 0, null, MipMaps, i);
                offsetIndex++;
            }
            Array.Copy(PerMapOffsetScale, PerMapOffsetScaleCurrent, PerMapOffsetScale.Length);
        }


        private ObjectParameterKey<Texture> ClipMapskey;
        private ObjectParameterKey<Texture> MipMapskey;
        private ValueParameterKey<Vector4> perMapOffsetScaleKey;
        public void UpdateSamplingLayout(string compositionName)
        {
            ClipMapskey = VoxelStorageTextureClipmapShaderKeys.clipMaps.ComposeWith(compositionName);
            MipMapskey = VoxelStorageTextureClipmapShaderKeys.mipMaps.ComposeWith(compositionName);
            perMapOffsetScaleKey = VoxelStorageTextureClipmapShaderKeys.perMapOffsetScale.ComposeWith(compositionName);
        }
        public ShaderClassSource GetSamplingShader()
        {
            sampler = new ShaderClassSource("VoxelStorageTextureClipmapShader", VoxelSize, ClipMapCount, LayoutSize, ClipMapResolution.Y/2.0f);
            return sampler;
        }
        public void ApplySamplingParameters(VoxelViewContext viewContext, ParameterCollection parameters)
        {
            parameters.Set(ClipMapskey, ClipMaps);
            parameters.Set(MipMapskey, MipMaps);
            parameters.Set(perMapOffsetScaleKey, viewContext.IsVoxelView? PerMapOffsetScaleCurrent : PerMapOffsetScale);
        }
    }
}
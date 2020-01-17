﻿using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Shaders;
using Xenko.Rendering.Materials;
using Xenko.Core.Mathematics;

namespace Xenko.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Emission+Opacity")]
    public class VoxelAttributeEmissionOpacity : IVoxelAttribute
    {
        public enum LightFalloffs
        {
            [Display("Sharp")] Sharp,
            [Display("Physically Based")] PhysicallyBased,
            [Display("Physically Based + Shadowing Heuristic")] Heuristic,
        }

        [NotNull]
        public IVoxelLayout VoxelLayout { get; set; } = new VoxelLayoutIsotropic();

        public List<IVoxelModifierEmissionOpacity> Modifiers { get; set; } = new List<IVoxelModifierEmissionOpacity>();

        public LightFalloffs LightFalloff { get; set; } = LightFalloffs.Heuristic;


        public void PrepareLocalStorage(VoxelStorageContext context, IVoxelStorage storage)
        {
            SetBufferOffset(VoxelLayout.PrepareLocalStorage(context, storage));
        }
        public void PrepareOutputStorage(VoxelStorageContext context, IVoxelStorage storage)
        {
            VoxelLayout.PrepareOutputStorage(context, storage);
        }
        public void ClearOutputStorage()
        {
            VoxelLayout.ClearOutputStorage();
        }




        public void CollectVoxelizationPasses(VoxelizationPassList passList, IVoxelStorer storer, Matrix view, Vector3 resolution, VoxelizationStage stage, bool output)
        {
            passList.defaultVoxelizationMethod.CollectVoxelizationPasses(passList, storer, view, resolution, this, stage, output, true);
        }
        public void CollectAttributes(List<AttributeStream> attributes, VoxelizationStage stage, bool output)
        {
            foreach (IVoxelModifierEmissionOpacity modifier in Modifiers)
            {
                modifier.CollectAttributes(attributes, VoxelizationStage.Post, false);
            }
            attributes.Add(new AttributeStream(this, VoxelizationStage.Post, output));
        }

        public void PostProcess(RenderDrawContext drawContext)
        {
            switch (LightFalloff)
            {
                case LightFalloffs.Sharp:
                    VoxelLayout.PostProcess(drawContext, "VoxelMipmapSimple"); break;
                case LightFalloffs.PhysicallyBased:
                    VoxelLayout.PostProcess(drawContext, "VoxelMipmapPhysicallyBased"); break;
                case LightFalloffs.Heuristic:
                    VoxelLayout.PostProcess(drawContext, "VoxelMipmapHeuristic"); break;
            }
        }




        ShaderClassSource source = new ShaderClassSource("VoxelAttributeEmissionOpacityShader");

        public ShaderSource GetVoxelizationShader()
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(source);
            mixin.AddComposition("layout", VoxelLayout.GetVoxelizationShader(Modifiers));
            return mixin;
        }
        public void UpdateVoxelizationLayout(string compositionName)
        {
            int i = 0;
            foreach (IVoxelModifierEmissionOpacity modifier in Modifiers)
            {
                modifier.UpdateVoxelizationLayout("Modifiers[" + i.ToString() + "].layout." + compositionName);
                i++;
            }
            VoxelLayout.UpdateVoxelizationLayout("layout." + compositionName, Modifiers);
        }
        public void ApplyVoxelizationParameters(ParameterCollection parameters)
        {
            foreach (IVoxelModifierEmissionOpacity modifier in Modifiers)
            {
                modifier.ApplyVoxelizationParameters(parameters);
            }
            VoxelLayout.ApplyVoxelizationParameters(parameters, Modifiers);
        }

        int bufferOffset;

        public void SetBufferOffset(int bo)
        {
            bufferOffset = bo;
        }
        public int GetBufferOffset()
        {
            return bufferOffset;
        }




        public ShaderSource GetSamplingShader()
        {
            return VoxelLayout.GetSamplingShader();
        }
        public void UpdateSamplingLayout(string compositionName)
        {
            VoxelLayout.UpdateSamplingLayout(compositionName);
        }
        public void ApplySamplingParameters(VoxelViewContext viewContext, ParameterCollection parameters)
        {
            VoxelLayout.ApplySamplingParameters(viewContext, parameters);
        }

        int samplerLocalID;

        public void SetLocalSamplerID(int id)
        {
            samplerLocalID = id;
        }
        public int GetLocalSamplerID()
        {
            return samplerLocalID;
        }
    }
}
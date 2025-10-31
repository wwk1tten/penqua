using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public class Underwater : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material material;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        public Color color;
        public float FogDensity = 1;
        [Range(0, 1)]
        public float alpha;
        public float refraction = 0.1f;
        public Texture normalmap;
        public Vector4 UV = new Vector4(1, 1, 0.2f, 0.1f);
    }

    public Settings settings = new Settings();

    class Pass : ScriptableRenderPass
    {
        public Settings settings;
        private RTHandle source;
        private RTHandle tempTexture;
        private string profilerTag;

        public Pass(string profilerTag)
        {
            this.profilerTag = profilerTag;
        }

        public void Setup(RTHandle source)
        {
            this.source = source;
        }

        [Obsolete]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            
            RenderingUtils.ReAllocateIfNeeded(ref tempTexture, descriptor, 
                FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_UnderwaterTempTexture");
        }

        [Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (settings.material == null)
            {
                Debug.LogWarning("Underwater material is null!");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            cmd.Clear();

            try
            {
                // Set material properties
                settings.material.SetFloat("_FogDensity", settings.FogDensity);
                settings.material.SetFloat("_alpha", settings.alpha);
                settings.material.SetColor("_color", settings.color);
                settings.material.SetTexture("_NormalMap", settings.normalmap);
                settings.material.SetFloat("_refraction", settings.refraction);
                settings.material.SetVector("_normalUV", settings.UV);

                // Blit using RTHandle (URP 13+)
                Blitter.BlitCameraTexture(cmd, source, tempTexture);
                Blitter.BlitCameraTexture(cmd, tempTexture, source, settings.material, 0);

                context.ExecuteCommandBuffer(cmd);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Underwater Effect Error: " + e.Message);
            }

            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // Cleanup is handled automatically by RTHandle system
        }

        public void Dispose()
        {
            tempTexture?.Release();
        }
    }

    Pass pass;

    public override void Create()
    {
        pass = new Pass("Underwater Effects");
        name = "Underwater Effects";
        pass.settings = settings;
        pass.renderPassEvent = settings.renderPassEvent;
    }

    [Obsolete]
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game || 
            renderingData.cameraData.cameraType == CameraType.SceneView)
        {
            pass.Setup(renderer.cameraColorTargetHandle);
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material != null)
        {
            renderer.EnqueuePass(pass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            pass?.Dispose();
        }
    }
}

﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SiliconStudio.Assets;
using SiliconStudio.Assets.Compiler;
using SiliconStudio.Assets.Serializers;
using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Xenko.Assets.Scripts;
using SiliconStudio.Xenko.Rendering;
using SiliconStudio.Xenko.Rendering.Composers;

namespace SiliconStudio.Xenko.Assets.Rendering
{
    [DataContract("GraphicsCompositorAsset")]
    [Display(82, "Graphics Compositor")]
    [AssetContentType(typeof(GraphicsCompositor))]
    [AssetDescription(FileExtension)]
    [AssetPartReference(typeof(RenderStage))]
    // TODO: next 2 lines are here to force RenderStage to be serialized as references; ideally it should be separated from asset parts,
    //       be a member attribute on RenderStages such as [ContainFullType(typeof(RenderStage))] and everywhere else is references
    [AssetPartReference(typeof(RootRenderFeature))]
    [AssetPartReference(typeof(ISharedRenderer))]
    [AssetCompiler(typeof(GraphicsCompositorAssetCompiler))]
    public class GraphicsCompositorAsset : AssetComposite
    {
        /// <summary>
        /// The default file extension used by the <see cref="GraphicsCompositorAsset"/>.
        /// </summary>
        public const string FileExtension = ".xkgfxcomp";

        /// <summary>
        /// Gets the cameras used by this composition.
        /// </summary>
        /// <value>The cameras.</value>
        /// <userdoc>The list of cameras used in the graphic pipeline</userdoc>
        [Category]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public SceneCameraSlotCollection Cameras { get; } = new SceneCameraSlotCollection();

        /// <summary>
        /// The list of render stages.
        /// </summary>
        [Category]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public List<RenderStage> RenderStages { get; } = new List<RenderStage>();

        /// <summary>
        ///  The list of render groups.
        /// </summary>
        [Category]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public List<string> RenderGroups { get; } = new List<string>();

        /// <summary>
        /// The list of render features.
        /// </summary>
        [Category]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public List<RootRenderFeature> RenderFeatures { get; } = new List<RootRenderFeature>();

        /// <summary>
        /// The code and values defined by this graphics compositor.
        /// </summary>
        public ISceneRenderer TopLevel { get; set; }

        /// <summary>
        /// The list of graphics compositors.
        /// </summary>
        [Category]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public List<ISharedRenderer> SharedRenderers { get; } = new List<ISharedRenderer>();

        /// <inheritdoc/>
        public override IEnumerable<AssetPart> CollectParts()
        {
            foreach (var renderStage in RenderStages)
                yield return new AssetPart(renderStage.Id, null, newBase => {});
            foreach (var sharedRenderer in SharedRenderers)
                yield return new AssetPart(sharedRenderer.Id, null, newBase => { });
        }

        /// <inheritdoc/>
        public override IIdentifiable FindPart(Guid partId)
        {
            foreach (var renderStage in RenderStages)
            {
                if (renderStage.Id == partId)
                    return renderStage;
            }

            foreach (var sharedRenderer in SharedRenderers)
            {
                if (sharedRenderer.Id == partId)
                    return sharedRenderer;
            }

            return null;
        }

        /// <inheritdoc/>
        public override bool ContainsPart(Guid partId)
        {
            foreach (var renderStage in RenderStages)
            {
                if (renderStage.Id == partId)
                    return true;
            }
            foreach (var sharedRenderer in SharedRenderers)
            {
                if (sharedRenderer.Id == partId)
                    return true;
            }

            return false;
        }

        /// <inheritdoc/>
        protected override object ResolvePartReference(object referencedObject)
        {
            var renderStageReference = referencedObject as RenderStage;
            if (renderStageReference != null)
            {
                foreach (var renderStage in RenderStages)
                {
                    if (renderStage.Id == renderStageReference.Id)
                        return renderStage;
                }
                return null;
            }

            var partReference = referencedObject as ISharedRenderer;
            if (partReference != null)
            {
                foreach (var sharedRenderer in SharedRenderers)
                {
                    if (sharedRenderer.Id == partReference.Id)
                        return sharedRenderer;
                }
                return null;
            }

            return null;
        }
    }
}
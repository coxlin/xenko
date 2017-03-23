﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Xenko.Rendering;
using SiliconStudio.Xenko.Shaders;

namespace SiliconStudio.Xenko.Rendering
{
    public static class XenkoMRTKeys
    {
        public static readonly PermutationParameterKey<ShaderSourceCollection> RenderTargetExtensions = ParameterKeys.NewPermutation<ShaderSourceCollection>();
    }
}

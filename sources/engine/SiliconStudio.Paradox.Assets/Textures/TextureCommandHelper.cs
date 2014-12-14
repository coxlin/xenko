﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Threading;

using SiliconStudio.BuildEngine;
using SiliconStudio.Core;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Paradox.Assets.Materials;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Graphics.Data;
using SiliconStudio.TextureConverter;

namespace SiliconStudio.Paradox.Assets.Textures
{
    /// <summary>
    /// An helper for the compile commands that needs to process textures.
    /// </summary>
    public static class TextureCommandHelper
    {
        /// <summary>
        /// Returns true if the provided int is a power of 2.
        /// </summary>
        /// <param name="x">the int value to test</param>
        /// <returns>true if power of two</returns>
        public static  bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }

        /// <summary>
        /// Returns true if the PVRTC can be used for the provided texture size.
        /// </summary>
        /// <param name="textureSize">the size of the texture</param>
        /// <returns>true if PVRTC is supported</returns>
        public static bool SupportPVRTC(Int2 textureSize)
        {
            return textureSize.X == textureSize.Y && IsPowerOfTwo(textureSize.X);
        }

        /// <summary>
        /// Utility function to check that the texture size is supported on the graphics platform for the provided graphics profile.
        /// </summary>
        /// <param name="textureFormat">The desired type of format for the output texture</param>
        /// <param name="textureSize">The size of the texture</param>
        /// <param name="graphicsProfile">The graphics profile</param>
        /// <param name="platform">The graphics platform</param>
        /// <param name="generateMipmaps">Indicate if mipmaps should be generated for the output texture</param>
        /// <param name="logger">The logger used to log messages</param>
        /// <returns>true if the texture size is supported</returns>
        public static bool TextureSizeSupported(TextureFormat textureFormat, GraphicsPlatform platform, GraphicsProfile graphicsProfile, Int2 textureSize, bool generateMipmaps, Logger logger)
        {
            // compressed DDS files has to have a size multiple of 4.
            if (platform == GraphicsPlatform.Direct3D11 && textureFormat == TextureFormat.Compressed
                && ((textureSize.X % 4) != 0 || (textureSize.Y % 4) != 0))
            {
                logger.Error("DDS compression does not support texture files that do not have a size multiple of 4." +
                             "Please disable texture compression or adjust your texture size to multiple of 4.");
                return false;
            }

            // determine if the desired size if valid depending on the graphics profile
            switch (graphicsProfile)
            {
                case GraphicsProfile.Level_9_1:
                case GraphicsProfile.Level_9_2:
                case GraphicsProfile.Level_9_3:
                    if (generateMipmaps && (!IsPowerOfTwo(textureSize.Y) || !IsPowerOfTwo(textureSize.X)))
                    {
                        logger.Error("Graphic profiles 9.1/9.2/9.3 do not support mipmaps with textures that are not power of 2. " +
                                     "Please disable mipmap generation, modify your texture resolution or upgrade your graphic profile to a value >= 10.0.");
                        return false;
                    }
                    break;
                case GraphicsProfile.Level_10_0:
                case GraphicsProfile.Level_10_1:
                case GraphicsProfile.Level_11_0:
                case GraphicsProfile.Level_11_1:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("graphicsProfile");
            }

            return true;
        }

        /// <summary>
        /// Determine the output format of the texture depending on the platform and asset properties.
        /// </summary>
        /// <param name="parameters">The conversion request parameters</param>
        /// <param name="imageSize">The texture output size</param>
        /// <param name="inputImageFormat">The pixel format of the input image</param>
        /// <param name="textureAsset">The texture asset</param>
        /// <returns>The pixel format to use as output</returns>
        public static PixelFormat DetermineOutputFormat(TextureAsset textureAsset, TextureConvertParameters parameters, Int2 imageSize, PixelFormat inputImageFormat)
        {
            if (textureAsset.SRgb && ((int)parameters.GraphicsProfile < (int)GraphicsProfile.Level_9_2 && parameters.GraphicsPlatform != GraphicsPlatform.Direct3D11))
                throw new NotSupportedException("sRGB is not supported on OpenGl profile level {0}".ToFormat(parameters.GraphicsProfile));

            if (textureAsset.SRgb && textureAsset.Format == TextureFormat.Compressed && parameters.GraphicsPlatform == GraphicsPlatform.OpenGLES)
                throw new NotImplementedException("sRGB compression for OpenGl 3.0 is not implemented yet");

            PixelFormat outputFormat;
            switch (textureAsset.Format)
            {
                case TextureFormat.Compressed:
                    switch (parameters.Platform)
                    {
                        case PlatformType.Android:
                            outputFormat = textureAsset.Alpha == AlphaFormat.None ? PixelFormat.ETC1 : PixelFormat.R8G8B8A8_UNorm;
                            break;

                        case PlatformType.iOS:
                            // PVRTC works only for square POT textures
                            if (SupportPVRTC(imageSize))
                            {
                                switch (textureAsset.Alpha)
                                {
                                    case AlphaFormat.None:
                                        // DXT1 handles 1-bit alpha channel
                                        outputFormat = PixelFormat.PVRTC_4bpp_RGB;
                                        break;
                                    case AlphaFormat.Mask:
                                        // DXT1 handles 1-bit alpha channel
                                        // TODO: Not sure about the equivalent here?
                                        outputFormat = PixelFormat.PVRTC_4bpp_RGBA;
                                        break;
                                    case AlphaFormat.Explicit:
                                    case AlphaFormat.Interpolated:
                                        // DXT3 is good at sharp alpha transitions
                                        // TODO: Not sure about the equivalent here?
                                        outputFormat = PixelFormat.PVRTC_4bpp_RGBA;
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                            }
                            else
                            {
                                outputFormat = PixelFormat.R8G8B8A8_UNorm;
                            }
                            break;
                        case PlatformType.Windows:
                        case PlatformType.WindowsPhone:
                        case PlatformType.WindowsStore:
                            switch (parameters.GraphicsPlatform)
                            {
                                case GraphicsPlatform.Direct3D11:
                                    switch (textureAsset.Alpha)
                                    {
                                        case AlphaFormat.None:
                                        case AlphaFormat.Mask:
                                            // DXT1 handles 1-bit alpha channel
                                            outputFormat = textureAsset.SRgb? PixelFormat.BC1_UNorm_SRgb : PixelFormat.BC1_UNorm;
                                            break;
                                        case AlphaFormat.Explicit:
                                            // DXT3 is good at sharp alpha transitions
                                            outputFormat = textureAsset.SRgb ? PixelFormat.BC2_UNorm_SRgb : PixelFormat.BC2_UNorm;
                                            break;
                                        case AlphaFormat.Interpolated:
                                            // DXT5 is good at alpha gradients
                                            outputFormat = textureAsset.SRgb ? PixelFormat.BC3_UNorm_SRgb : PixelFormat.BC3_UNorm;
                                            break;
                                        default:
                                            throw new ArgumentOutOfRangeException();
                                    }
                                    break;
                                default:
                                    // OpenGL & OpenGL ES on Windows
                                    // TODO: Need to handle OpenGL Desktop compression
                                    outputFormat = textureAsset.SRgb ? PixelFormat.R8G8B8A8_UNorm_SRgb : PixelFormat.R8G8B8A8_UNorm;
                                    break;
                            }
                            break;
                        default:
                            throw new NotSupportedException("Platform " + parameters.Platform + " is not supported by TextureTool");
                    }
                    break;
                case TextureFormat.HighColor:
                    if (textureAsset.SRgb)
                        throw new NotSupportedException("HighColor texture format does not support sRGB color encryption");

                    if (textureAsset.Alpha == AlphaFormat.None)
                        outputFormat = PixelFormat.B5G6R5_UNorm;
                    else if (textureAsset.Alpha == AlphaFormat.Mask)
                        outputFormat = PixelFormat.B5G5R5A1_UNorm;
                    else
                        throw new NotImplementedException("This alpha format requires a TrueColor texture format.");
                    break;
                case TextureFormat.TrueColor:
                    outputFormat = textureAsset.SRgb ? PixelFormat.R8G8B8A8_UNorm_SRgb : PixelFormat.R8G8B8A8_UNorm;
                    break;
                case TextureFormat.AsIs:
                    outputFormat = inputImageFormat;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return outputFormat;
        }

        public static ResultStatus ImportAndSaveTextureImage(UFile sourcePath, string outputUrl, TextureAsset textureAsset, TextureConvertParameters parameters, CancellationToken cancellationToken, Logger logger)
        {
            var assetManager = new AssetManager();

            using (var texTool = new TextureTool())
            using (var texImage = texTool.Load(sourcePath, textureAsset.SRgb))
            {
                // Apply transformations
                texTool.Decompress(texImage, textureAsset.SRgb);

                if (cancellationToken.IsCancellationRequested) // abort the process if cancellation is demanded
                    return ResultStatus.Cancelled;
                
                // Resize the image
                if (textureAsset.IsSizeInPercentage)
                    texTool.Rescale(texImage, textureAsset.Width / 100.0f, textureAsset.Height / 100.0f, Filter.Rescaling.Lanczos3);
                else
                    texTool.Resize(texImage, (int)textureAsset.Width, (int)textureAsset.Height, Filter.Rescaling.Lanczos3);

                if (cancellationToken.IsCancellationRequested) // abort the process if cancellation is demanded
                    return ResultStatus.Cancelled;

                // texture size is now determined, we can cache it
                var textureSize = new Int2(texImage.Width, texImage.Height);

                // Check that the resulting texture size is supported by the targeted graphics profile
                if (!TextureSizeSupported(textureAsset.Format, parameters.GraphicsPlatform, parameters.GraphicsProfile, textureSize, textureAsset.GenerateMipmaps, logger))
                    return ResultStatus.Failed;


                // Apply the color key
                if (textureAsset.ColorKeyEnabled)
                    texTool.ColorKey(texImage, textureAsset.ColorKeyColor);

                if (cancellationToken.IsCancellationRequested) // abort the process if cancellation is demanded
                    return ResultStatus.Cancelled;


                // Pre-multiply alpha
                if (textureAsset.PremultiplyAlpha)
                    texTool.PreMultiplyAlpha(texImage);

                if (cancellationToken.IsCancellationRequested) // abort the process if cancellation is demanded
                    return ResultStatus.Cancelled;


                // Generate mipmaps
                if (textureAsset.GenerateMipmaps)
                {
                    var boxFilteringIsSupported = texImage.Format != PixelFormat.B8G8R8A8_UNorm_SRgb || (IsPowerOfTwo(textureSize.X) && IsPowerOfTwo(textureSize.Y));
                    texTool.GenerateMipMaps(texImage, boxFilteringIsSupported? Filter.MipMapGeneration.Box: Filter.MipMapGeneration.Linear);
                }
                
                if (cancellationToken.IsCancellationRequested) // abort the process if cancellation is demanded
                    return ResultStatus.Cancelled;


                // Convert/Compress to output format
                // TODO: Change alphaFormat depending on actual image content (auto-detection)?
                var outputFormat = DetermineOutputFormat(textureAsset, parameters, textureSize, texImage.Format);
                texTool.Compress(texImage, outputFormat, (TextureConverter.Requests.TextureQuality)parameters.TextureQuality);

                if (cancellationToken.IsCancellationRequested) // abort the process if cancellation is demanded
                    return ResultStatus.Cancelled;


                // Save the texture
                if (parameters.SeparateAlpha)
                {
                    TextureAlphaComponentSplitter.CreateAndSaveSeparateTextures(texTool, texImage, outputUrl, textureAsset.GenerateMipmaps);
                }
                else
                {
                    using (var outputImage = texTool.ConvertToParadoxImage(texImage))
                    {
                        if (cancellationToken.IsCancellationRequested) // abort the process if cancellation is demanded
                            return ResultStatus.Cancelled;

                        assetManager.Save(outputUrl, outputImage.ToSerializableVersion());

                        logger.Info("Compression successful [{3}] to ({0}x{1},{2})", outputImage.Description.Width, outputImage.Description.Height, outputImage.Description.Format, outputUrl);
                    }
                }
            }

            return ResultStatus.Successful;
        }
    }
}
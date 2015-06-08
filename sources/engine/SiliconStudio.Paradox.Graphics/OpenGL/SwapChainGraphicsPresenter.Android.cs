﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
#if SILICONSTUDIO_PLATFORM_ANDROID
using SiliconStudio.Core.Mathematics;
using OpenTK;
using OpenTK.Platform.Android;

namespace SiliconStudio.Paradox.Graphics
{
    public class SwapChainGraphicsPresenter : GraphicsPresenter
    {
        private AndroidGameView gameWindow;
        private Texture backBuffer;

        public SwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters) : base(device, presentationParameters)
        {
            device.InitDefaultRenderTarget(Description);

            backBuffer = Texture.New2D(device, Description.BackBufferWidth, Description.BackBufferHeight, presentationParameters.BackBufferFormat, TextureFlags.RenderTarget | TextureFlags.ShaderResource);
        }

        public override Texture BackBuffer
        {
            get { return backBuffer; }
        }

        public override object NativePresenter
        {
            get { return null; }
        }

        public override bool IsFullScreen
        {
            get
            {
                return gameWindow.WindowState == WindowState.Fullscreen;
            }
            set
            {
                gameWindow.WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
            }
        }

        protected override void ProcessPresentationParameters()
        {
            // Use aspect ratio of device
            gameWindow = (AndroidGameView)Description.DeviceWindowHandle.NativeHandle;
            var panelWidth = gameWindow.Size.Width;
            var panelHeight = gameWindow.Size.Height;
            var panelRatio = (float)panelWidth / panelHeight;

            var desiredWidth = Description.BackBufferWidth;
            var desiredHeight = Description.BackBufferHeight;

            if (panelRatio >= 1.0f) // Landscape => use height as base
            {
                Description.BackBufferHeight = (int)(desiredWidth / panelRatio);
            }
            else // Portrait => use width as base
            {
                Description.BackBufferWidth = (int)(desiredHeight * panelRatio);
            }
        }

        public override void Present()
        {
            GraphicsDevice.Begin();

            GraphicsDevice.windowProvidedRenderTexture.InternalSetSize(gameWindow.Width, gameWindow.Height);

            // If we made a fake render target to avoid OpenGL limitations on window-provided back buffer, let's copy the rendering result to it
            if (backBuffer != GraphicsDevice.windowProvidedRenderTexture)
                GraphicsDevice.CopyScaler2D(backBuffer, GraphicsDevice.windowProvidedRenderTexture,
                    new Rectangle(0, 0, backBuffer.Width, backBuffer.Height),
                    new Rectangle(0, 0, GraphicsDevice.windowProvidedRenderTexture.Width, GraphicsDevice.windowProvidedRenderTexture.Height), false);

            ((AndroidGraphicsContext)gameWindow.GraphicsContext).Swap();

            GraphicsDevice.End();
        }

        protected override void ResizeBackBuffer(int width, int height, PixelFormat format)
        {
        }

        protected override void ResizeDepthStencilBuffer(int width, int height, PixelFormat format)
        {
            ReleaseCurrentDepthStencilBuffer();
        }
    }
}
#endif
﻿// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Toolkit.Graphics;

namespace SharpDX.Toolkit
{
    internal abstract class GamePlatform : IGraphicsDeviceFactory, IGamePlatform
    {
        protected IServiceRegistry Services;

        protected GamePlatform(IServiceRegistry services)
        {
            Services = services;
        }

        public static GamePlatform Create(IServiceRegistry services)
        {
#if WIN8METRO
            return new GamePlatformWinRT(services);
#elif WP8
            return new GamePlatformWP8(services);
#else
            return new GamePlatformDesktop(services);
#endif
        }

        public abstract string DefaultAppDirectory { get; }

        public object WindowContext { get; set; }

        public event EventHandler<EventArgs> Activated;

        public event EventHandler<EventArgs> Deactivated;

        public event EventHandler<EventArgs> Exiting;

        public event EventHandler<EventArgs> Idle;

        public event EventHandler<EventArgs> Resume;

        public event EventHandler<EventArgs> Suspend;

        public abstract GameWindow MainWindow { get; }

        public abstract GameWindow CreateWindow(object windowContext = null, int width = 0, int height = 0);

        public bool IsBlockingRun { get; protected set; }

        public abstract void Run(object windowContext, VoidAction initCallback, VoidAction tickCallback);

        public virtual void Exit()
        {
            Activated = null;
            Deactivated = null;
            Exiting = null;
            Idle = null;
            Resume = null;
            Suspend = null;
        }
        
        protected void OnActivated(object source, EventArgs e)
        {
            EventHandler<EventArgs> handler = Activated;
            if (handler != null) handler(this, e);
        }

        protected void OnDeactivated(object source, EventArgs e)
        {
            EventHandler<EventArgs> handler = Deactivated;
            if (handler != null) handler(this, e);
        }

        protected void OnExiting(object source, EventArgs e)
        {
            EventHandler<EventArgs> handler = Exiting;
            if (handler != null) handler(this, e);
        }

        protected void OnIdle(object source, EventArgs e)
        {
            EventHandler<EventArgs> handler = Idle;
            if (handler != null) handler(this, e);
        }

        protected void OnResume(object source, EventArgs e)
        {
            EventHandler<EventArgs> handler = Resume;
            if (handler != null) handler(this, e);
        }

        protected void OnSuspend(object source, EventArgs e)
        {
            EventHandler<EventArgs> handler = Suspend;
            if (handler != null) handler(this, e);
        }

        protected void AddDevice(GraphicsAdapter graphicsAdapter, DisplayMode mode,  GraphicsDeviceInformation deviceBaseInfo, GameGraphicsParameters prefferedParameters, List<GraphicsDeviceInformation> graphicsDeviceInfos)
        {
            var deviceInfo = deviceBaseInfo.Clone();

            deviceInfo.PresentationParameters.RefreshRate = mode.RefreshRate;

            if (prefferedParameters.IsFullScreen)
            {
                deviceInfo.PresentationParameters.BackBufferWidth = mode.Width;
                deviceInfo.PresentationParameters.BackBufferHeight = mode.Height;
            }
            else
            {
                deviceInfo.PresentationParameters.BackBufferWidth = prefferedParameters.PreferredBackBufferWidth;
                deviceInfo.PresentationParameters.BackBufferHeight = prefferedParameters.PreferredBackBufferHeight;
            }

            // TODO: Handle BackBufferFormat / multisampling / depthstencil format
            deviceInfo.PresentationParameters.BackBufferFormat = prefferedParameters.PreferredBackBufferFormat;
            deviceInfo.PresentationParameters.DepthStencilFormat = prefferedParameters.PreferredDepthStencilFormat;
            deviceInfo.PresentationParameters.MultiSampleCount = MSAALevel.None;

            if (!graphicsDeviceInfos.Contains(deviceInfo))
            {
                graphicsDeviceInfos.Add(deviceInfo);
            }
        }

        public virtual List<GraphicsDeviceInformation> FindBestDevices(GameGraphicsParameters prefferedParameters)
        {
            var graphicsDeviceInfos = new List<GraphicsDeviceInformation>();

            // Iterate on each adapter
            foreach (var graphicsAdapter in GraphicsAdapter.Adapters)
            {
                // Iterate on each preferred graphics profile
                foreach (var featureLevel in prefferedParameters.PreferredGraphicsProfile)
                {
                    // Check if this profile is supported.
                    if (graphicsAdapter.IsProfileSupported(featureLevel))
                    {
                        var deviceInfo = new GraphicsDeviceInformation
                            {
                                Adapter = graphicsAdapter,
                                GraphicsProfile = featureLevel,
                                PresentationParameters =
                                    {
                                        MultiSampleCount = MSAALevel.None,
                                        IsFullScreen = prefferedParameters.IsFullScreen,
                                        PresentationInterval = prefferedParameters.SynchronizeWithVerticalRetrace ? PresentInterval.One : PresentInterval.Immediate,
                                        DeviceWindowHandle = MainWindow.NativeWindow,
                                        RenderTargetUsage = Usage.BackBuffer | Usage.RenderTargetOutput
                                    }
                            };

                        if (graphicsAdapter.CurrentDisplayMode != null)
                        {
                            AddDevice(graphicsAdapter, graphicsAdapter.CurrentDisplayMode, deviceInfo, prefferedParameters, graphicsDeviceInfos);
                        }

                        if (prefferedParameters.IsFullScreen)
                        {
                            // Get display mode for the particular width, height, pixelformat
                            foreach (var displayMode in graphicsAdapter.SupportedDisplayModes)
                            {
                                AddDevice(graphicsAdapter, displayMode, deviceInfo, prefferedParameters, graphicsDeviceInfos);
                            }
                        }

                        // If the profile is supported, we are just using the first best one
                        break;
                    }
                }
            }

            return graphicsDeviceInfos;
        }

        public virtual GraphicsDevice CreateDevice(GraphicsDeviceInformation deviceInformation)
        {
            var device = GraphicsDevice.New(deviceInformation.Adapter, deviceInformation.DeviceCreationFlags);
            device.Presenter = new SwapChainGraphicsPresenter(device, deviceInformation.PresentationParameters);
            return device;
        }
    }
}
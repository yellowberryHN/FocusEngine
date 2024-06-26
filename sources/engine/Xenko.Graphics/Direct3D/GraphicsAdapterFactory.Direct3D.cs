// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_DIRECT3D
using System.Collections.Generic;
using SharpDX.DXGI;

namespace Xenko.Graphics
{
    public static partial class GraphicsAdapterFactory
    {
#if XENKO_PLATFORM_WINDOWS_DESKTOP
        internal static Factory1 NativeFactory;
#else
        internal static Factory2 NativeFactory;
#endif

        /// <summary>
        /// Initializes all adapters with the specified factory.
        /// </summary>
        internal static void InitializeInternal(bool debug)
        {
            staticCollector.Dispose();

#if DIRECTX11_1
            using (var factory = new Factory1())
            NativeFactory = factory.QueryInterface<Factory2>();
#elif XENKO_PLATFORM_UWP
            // Maybe this will become default code for everybody if we switch to DX 11.1/11.2 SharpDX dll?
            NativeFactory = new Factory2();
#else
            NativeFactory = new Factory1();
#endif

            staticCollector.Add(NativeFactory);

            int countAdapters = NativeFactory.GetAdapterCount1();
            var adapterList = new List<GraphicsAdapter>();
            for (int i = 0; i < countAdapters; i++)
            {
                var adapter = new GraphicsAdapter(NativeFactory, i);
                staticCollector.Add(adapter);
                adapterList.Add(adapter);
            }

            defaultAdapter = adapterList.Count > 0 ? adapterList[0] : null;
            adapters = adapterList.ToArray();
        }

        /// <summary>
        /// Gets the <see cref="Factory1"/> used by all GraphicsAdapter.
        /// </summary>
        internal static Factory1 Factory
        {
            get
            {
                lock (StaticLock)
                {
                    Initialize();
                    return NativeFactory;
                }
            }
        }
    }
}
#endif 

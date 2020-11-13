// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.UI.Controls;

namespace Xenko.UI.Renderers
{
    /// <summary>
    /// The default renderer for <see cref="ImageElement"/>.
    /// </summary>
    internal class DefaultImageRenderer : ElementRenderer
    {
        public DefaultImageRenderer(IServiceRegistry services)
            : base(services)
        {
        }

        public override void RenderColor(UIElement element, UIRenderingContext context, UIBatch Batch)
        {
            base.RenderColor(element, context, Batch);

            var image = (ImageElement)element;
            var sprite = image.Source?.GetSprite();
            if (sprite?.Texture == null)
                return;

            float finalOpacity = element.RenderOpacity * image.Tint.A;
            var color = new Color4(image.Tint.R * finalOpacity, image.Tint.B * finalOpacity, image.Tint.G * finalOpacity, finalOpacity);
            Batch.DrawImage(sprite.Texture, ref element.WorldMatrixInternal, ref sprite.RegionInternal, ref element.RenderSizeInternal, ref sprite.BordersInternal, ref color, context.DepthBias, sprite.Orientation);
        }
    }
}

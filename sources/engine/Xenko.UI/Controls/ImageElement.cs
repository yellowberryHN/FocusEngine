// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using System.Diagnostics;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Games;
using Xenko.Graphics;

namespace Xenko.UI.Controls
{
    /// <summary>
    /// Represents a control that displays an image.
    /// </summary>
    [DataContract(nameof(ImageElement))]
    [DebuggerDisplay("ImageElement - Name={Name}")]
    public class ImageElement : UIElement
    {
        private ISpriteProvider source;
        private Sprite sprite;
        private StretchType stretchType = StretchType.Uniform;
        private StretchDirection stretchDirection = StretchDirection.Both;

        /// <summary>
        /// Gets or sets the <see cref="ISpriteProvider"/> for the image.
        /// </summary>
        /// <userdoc>The provider for the image.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider Source
        {
            get { return source;}
            set
            {
                if (source == value)
                    return;

                source = value;
                OnSpriteChanged(source?.GetSprite());
            }
        }

        /// <summary>
        /// Gets or set the color used to tint the image. Default value is White/>.
        /// </summary>
        /// <remarks>The initial image color is multiplied by this color.</remarks>
        /// <userdoc>The color used to tint the image. The default value is white.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Color4 Tint { get; set; } = Color4.White;

        /// <summary>
        /// For backwards compatibility
        /// </summary>
        [DataMemberIgnore]
        public Color Color
        {
            get
            {
                return Tint.ToColor();
            }
            set
            {
                Tint = value.ToColor4();
            }
        }

        /// <summary>
        /// Gets or sets a value that describes how the image should be stretched to fill the destination rectangle.
        /// </summary>
        /// <userdoc>Indicates how the image should be stretched to fill the destination rectangle.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        [DefaultValue(StretchType.Uniform)]
        public StretchType StretchType
        {
            get { return stretchType; }
            set
            {
                stretchType = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates how the image is scaled.
        /// </summary>
        /// <userdoc>Indicates how the image is scaled.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        [DefaultValue(StretchDirection.Both)]
        public StretchDirection StretchDirection
        {
            get { return stretchDirection; }
            set
            {
                stretchDirection = value;
                InvalidateMeasure();
            }
        }

        protected override Vector3 ArrangeOverride(Vector3 finalSizeWithoutMargins)
        {
            return ImageSizeHelper.CalculateImageSizeFromAvailable(sprite, finalSizeWithoutMargins, StretchType, StretchDirection, false);
        }

        protected override Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
        {
            return ImageSizeHelper.CalculateImageSizeFromAvailable(sprite, availableSizeWithoutMargins, StretchType, StretchDirection, true);
        }

        protected override void Update(GameTime time)
        {
            var currentSprite = source?.GetSprite();
            if (sprite != currentSprite)
            {
                OnSpriteChanged(currentSprite);
            }
        }

        private void InvalidateMeasure(object sender, EventArgs eventArgs)
        {
            InvalidateMeasure();
        }

        private void OnSpriteChanged(Sprite currentSprite)
        {
            if (sprite != null)
            {
                sprite.SizeChanged -= InvalidateMeasure;
                sprite.BorderChanged -= InvalidateMeasure;
            }
            sprite = currentSprite;
            InvalidateMeasure();
            if (sprite != null)
            {
                sprite.SizeChanged += InvalidateMeasure;
                sprite.BorderChanged += InvalidateMeasure;
            }
        }
    }
}

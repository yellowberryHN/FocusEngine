// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xenko.Core.Mathematics;

namespace Xenko.UI
{
    /// <summary>
    /// A context containing information for UI layouting.
    /// </summary>
    public class LayoutingContext : IEquatable<LayoutingContext>
    {
        /// <summary>
        /// The resolution of the output target.
        /// </summary>
        public Vector2 RealResolution { get; internal set; }

        /// <summary>
        /// The virtual resolution of the UI.
        /// </summary>
        public Vector3 VirtualResolution { get; internal set; }

        /// <summary>
        /// The ratio between the real and virtual resolution (=real/virtual)
        /// </summary>
        public Vector2 RealVirtualResolutionRatio { get; internal set; }

        /// <summary>
        /// Determine if two <see cref="LayoutingContext"/> are equals.
        /// </summary>
        /// <param name="other">the other context</param>
        /// <returns><value>True</value> if the two contexts are equals</returns>
        public bool Equals(LayoutingContext other)
        {
            return RealResolution == other.RealResolution && RealVirtualResolutionRatio == other.RealVirtualResolutionRatio;
        }
    }
}

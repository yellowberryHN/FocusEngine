// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable 436 // The type 'type' in 'assembly' conflicts with the imported type 'type2' in 'assembly' (due to XenkoVersion being duplicated)

using Xenko.Core.Annotations;

namespace Xenko.GameStudio
{
    public static class XenkoGameStudio
    {
        [NotNull]
        public static string CopyrightText1 => "© 2018 Xenko contributors";

        [NotNull]
        public static string CopyrightText2 => "© 2011-2018 Silicon Studio Corp.";

        [NotNull]
        public static string EditorName => Program.EngineName + " " + Program.GameStudioName + " " + EditorVersion;

        [NotNull]
        public static string EditorVersion => XenkoVersion.VersionToShowInEditor;

        [NotNull]
        public static string EditorVersionWithMetadata => EditorVersion;

        public static string EditorVersionMajor => new System.Version(EditorVersion).ToString(2);

        [NotNull]
        public static string AnswersUrl => "http://answers.xenko.com/";

        [NotNull]
        public static string DocumentationUrl => $"https://doc.xenko.com/{EditorVersionMajor}/";

        [NotNull]
        public static string ForumsUrl => "https://forums.xenko.com/";

        [NotNull]
        public static string ReportIssueUrl => "https://github.com/xenko3d/xenko/issues/";
    }
}

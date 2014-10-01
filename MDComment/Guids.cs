// Guids.cs
// MUST match guids.h
using System;

namespace Microsoft.MDComment
{
    static class GuidList
    {
        public const string guidMDCommentPkgString = "f547e7eb-fa9f-4284-90c9-fab2bd1fa0be";
        public const string guidMDCommentCmdSetString = "df5b3119-2ade-4972-91df-2c2e08cd87bd";
        public const string guidToolWindowPersistanceString = "e4b8d68a-6143-4b5d-b804-2ca167187d34";

        public static readonly Guid guidMDCommentCmdSet = new Guid(guidMDCommentCmdSetString);
    };
}
using System;
using LanguageExt;

namespace Murg.Backend.Input
{
    public sealed class InputRoot
    {
        public readonly Uri Uri;
        public readonly Arr<InputFile> Files;

        public InputRoot(Uri uri, Arr<InputFile> files)
        {
            Uri = uri;
            Files = files;
        }
    }
}
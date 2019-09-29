using LanguageExt;

namespace Murg.Backend.Input
{
    public enum InputFileType { Text, Audio }
    
    // TODO: DU of InputAudioFile InputTextFile
    public sealed class InputFile
    {
        public readonly string Path; // TODO: URI
        public readonly InputFileType Type;
        public readonly Option<string> Content;

        public InputFile(Some<string> path, InputFileType type, Option<string> content)
        {
            Path = path;
            Type = type;
            Content = content;
        }
    }
}
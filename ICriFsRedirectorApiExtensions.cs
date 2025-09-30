using CriFs.V2.Hook.Interfaces;

namespace p5rpc.bustupparam.merging;

internal static class ICriFsRedirectorApiExtensions
{
    public static void AddBind(
        this ICriFsRedirectorApi api,
        string file,
        string bindPath,
        string modId)
    {
        api.AddBindCallback(context =>
        {
            context.RelativePathToFileMap[$@"R2\{bindPath}"] = new()
            {
                new()
                {
                    FullPath = file,
                    LastWriteTime = DateTime.UtcNow,
                    ModId = modId,
                },
            };

            // Log.Verbose($"Bind: {bindPath}\nFile: {file}");
        });
    }
} // https://github.com/RyoTune/P5R.CostumeFramework/blob/main/P5R.CostumeFramework/ICriFsRedirectorApiExtensions.cs
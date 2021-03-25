using System.Collections.Generic;
using System.IO;
using Godot;
using Godot.Collections;
using Google.Protobuf;
using Yarn;
using Yarn.Compiler;
using Array = Godot.Collections.Array;
using File = Godot.File;

namespace YarnSpinnerGodot
{
    [Tool]
    public class YarnSpinnerImporter: EditorImportPlugin
    {
        public override string GetImporterName() => "YarnSpinner.godot.importer";
        public override string GetVisibleName() => "Yarn Spinner";

        private Array _recognizedExtensions = null;
        public override Array GetRecognizedExtensions() => _recognizedExtensions ?? (_recognizedExtensions = new Array(new[] {"yarn", "yarnc"}));
        public override string GetSaveExtension() => "res";
        public override string GetResourceType() => "Resource";

        public override int GetPresetCount() => 0;
        public override Array GetImportOptions(int preset) => new Array();

        public override int Import(string sourceFile, string savePath, Dictionary options, Array platformVariants, Array genFiles)
        {
            if (sourceFile.Contains("res://addons/"))
                return (int)Error.Skip;
            
            YarnProgram yarnResource;
            using (File yarnFile = new File())
            {
                Error error = yarnFile.Open(sourceFile, File.ModeFlags.Read);
                if (error != Error.Ok)
                {
                    GD.PrintErr($"Failed to open ${sourceFile}");
                    return (int) error;
                }

                string yarnContent = yarnFile.GetAsText();
                yarnFile.Close();

                Compiler.CompileString(yarnContent, sourceFile.GetFile(), out Program program,
                    out IDictionary<string, StringInfo> stringTable);
                
                File csvFile = new File();
                string csvPath = sourceFile.Remove(sourceFile.LastIndexOf('.'));
                csvPath = $"{csvPath}.csv";
                error = csvFile.Open(csvPath, File.ModeFlags.Write);
                if (error != Error.Ok)
                {
                    GD.PrintErr($"Failed to open or create {csvPath}");
                    return (int) error;
                }

                csvFile.StoreCsvLine(new []{"", "en"});
                foreach (KeyValuePair<string, StringInfo> stringInfo in stringTable)
                    csvFile.StoreCsvLine(new []{stringInfo.Key, stringInfo.Value.text});
                
                csvFile.Close();
                genFiles.Add(csvPath);
                GD.Print($"Saved translation file {csvPath}");

                using (MemoryStream memoryStream = new MemoryStream())
                using (CodedOutputStream outputStream = new CodedOutputStream(memoryStream))
                {
                    program.WriteTo(outputStream);
                    outputStream.Flush();

                    yarnResource = new YarnProgram
                    {
                        compiledProgram = memoryStream.ToArray()
                    };
                }
            }
            
            Error saveError = ResourceSaver.Save($"{savePath}.{GetSaveExtension()}", yarnResource);
            
            if(saveError != Error.Ok)
                GD.PrintErr($"Failed to import {sourceFile}");

            return (int) saveError;
        }
    }
}
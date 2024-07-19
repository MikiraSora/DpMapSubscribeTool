﻿using System.IO;

namespace DpMapSubscribeTool.Utils;

public static class TempFileHelper
{
    private const string TempFolder = "DpMapSubscribeToolTempFolder";
    private const int RandomStringLength = 10;

    /// <summary>
    ///     Get new temp file path
    /// </summary>
    /// <param name="subTempFolderName"></param>
    /// <param name="prefix"></param>
    /// <param name="extension"></param>
    /// <returns>
    ///     will return like
    ///     "C:\Users\mikir\AppData\Local\Temp\NagekiFumenEditorTempFolder\ParseAndDecodeACBFile\music2857.stUKmOg0Ev.wav"
    /// </returns>
    public static string GetTempFilePath(string subTempFolderName = "misc", string prefix = "tempFile",
        string extension = ".dat", bool randomForUnusedFile = true)
    {
        extension ??= ".unk";
        if (!extension.StartsWith("."))
            extension = "." + extension;

        var tempFolder = Path.Combine(Path.GetTempPath(), TempFolder, subTempFolderName);
        Directory.CreateDirectory(tempFolder);

        while (true)
        {
            var actualPrefix = randomForUnusedFile ? prefix + "." + RandomHepler.RandomString() : prefix;
            var fullTempFileName = Path.Combine(tempFolder, actualPrefix + extension);
            if (!randomForUnusedFile)
                return fullTempFileName;
            if (!File.Exists(fullTempFileName))
                return fullTempFileName;
        }
    }

    public static string GetTempFolderPath(string subTempFolderName = "misc", string prefix = "tempFolder",
        bool random = true)
    {
        while (true)
        {
            var actualPrefix = random ? prefix + "_" + RandomHepler.RandomString() : prefix;
            var tempFolder = Path.Combine(Path.GetTempPath(), TempFolder, subTempFolderName, actualPrefix);
            if (!File.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
                return tempFolder;
            }
        }
    }
}
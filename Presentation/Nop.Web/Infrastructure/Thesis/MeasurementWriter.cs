using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Infrastructure;

namespace Nop.Web.Infrastructure.Thesis;

public partial interface IMeasurementWriter
{
    Task WriteMeasurementAsync(MeasurementRecord measurementRecord);

    Task WriteDatasetManifestAsync(object datasetManifest);
}

public partial class MeasurementWriter : IMeasurementWriter
{
    private static readonly SemaphoreSlim _writeLock = new(1, 1);

    private readonly INopFileProvider _fileProvider;
    private readonly ThesisProfilingSettings _thesisProfilingSettings;

    public MeasurementWriter(INopFileProvider fileProvider,
        ThesisProfilingSettings thesisProfilingSettings)
    {
        _fileProvider = fileProvider;
        _thesisProfilingSettings = thesisProfilingSettings;
    }

    public virtual async Task WriteMeasurementAsync(MeasurementRecord measurementRecord)
    {
        var targetFilePath = GetOutputFilePath(_thesisProfilingSettings.MeasurementsFileName);
        var jsonLine = JsonConvert.SerializeObject(measurementRecord);

        await _writeLock.WaitAsync();
        try
        {
            string fileContent;
            if (_fileProvider.FileExists(targetFilePath))
            {
                var existingContent = await _fileProvider.ReadAllTextAsync(targetFilePath, System.Text.Encoding.UTF8);
                fileContent = string.IsNullOrEmpty(existingContent)
                    ? jsonLine + Environment.NewLine
                    : existingContent + jsonLine + Environment.NewLine;
            }
            else
                fileContent = jsonLine + Environment.NewLine;

            await _fileProvider.WriteAllTextAsync(targetFilePath, fileContent, System.Text.Encoding.UTF8);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public virtual async Task WriteDatasetManifestAsync(object datasetManifest)
    {
        var targetFilePath = GetOutputFilePath(_thesisProfilingSettings.DatasetManifestFileName);
        var json = JsonConvert.SerializeObject(datasetManifest, Formatting.Indented);

        await _writeLock.WaitAsync();
        try
        {
            await _fileProvider.WriteAllTextAsync(targetFilePath, json, System.Text.Encoding.UTF8);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    protected virtual string GetOutputFilePath(string fileName)
    {
        var relativeDirectory = _thesisProfilingSettings.OutputDirectory?.Trim('/')?.Trim('\\') ?? "App_Data/ThesisProfiling";
        var directoryPath = _fileProvider.MapPath($"~/{relativeDirectory}");
        _fileProvider.CreateDirectory(directoryPath);

        return _fileProvider.Combine(directoryPath, fileName);
    }
}

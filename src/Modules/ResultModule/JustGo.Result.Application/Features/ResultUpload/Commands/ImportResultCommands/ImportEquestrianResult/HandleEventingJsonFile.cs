using System.Text.Json;
using JustGo.Authentication.Infrastructure.Exceptions;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.ImportEquestrianResult;

public static class HandleEventingJsonFile
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task ParseJsonFileAsync(
        Stream stream,
        List<Dictionary<string, string>> fileData,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(() =>
            {
                using var reader = new StreamReader(stream);
                var jsonContent = reader.ReadToEnd();

                if (string.IsNullOrWhiteSpace(jsonContent))
                    throw new CustomValidationException("JSON file is empty.");

                var jsonDocument = JsonDocument.Parse(jsonContent);
                var headers = new HashSet<string>();

                if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in jsonDocument.RootElement.EnumerateArray())
                    {
                        if (element.ValueKind == JsonValueKind.Object)
                        {
                            ProcessJsonElement(element, fileData, headers);
                        }
                    }
                }
                else if (jsonDocument.RootElement.ValueKind == JsonValueKind.Object)
                {
                    ProcessJsonElement(jsonDocument.RootElement, fileData, headers);
                }
                else
                {
                    throw new CustomValidationException("JSON must contain an object or array of objects.");
                }

                if (fileData.Count == 0)
                    throw new CustomValidationException("No valid data found in JSON file.");

                //return headers.ToList();
            }, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new CustomValidationException($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex) when (ex is not CustomValidationException)
        {
            throw new CustomValidationException($"Error parsing JSON file: {ex.Message}");
        }
    }

    private static void ProcessJsonElement(JsonElement element, List<Dictionary<string, string>> fileData,
        HashSet<string> headers)
    {
        var rootLevelData = new Dictionary<string, string>();
        JsonElement classesArray = default;
        bool hasClasses = false;

        foreach (var rootProperty in element.EnumerateObject())
        {
            if (rootProperty.Name.Equals("Classes", StringComparison.OrdinalIgnoreCase) &&
                rootProperty.Value.ValueKind == JsonValueKind.Array)
            {
                classesArray = rootProperty.Value;
                hasClasses = true;
            }
            else
            {
                // Capture all root-level properties (event data)
                var flattenedRootProps = FlattenJsonElement(rootProperty.Value, rootProperty.Name);
                foreach (var kvp in flattenedRootProps)
                {
                    rootLevelData[kvp.Key] = kvp.Value;
                    headers.Add(kvp.Key);
                }
            }
        }

        if (hasClasses)
        {
            // Extract participants from Classes array and include root-level data
            ExtractParticipantsFromClasses(classesArray, fileData, headers, rootLevelData);
        }
        else
        {
            // No Classes array, treat as regular object
            var flattenedDict = FlattenJsonObject(element, string.Empty);

            // Add all keys to headers
            foreach (var key in flattenedDict.Keys)
            {
                headers.Add(key);
            }

            fileData.Add(flattenedDict);
        }
    }

    private static void ExtractParticipantsFromClasses(JsonElement classesArray,
        List<Dictionary<string, string>> fileData, HashSet<string> headers,
        Dictionary<string, string> rootLevelData)
    {
        foreach (var classElement in classesArray.EnumerateArray())
        {
            if (classElement.ValueKind != JsonValueKind.Object) continue;

            var classData = new Dictionary<string, string>();
            JsonElement participantsArray = default;
            bool hasParticipants = false;

            foreach (var classProp in classElement.EnumerateObject())
            {
                if (classProp.Name.Equals("Participant", StringComparison.OrdinalIgnoreCase) &&
                    classProp.Value.ValueKind == JsonValueKind.Array)
                {
                    participantsArray = classProp.Value;
                    hasParticipants = true;
                }
                else
                {
                    // Remove Class_ prefix and use original property names
                    var flattenedClassProps = FlattenJsonElement(classProp.Value, classProp.Name);
                    foreach (var kvp in flattenedClassProps)
                    {
                        classData[kvp.Key] = kvp.Value;
                    }
                }
            }

            // Process each participant as a separate row (like CSV)
            if (hasParticipants)
            {
                foreach (var participantElement in participantsArray.EnumerateArray())
                {
                    if (participantElement.ValueKind != JsonValueKind.Object) continue;

                    // Create a new dictionary for this participant (like a CSV row)
                    var participantRow = new Dictionary<string, string>();

                    // Add root-level data (event information) to this participant row
                    foreach (var kvp in rootLevelData)
                    {
                        participantRow[kvp.Key] = kvp.Value;
                        headers.Add(kvp.Key);
                    }

                    // Add class data to this participant row
                    foreach (var kvp in classData)
                    {
                        participantRow[kvp.Key] = kvp.Value;
                        headers.Add(kvp.Key);
                    }

                    // Add participant data to this row
                    var participantData = FlattenJsonObject(participantElement, string.Empty);
                    foreach (var kvp in participantData)
                    {
                        participantRow[kvp.Key] = kvp.Value;
                        headers.Add(kvp.Key);
                    }

                    // Add this participant as a single row to fileData (same as CSV format)
                    fileData.Add(participantRow);
                }
            }
            else
            {
                // No participants in this class, but still include event and class data as a row
                var classRow = new Dictionary<string, string>();

                // Add root-level data (event information)
                foreach (var kvp in rootLevelData)
                {
                    classRow[kvp.Key] = kvp.Value;
                    headers.Add(kvp.Key);
                }

                // Add class data
                foreach (var kvp in classData)
                {
                    classRow[kvp.Key] = kvp.Value;
                    headers.Add(kvp.Key);
                }

                fileData.Add(classRow);
            }
        }
    }

    private static Dictionary<string, string> FlattenJsonObject(JsonElement element, string prefix)
    {
        var result = new Dictionary<string, string>();

        if (element.ValueKind != JsonValueKind.Object)
        {
            // For non-object elements, use "Value" as the key
            result["Value"] = GetJsonElementValue(element);
            return result;
        }

        foreach (var property in element.EnumerateObject())
        {
            // Use original property name without prefix
            var flattenedProperties = FlattenJsonElement(property.Value, property.Name);
            foreach (var kvp in flattenedProperties)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    private static Dictionary<string, string> FlattenJsonElement(JsonElement element, string key)
    {
        var result = new Dictionary<string, string>();

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                // Recursively flatten nested objects without adding prefixes
                foreach (var nestedProp in element.EnumerateObject())
                {
                    // Use original nested property name without prefix
                    var nestedFlattened = FlattenJsonElement(nestedProp.Value, nestedProp.Name);
                    foreach (var kvp in nestedFlattened)
                    {
                        // Handle potential key conflicts by using the nested property name directly
                        var finalKey = kvp.Key;

                        // If there's a conflict, you might want to handle it here
                        // For now, later properties will overwrite earlier ones with the same name
                        result[finalKey] = kvp.Value;
                    }
                }

                break;

            case JsonValueKind.Array:
                // Check if this is an array we should exclude from flattening
                if (IsExcludedArray(key))
                {
                    // Store the entire array as JSON string instead of flattening
                    result[key] = JsonSerializer.Serialize(element, JsonOptions);
                }
                else
                {
                    // Handle arrays by creating indexed or concatenated values
                    if (element.GetArrayLength() == 0)
                    {
                        result[key] = string.Empty;
                    }
                    else
                    {
                        var arrayValues = new List<string>();
                        var arrayIndex = 0;

                        foreach (var item in element.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.Object)
                            {
                                // For objects in arrays, flatten them without prefixes
                                var indexedFlattened = FlattenJsonElement(item, $"{key}_{arrayIndex}");
                                foreach (var kvp in indexedFlattened)
                                {
                                    result[kvp.Key] = kvp.Value;
                                }
                            }
                            else
                            {
                                // For primitive values, concatenate them
                                arrayValues.Add(GetJsonElementValue(item));
                            }

                            arrayIndex++;
                        }

                        // Store concatenated primitive values
                        if (arrayValues.Count > 0)
                        {
                            result[key] = string.Join(" | ", arrayValues);
                        }
                    }
                }

                break;

            default:
                // Store primitive values directly with original key
                result[key] = GetJsonElementValue(element);
                break;
        }

        return result;
    }

    private static bool IsExcludedArray(string arrayKey)
    {
        // Define arrays that should not be flattened
        var excludedArrays = new[]
        {
            "Obstacle",
            "Official"
        };

        return excludedArrays.Contains(arrayKey, StringComparer.OrdinalIgnoreCase);
    }

    private static string GetJsonElementValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => element.GetRawText()
        };
    }
}
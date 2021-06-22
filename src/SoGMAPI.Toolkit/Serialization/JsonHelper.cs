using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SoGModdingAPI.Toolkit.Serialization.Converters;

namespace SoGModdingAPI.Toolkit.Serialization
{
    /// <summary>Encapsulates SMAPI's JSON file parsing.</summary>
    public class JsonHelper
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The JSON settings to use when serializing and deserializing files.</summary>
        public JsonSerializerSettings JsonSettings { get; } = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ObjectCreationHandling = ObjectCreationHandling.Replace, // avoid issue where default ICollection<T> values are duplicated each time the config is loaded
            Converters = new List<JsonConverter>
            {
                new SemanticVersionConverter(),
                new StringEnumConverter()
            }
        };


        /*********
        ** Public methods
        *********/
        /// <summary>Read a JSON file.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="fullPath">The absolute file path.</param>
        /// <param name="result">The parsed content model.</param>
        /// <returns>Returns false if the file doesn't exist, else true.</returns>
        /// <exception cref="ArgumentException">The given <paramref name="fullPath"/> is empty or invalid.</exception>
        /// <exception cref="JsonReaderException">The file contains invalid JSON.</exception>
        public bool ReadJsonFileIfExists<TModel>(string fullPath, out TModel result)
        {
            // validate
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentException("The file path is empty or invalid.", nameof(fullPath));

            // read file
            string json;
            try
            {
                json = File.ReadAllText(fullPath);
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is FileNotFoundException)
            {
                result = default(TModel);
                return false;
            }

            // deserialize model
            try
            {
                result = this.Deserialize<TModel>(json);
                return true;
            }
            catch (Exception ex)
            {
                string error = $"Can't parse JSON file at {fullPath}.";

                if (ex is JsonReaderException)
                {
                    error += " This doesn't seem to be valid JSON.";
                    if (json.Contains("“") || json.Contains("”"))
                        error += " Found curly quotes in the text; note that only straight quotes are allowed in JSON.";
                }
                error += $"\nTechnical details: {ex.Message}";
                throw new JsonReaderException(error);
            }
        }

        /// <summary>Save to a JSON file.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="fullPath">The absolute file path.</param>
        /// <param name="model">The model to save.</param>
        /// <exception cref="InvalidOperationException">The given path is empty or invalid.</exception>
        public void WriteJsonFile<TModel>(string fullPath, TModel model)
            where TModel : class
        {
            // validate
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentException("The file path is empty or invalid.", nameof(fullPath));

            // create directory if needed
            string dir = Path.GetDirectoryName(fullPath);
            if (dir == null)
                throw new ArgumentException("The file path is invalid.", nameof(fullPath));
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // write file
            string json = this.Serialize(model);
            File.WriteAllText(fullPath, json);
        }

        /// <summary>Deserialize JSON text if possible.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="json">The raw JSON text.</param>
        public TModel Deserialize<TModel>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<TModel>(json, this.JsonSettings);
            }
            catch (JsonReaderException)
            {
                // try replacing curly quotes
                if (json.Contains("“") || json.Contains("”"))
                {
                    try
                    {
                        return JsonConvert.DeserializeObject<TModel>(json.Replace('“', '"').Replace('”', '"'), this.JsonSettings);
                    }
                    catch { /* rethrow original error */ }
                }

                throw;
            }
        }

        /// <summary>Serialize a model to JSON text.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="model">The model to serialize.</param>
        /// <param name="formatting">The formatting to apply.</param>
        public string Serialize<TModel>(TModel model, Formatting formatting = Formatting.Indented)
        {
            return JsonConvert.SerializeObject(model, formatting, this.JsonSettings);
        }

        /// <summary>Get a low-level JSON serializer matching the <see cref="JsonSettings"/>.</summary>
        public JsonSerializer GetSerializer()
        {
            return JsonSerializer.CreateDefault(this.JsonSettings);
        }
    }
}

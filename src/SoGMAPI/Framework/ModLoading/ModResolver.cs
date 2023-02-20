using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using SoGModdingAPI.Toolkit;
using SoGModdingAPI.Toolkit.Framework;
using SoGModdingAPI.Toolkit.Framework.ModData;
using SoGModdingAPI.Toolkit.Framework.ModScanning;
using SoGModdingAPI.Toolkit.Framework.UpdateData;
using SoGModdingAPI.Toolkit.Serialization.Models;
using SoGModdingAPI.Toolkit.Utilities.PathLookups;

namespace SoGModdingAPI.Framework.ModLoading
{
    /// <summary>Finds and processes mod metadata.</summary>
    internal class ModResolver
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Get manifest metadata for each folder in the given root path.</summary>
        /// <param name="toolkit">The mod toolkit.</param>
        /// <param name="rootPath">The root path to search for mods.</param>
        /// <param name="modDatabase">Handles access to SoGMAPI's internal mod metadata list.</param>
        /// <param name="useCaseInsensitiveFilePaths">Whether to match file paths case-insensitively, even on Linux.</param>
        /// <returns>Returns the manifests by relative folder.</returns>
        public IEnumerable<IModMetadata> ReadManifests(ModToolkit toolkit, string rootPath, ModDatabase modDatabase, bool useCaseInsensitiveFilePaths)
        {
            foreach (ModFolder folder in toolkit.GetModFolders(rootPath, useCaseInsensitiveFilePaths))
            {
                Manifest? manifest = folder.Manifest;

                // parse internal data record (if any)
                ModDataRecordVersionedFields? dataRecord = modDatabase.Get(manifest?.UniqueID)?.GetVersionedFields(manifest);

                // apply defaults
                if (manifest != null && dataRecord?.UpdateKey is not null)
                    manifest.OverrideUpdateKeys(dataRecord.UpdateKey);

                // build metadata
                bool shouldIgnore = folder.Type == ModType.Ignored;
                ModMetadataStatus status = folder.ManifestParseError == ModParseError.None || shouldIgnore
                    ? ModMetadataStatus.Found
                    : ModMetadataStatus.Failed;

                IModMetadata metadata = new ModMetadata(folder.DisplayName, folder.Directory.FullName, rootPath, manifest, dataRecord, isIgnored: shouldIgnore);
                if (shouldIgnore)
                    metadata.SetStatus(status, ModFailReason.DisabledByDotConvention, "disabled by dot convention");
                else if (status == ModMetadataStatus.Failed)
                {
                    ModFailReason reason = folder.ManifestParseError switch
                    {
                        ModParseError.EmptyFolder or ModParseError.EmptyVortexFolder => ModFailReason.EmptyFolder,
                        ModParseError.XnbMod => ModFailReason.XnbMod,
                        _ => ModFailReason.InvalidManifest
                    };

                    metadata.SetStatus(status, reason, folder.ManifestParseErrorText);
                }

                yield return metadata;
            }
        }

        /// <summary>Validate manifest metadata.</summary>
        /// <param name="mods">The mod manifests to validate.</param>
        /// <param name="apiVersion">The current SoGMAPI version.</param>
        /// <param name="getUpdateUrl">Get an update URL for an update key (if valid).</param>
        /// <param name="getFileLookup">Get a file lookup for the given directory.</param>
        /// <param name="validateFilesExist">Whether to validate that files referenced in the manifest (like <see cref="IManifest.EntryDll"/>) exist on disk. This can be disabled to only validate the manifest itself.</param>
        [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract", Justification = "Manifest values may be null before they're validated.")]
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract", Justification = "Manifest values may be null before they're validated.")]
        public void ValidateManifests(IEnumerable<IModMetadata> mods, ISemanticVersion apiVersion, Func<string, string?> getUpdateUrl, Func<string, IFileLookup> getFileLookup, bool validateFilesExist = true)
        {
            mods = mods.ToArray();

            // validate each manifest
            foreach (IModMetadata mod in mods)
            {
                // skip if already failed
                if (mod.Status == ModMetadataStatus.Failed)
                    continue;

                // validate compatibility from internal data
                switch (mod.DataRecord?.Status)
                {
                    case ModStatus.Obsolete:
                        mod.SetStatus(ModMetadataStatus.Failed, ModFailReason.Obsolete, $"it's obsolete: {mod.DataRecord.StatusReasonPhrase}", this.GetTechnicalReasonForStatusOverride(mod));
                        continue;

                    case ModStatus.AssumeBroken:
                        {
                            // get reason
                            string reasonPhrase = mod.DataRecord.StatusReasonPhrase ?? "it's no longer compatible";

                            // get update URLs
                            List<string> updateUrls = new List<string>();
                            foreach (UpdateKey key in mod.GetUpdateKeys(validOnly: true))
                            {
                                string? url = getUpdateUrl(key.ToString());
                                if (url != null)
                                    updateUrls.Add(url);
                            }

                            // default update URL
                            updateUrls.Add("https://smapi.io/mods");

                            // build error
                            string error = $"{reasonPhrase}. Please check for a ";
                            if (mod.DataRecord.StatusUpperVersion == null || mod.Manifest.Version?.Equals(mod.DataRecord.StatusUpperVersion) == true)
                                error += "newer version";
                            else
                                error += $"version newer than {mod.DataRecord.StatusUpperVersion}";
                            error += " at " + string.Join(" or ", updateUrls);

                            mod.SetStatus(ModMetadataStatus.Failed, ModFailReason.Incompatible, error, this.GetTechnicalReasonForStatusOverride(mod));
                        }
                        continue;
                }

                // validate SoGMAPI version
                if (mod.Manifest.MinimumApiVersion?.IsNewerThan(apiVersion) == true)
                {
                    mod.SetStatus(ModMetadataStatus.Failed, ModFailReason.Incompatible, $"it needs SoGMAPI {mod.Manifest.MinimumApiVersion} or later. Please update SoGMAPI to the latest version to use this mod.");
                    continue;
                }

                // validate manifest format
                if (!ManifestValidator.TryValidateFields(mod.Manifest, out string manifestError))
                {
                    mod.SetStatus(ModMetadataStatus.Failed, ModFailReason.InvalidManifest, $"its {manifestError}");
                    continue;
                }

                // check that DLL exists if applicable
                if (!string.IsNullOrEmpty(mod.Manifest.EntryDll) && validateFilesExist)
                {
                    IFileLookup pathLookup = getFileLookup(mod.DirectoryPath);
                    FileInfo file = pathLookup.GetFile(mod.Manifest.EntryDll!);
                    if (!file.Exists)
                    {
                        mod.SetStatus(ModMetadataStatus.Failed, ModFailReason.InvalidManifest, $"its DLL '{mod.Manifest.EntryDll}' doesn't exist.");
                        continue;
                    }
                }
            }

            // validate IDs are unique
            {
                var duplicatesByID = mods
                    .GroupBy(mod => mod.Manifest?.UniqueID?.Trim(), mod => mod, StringComparer.OrdinalIgnoreCase)
                    .Where(p => !string.IsNullOrEmpty(p.Key) && p.Count() > 1);
                foreach (var group in duplicatesByID)
                {
                    foreach (IModMetadata mod in group)
                    {
                        if (mod.Status == ModMetadataStatus.Failed && mod.FailReason is not (ModFailReason.InvalidManifest or ModFailReason.LoadFailed or ModFailReason.MissingDependencies))
                            continue;

                        string folderList = string.Join(", ", group.Select(p => p.GetRelativePathWithRoot()).OrderBy(p => p));
                        mod.SetStatus(ModMetadataStatus.Failed, ModFailReason.Duplicate, $"you have multiple copies of this mod installed. To fix this, delete these folders and reinstall the mod: {folderList}.");
                    }
                }
            }
        }

        /// <summary>Apply preliminary overrides to the load order based on the SoGMAPI configuration.</summary>
        /// <param name="mods">The mods to process.</param>
        /// <param name="modIdsToLoadEarly">The mod IDs SoGMAPI should load before any other mods (except those needed to load them).</param>
        /// <param name="modIdsToLoadLate">The mod IDs SoGMAPI should load after any other mods.</param>
        public IModMetadata[] ApplyLoadOrderOverrides(IModMetadata[] mods, HashSet<string> modIdsToLoadEarly, HashSet<string> modIdsToLoadLate)
        {
            if (!modIdsToLoadEarly.Any() && !modIdsToLoadLate.Any())
                return mods;

            string[] earlyArray = modIdsToLoadEarly.ToArray();
            string[] lateArray = modIdsToLoadLate.ToArray();

            return mods
                .OrderBy(mod =>
                {
                    string id = mod.Manifest.UniqueID;

                    if (modIdsToLoadEarly.TryGetValue(id, out string? actualId))
                        return -int.MaxValue + Array.IndexOf(earlyArray, actualId);

                    if (modIdsToLoadLate.TryGetValue(id, out actualId))
                        return int.MaxValue - Array.IndexOf(lateArray, actualId);

                    return 0;
                })
                .ToArray();
        }

        /// <summary>Sort the given mods by the order they should be loaded.</summary>
        /// <param name="mods">The mods to process.</param>
        /// <param name="modDatabase">Handles access to SoGMAPI's internal mod metadata list.</param>
        public IEnumerable<IModMetadata> ProcessDependencies(IReadOnlyList<IModMetadata> mods, ModDatabase modDatabase)
        {
            // initialize metadata
            mods = mods.ToArray();
            var sortedMods = new Stack<IModMetadata>();
            var states = mods.ToDictionary(mod => mod, _ => ModDependencyStatus.Queued);

            // handle failed mods
            foreach (IModMetadata mod in mods.Where(m => m.Status == ModMetadataStatus.Failed))
            {
                states[mod] = ModDependencyStatus.Failed;
                sortedMods.Push(mod);
            }

            // sort mods
            foreach (IModMetadata mod in mods)
                this.ProcessDependencies(mods, modDatabase, mod, states, sortedMods, new List<IModMetadata>());

            return sortedMods.Reverse();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Sort a mod's dependencies by the order they should be loaded, and remove any mods that can't be loaded due to missing or conflicting dependencies.</summary>
        /// <param name="mods">The full list of mods being validated.</param>
        /// <param name="modDatabase">Handles access to SoGMAPI's internal mod metadata list.</param>
        /// <param name="mod">The mod whose dependencies to process.</param>
        /// <param name="states">The dependency state for each mod.</param>
        /// <param name="sortedMods">The list in which to save mods sorted by dependency order.</param>
        /// <param name="currentChain">The current change of mod dependencies.</param>
        /// <returns>Returns the mod dependency status.</returns>
        private ModDependencyStatus ProcessDependencies(IReadOnlyList<IModMetadata> mods, ModDatabase modDatabase, IModMetadata mod, IDictionary<IModMetadata, ModDependencyStatus> states, Stack<IModMetadata> sortedMods, ICollection<IModMetadata> currentChain)
        {
            // check if already visited
            switch (states[mod])
            {
                // already sorted or failed
                case ModDependencyStatus.Sorted:
                case ModDependencyStatus.Failed:
                    return states[mod];

                // dependency loop
                case ModDependencyStatus.Checking:
                    // This should never happen. The higher-level mod checks if the dependency is
                    // already being checked, so it can fail without visiting a mod twice. If this
                    // case is hit, that logic didn't catch the dependency loop for some reason.
                    throw new InvalidModStateException($"A dependency loop was not caught by the calling iteration ({string.Join(" => ", currentChain.Select(p => p.DisplayName))} => {mod.DisplayName})).");

                // not visited yet, start processing
                case ModDependencyStatus.Queued:
                    break;

                // sanity check
                default:
                    throw new InvalidModStateException($"Unknown dependency status '{states[mod]}'.");
            }

            // collect dependencies
            ModDependency[] dependencies = this.GetDependenciesFrom(mod.Manifest, mods).ToArray();

            // mark sorted if no dependencies
            if (!dependencies.Any())
            {
                sortedMods.Push(mod);
                return states[mod] = ModDependencyStatus.Sorted;
            }

            // mark failed if missing dependencies
            {
                string[] failedModNames = (
                    from entry in dependencies
                    where entry.IsRequired && entry.Mod == null
                    let displayName = modDatabase.Get(entry.ID)?.DisplayName ?? entry.ID
                    let modUrl = modDatabase.GetModPageUrlFor(entry.ID)
                    orderby displayName
                    select modUrl != null
                        ? $"{displayName}: {modUrl}"
                        : displayName
                ).ToArray();
                if (failedModNames.Any())
                {
                    sortedMods.Push(mod);
                    mod.SetStatus(ModMetadataStatus.Failed, ModFailReason.MissingDependencies, $"it requires mods which aren't installed ({string.Join(", ", failedModNames)}).");
                    return states[mod] = ModDependencyStatus.Failed;
                }
            }

            // dependency min version not met, mark failed
            {
                string[] failedLabels =
                    (
                        from entry in dependencies
                        where
                            entry.Mod != null
                            && entry.MinVersion != null
                            && entry.MinVersion.IsNewerThan(entry.Mod.Manifest.Version)
                        select $"{entry.Mod!.DisplayName} (needs {entry.MinVersion} or later)"
                    )
                    .ToArray();
                if (failedLabels.Any())
                {
                    sortedMods.Push(mod);
                    mod.SetStatus(ModMetadataStatus.Failed, ModFailReason.MissingDependencies, $"it needs newer versions of some mods: {string.Join(", ", failedLabels)}.");
                    return states[mod] = ModDependencyStatus.Failed;
                }
            }

            // process dependencies
            {
                states[mod] = ModDependencyStatus.Checking;

                // recursively sort dependencies
                foreach (ModDependency dependency in dependencies)
                {
                    IModMetadata? requiredMod = dependency.Mod;
                    if (requiredMod == null)
                        continue; // missing dependencies are handled earlier

                    // detect dependency loop
                    var subchain = new List<IModMetadata>(currentChain) { mod };
                    if (states[requiredMod] == ModDependencyStatus.Checking)
                    {
                        sortedMods.Push(mod);
                        mod.SetStatus(ModMetadataStatus.Failed, ModFailReason.MissingDependencies, $"its dependencies have a circular reference: {string.Join(" => ", subchain.Select(p => p.DisplayName))} => {requiredMod.DisplayName}).");
                        return states[mod] = ModDependencyStatus.Failed;
                    }

                    // recursively process each dependency
                    var subStatus = this.ProcessDependencies(mods, modDatabase, requiredMod, states, sortedMods, subchain);
                    switch (subStatus)
                    {
                        // sorted successfully
                        case ModDependencyStatus.Sorted:
                        case ModDependencyStatus.Failed when !dependency.IsRequired: // ignore failed optional dependency
                            break;

                        // failed, which means this mod can't be loaded either
                        case ModDependencyStatus.Failed:
                            sortedMods.Push(mod);
                            mod.SetStatus(ModMetadataStatus.Failed, ModFailReason.MissingDependencies, $"it needs the '{requiredMod.DisplayName}' mod, which couldn't be loaded.");
                            return states[mod] = ModDependencyStatus.Failed;

                        // unexpected status
                        case ModDependencyStatus.Queued:
                        case ModDependencyStatus.Checking:
                            throw new InvalidModStateException($"Something went wrong sorting dependencies: mod '{requiredMod.DisplayName}' unexpectedly stayed in the '{subStatus}' status.");

                        // sanity check
                        default:
                            throw new InvalidModStateException($"Unknown dependency status '{states[mod]}'.");
                    }
                }

                // all requirements sorted successfully
                sortedMods.Push(mod);
                return states[mod] = ModDependencyStatus.Sorted;
            }
        }

        /// <summary>Get the dependencies declared in a manifest.</summary>
        /// <param name="manifest">The mod manifest.</param>
        /// <param name="loadedMods">The loaded mods.</param>
        private IEnumerable<ModDependency> GetDependenciesFrom(IManifest manifest, IReadOnlyList<IModMetadata> loadedMods)
        {
            IModMetadata? FindMod(string id) => loadedMods.FirstOrDefault(m => m.HasID(id));

            // yield dependencies
            foreach (IManifestDependency entry in manifest.Dependencies)
                yield return new ModDependency(entry.UniqueID, entry.MinimumVersion, FindMod(entry.UniqueID), entry.IsRequired);

            // yield content pack parent
            if (manifest.ContentPackFor != null)
                yield return new ModDependency(manifest.ContentPackFor.UniqueID, manifest.ContentPackFor.MinimumVersion, FindMod(manifest.ContentPackFor.UniqueID), isRequired: true);
        }

        /// <summary>Get a technical message indicating why a mod's compatibility status was overridden, if applicable.</summary>
        /// <param name="mod">The mod metadata.</param>
        private string? GetTechnicalReasonForStatusOverride(IModMetadata mod)
        {
            // get compatibility list record
            ModDataRecordVersionedFields? data = mod.DataRecord;
            if (data == null)
                return null;

            // get status label
            string statusLabel = data.Status switch
            {
                ModStatus.AssumeBroken => "'assume broken'",
                ModStatus.AssumeCompatible => "'assume compatible'",
                ModStatus.Obsolete => "obsolete",
                _ => data.Status.ToString()
            };

            // get reason
            string?[] reasons = new[] { data.StatusReasonPhrase, data.StatusReasonDetails }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();

            // build message
            return
                $"marked {statusLabel} in SoGMAPI's internal compatibility list for "
                + (data.StatusUpperVersion != null ? $"versions up to {data.StatusUpperVersion}" : "all versions")
                + ": "
                + (reasons.Any() ? string.Join(": ", reasons) : "no reason given")
                + ".";
        }


        /*********
        ** Private models
        *********/
        /// <summary>Represents a dependency from one mod to another.</summary>
        private readonly struct ModDependency
        {
            /*********
            ** Accessors
            *********/
            /// <summary>The unique ID of the required mod.</summary>
            public string ID { get; }

            /// <summary>The minimum required version (if any).</summary>
            public ISemanticVersion? MinVersion { get; }

            /// <summary>Whether the mod shouldn't be loaded if the dependency isn't available.</summary>
            public bool IsRequired { get; }

            /// <summary>The loaded mod that fulfills the dependency (if available).</summary>
            public IModMetadata? Mod { get; }


            /*********
            ** Public methods
            *********/
            /// <summary>Construct an instance.</summary>
            /// <param name="id">The unique ID of the required mod.</param>
            /// <param name="minVersion">The minimum required version (if any).</param>
            /// <param name="mod">The loaded mod that fulfills the dependency (if available).</param>
            /// <param name="isRequired">Whether the mod shouldn't be loaded if the dependency isn't available.</param>
            public ModDependency(string id, ISemanticVersion? minVersion, IModMetadata? mod, bool isRequired)
            {
                this.ID = id;
                this.MinVersion = minVersion;
                this.Mod = mod;
                this.IsRequired = isRequired;
            }
        }
    }
}

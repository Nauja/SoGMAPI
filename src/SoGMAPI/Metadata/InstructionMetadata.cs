﻿using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using SoGModdingAPI.Events;
using SoGModdingAPI.Framework.ModLoading;
using SoGModdingAPI.Framework.ModLoading.Finders;
using SoGModdingAPI.Framework.ModLoading.RewriteFacades;
using SoGModdingAPI.Framework.ModLoading.Rewriters;

namespace SoGModdingAPI.Metadata
{
    /// <summary>Provides CIL instruction handlers which rewrite mods for compatibility and throw exceptions for incompatible code.</summary>
    internal class InstructionMetadata
    {
        /*********
        ** Fields
        *********/
        /// <summary>The assembly names to which to heuristically detect broken references.</summary>
        /// <remarks>The current implementation only works correctly with assemblies that should always be present.</remarks>
        private readonly string[] ValidateReferencesToAssemblies = { "SecretsOfGrindeaModdingAPI", "Secrets Of Grindea", "SecretsOfGrindea" };


        /*********
        ** Public methods
        *********/
        /// <summary>Get rewriters which detect or fix incompatible CIL instructions in mod assemblies.</summary>
        /// <param name="paranoidMode">Whether to detect paranoid mode issues.</param>
        /// <param name="platformChanged">Whether the assembly was rewritten for crossplatform compatibility.</param>
        /// <param name="rewriteMods">Whether to get handlers which rewrite mods for compatibility.</param>
        public IEnumerable<IInstructionHandler> GetHandlers(bool paranoidMode, bool platformChanged, bool rewriteMods)
        {
            /****
            ** rewrite CIL to fix incompatible code
            ****/
            // rewrite for crossplatform compatibility
            if (rewriteMods)
            {
                if (platformChanged)
                    yield return new MethodParentRewriter(typeof(SpriteBatch), typeof(SpriteBatchFacade));

                // heuristic rewrites
                yield return new HeuristicFieldRewriter(this.ValidateReferencesToAssemblies);
                yield return new HeuristicMethodRewriter(this.ValidateReferencesToAssemblies);

#if HARMONY_2
                // rewrite for SMAPI 3.x (Harmony 1.x => 2.0 update)
                yield return new Harmony1AssemblyRewriter();
#endif
            }

            /****
            ** detect mod issues
            ****/
            // detect broken code
            yield return new ReferenceToMissingMemberFinder(this.ValidateReferencesToAssemblies);
            yield return new ReferenceToMemberWithUnexpectedTypeFinder(this.ValidateReferencesToAssemblies);

            /****
            ** detect code which may impact game stability
            ****/
#if HARMONY_2
            yield return new TypeFinder(typeof(HarmonyLib.Harmony).FullName, InstructionHandleResult.DetectedGamePatch);
#else
            yield return new TypeFinder(typeof(Harmony.HarmonyInstance).FullName, InstructionHandleResult.DetectedGamePatch);
#endif
            yield return new TypeFinder("System.Runtime.CompilerServices.CallSite", InstructionHandleResult.DetectedDynamic);
            yield return new EventFinder(typeof(ISpecializedEvents).FullName, nameof(ISpecializedEvents.UnvalidatedUpdateTicked), InstructionHandleResult.DetectedUnvalidatedUpdateTick);
            yield return new EventFinder(typeof(ISpecializedEvents).FullName, nameof(ISpecializedEvents.UnvalidatedUpdateTicking), InstructionHandleResult.DetectedUnvalidatedUpdateTick);

            /****
            ** detect paranoid issues
            ****/
            if (paranoidMode)
            {
                // filesystem access
                yield return new TypeFinder(typeof(System.Console).FullName, InstructionHandleResult.DetectedConsoleAccess);
                yield return new TypeFinder(typeof(System.IO.File).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.FileStream).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.FileInfo).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.Directory).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.DirectoryInfo).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.DriveInfo).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.FileSystemWatcher).FullName, InstructionHandleResult.DetectedFilesystemAccess);

                // shell access
                yield return new TypeFinder(typeof(System.Diagnostics.Process).FullName, InstructionHandleResult.DetectedShellAccess);
            }
        }
    }
}

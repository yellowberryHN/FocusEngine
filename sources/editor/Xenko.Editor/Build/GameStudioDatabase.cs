// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.BuildEngine;
using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.MicroThreading;
using Xenko.Core.Serialization.Contents;
using Xenko.Assets;
using Xenko.Editor.Preview;
using Xenko.Graphics;

namespace Xenko.Editor.Build
{
    public class GameStudioDatabase : IDisposable
    {
        private readonly Dictionary<ObjectUrl, OutputObject> database = new Dictionary<ObjectUrl, OutputObject>();
        private readonly GameStudioBuilderService assetBuilderService;
        private readonly GameSettingsProviderService settingsProvider;
        internal readonly AssetCompilerContext CompilerContext = new AssetCompilerContext { CompilationContext = typeof(AssetCompilationContext) };
        private readonly MicroThreadLock databaseLock = new MicroThreadLock();
        private readonly GameSettingsAsset databaseGameSettings;
        internal readonly AssetDependenciesCompiler AssetDependenciesCompiler = new AssetDependenciesCompiler(typeof(EditorGameCompilationContext));
        private bool isDisposed;

        public GameStudioDatabase(GameStudioBuilderService assetBuilderService, GameSettingsProviderService settingsProvider)
        {
            this.assetBuilderService = assetBuilderService;
            this.settingsProvider = settingsProvider;

            CompilerContext.Platform = PlatformType.Windows;

            databaseGameSettings = GameSettingsFactory.Create();
            //// TODO: get the best available between 10 and 11
            //databaseGameSettings.GetOrCreate<RenderingSettings>().DefaultGraphicsProfile = GraphicsProfile.Level_11_0;
            CompilerContext.SetGameSettingsAsset(databaseGameSettings);
            CompilerContext.CompilationContext = typeof(EditorGameCompilationContext);

            UpdateGameSettings(settingsProvider.CurrentGameSettings);
            settingsProvider.GameSettingsChanged += OnGameSettingsChanged;

            ((AssetDependencyManager)assetBuilderService.SessionViewModel.DependencyManager).AssetChanged += OnAssetChanged;          
        }

        private void OnAssetChanged(AssetItem sender, bool oldValue, bool newValue)
        {
            //if (newValue)
            //{
            //    AssetDependenciesCompiler.BuildDependencyManager.AssetChanged(sender);
            //}
        }

        public void Dispose()
        {
            isDisposed = true;
            databaseLock.Dispose();
            settingsProvider.GameSettingsChanged -= OnGameSettingsChanged;
            ((AssetDependencyManager)assetBuilderService.SessionViewModel.DependencyManager).AssetChanged -= OnAssetChanged;
            CompilerContext.Dispose();
            database.Clear();
        }

        public ILogger Logger { get; }

        public Task<ISyncLockable> ReserveSyncLock() => databaseLock.ReserveSyncLock();

        public Task<IDisposable> LockAsync() => databaseLock.LockAsync();

        public async Task<IDisposable> MountInCurrentMicroThread()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(GameStudioDatabase));
            if (Scheduler.CurrentMicroThread == null) throw new InvalidOperationException("The database can only be mounted in a micro-thread.");

            var lockObject = await databaseLock.LockAsync();
            // Return immediately if the database was disposed when waiting for the lock
            if (isDisposed)
                return lockObject;

            MicrothreadLocalDatabases.MountDatabase(database.Yield());
            return lockObject;
        }

        public async Task Build(AssetItem asset, BuildDependencyType dependencyType = BuildDependencyType.Runtime)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(GameStudioDatabase));

            var buildUnit = new EditorGameBuildUnit(asset, CompilerContext, AssetDependenciesCompiler);

            try
            {
                assetBuilderService.PushBuildUnit(buildUnit);
                await buildUnit.Wait();
            }
            catch (Exception e)
            {
                Logger?.Error($"An error occurred while building the scene: {e.Message}", e);
                return;
            }

            // Merge build result into the database
            using ((await databaseLock.ReserveSyncLock()).Lock())
            {
                if (isDisposed)
                    return;

                if (buildUnit.Failed)
                {
                    // Build failed => unregister object
                    // 1. If it is first-time scene loading and one of sub-asset failed, it will be in this state, but we don't care
                    // since database will be empty at that point (it won't have any effect)
                    // 2. The second case (the one we actually care about) happens when reloading a recently rebuilt individual asset (i.e. material or texture),
                    // this will actually remove it from database
                    database.Remove(new ObjectUrl(UrlType.Content, asset.Location));
                }

                foreach (var outputObject in buildUnit.OutputObjects)
                {
                    database[outputObject.Key] = outputObject.Value;
                }
            }
        }

        private void OnGameSettingsChanged(object sender, GameSettingsChangedEventArgs e)
        {
            UpdateGameSettings(e.GameSettings);
        }

        private void UpdateGameSettings(GameSettingsAsset currentGameSettings)
        {
            databaseGameSettings.GetOrCreate<EditorSettings>().RenderingMode = currentGameSettings.GetOrCreate<EditorSettings>().RenderingMode;
            databaseGameSettings.GetOrCreate<RenderingSettings>().ColorSpace = currentGameSettings.GetOrCreate<RenderingSettings>().ColorSpace;
            databaseGameSettings.GetOrCreate<Navigation.NavigationSettings>().Groups = currentGameSettings.GetOrDefault<Navigation.NavigationSettings>().Groups;
        }
    }
}

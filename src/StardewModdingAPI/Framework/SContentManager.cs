﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.Xna.Framework;
using StardewModdingAPI.AssemblyRewriters;
using StardewModdingAPI.Framework.Reflection;
using StardewValley;

namespace StardewModdingAPI.Framework
{
    /// <summary>SMAPI's implementation of the game's content manager which lets it raise content events.</summary>
    internal class SContentManager : LocalizedContentManager
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Encapsulates monitoring and logging.</summary>
        private readonly IMonitor Monitor;

        /// <summary>The underlying content manager's asset cache.</summary>
        private readonly IDictionary<string, object> Cache;

        /// <summary>Normalises an asset key to match the cache key.</summary>
        private readonly IPrivateMethod NormaliseAssetKey;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        public SContentManager(IServiceProvider serviceProvider, string rootDirectory, IMonitor monitor)
            : this(serviceProvider, rootDirectory, Thread.CurrentThread.CurrentUICulture, null, monitor) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        /// <param name="currentCulture">The current culture for which to localise content.</param>
        /// <param name="languageCodeOverride">The current language code for which to localise content.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        public SContentManager(IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture, string languageCodeOverride, IMonitor monitor)
            : base(serviceProvider, rootDirectory, currentCulture, languageCodeOverride)
        {
            this.Monitor = monitor;

            IReflectionHelper reflection = new ReflectionHelper();
            this.Cache = reflection
                .GetPrivateField<Dictionary<string, object>>(this, "loadedAssets")
                .GetValue();
            this.NormaliseAssetKey = Constants.TargetPlatform == Platform.Windows
                ? reflection.GetPrivateMethod(typeof(TitleContainer), "GetCleanPath")
                : reflection.GetPrivateMethod(this, nameof(this.NormaliseKeyForMono));
        }

        /// <summary>Load an asset that has been processed by the content pipeline.</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        public override T Load<T>(string assetName)
        {
            assetName = this.NormaliseAssetKey.Invoke<string>(assetName);
            return base.Load<T>(assetName);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Normalise an asset key for Mono.</summary>
        /// <param name="key">The asset key.</param>
        private string NormaliseKeyForMono(string key)
        {
            return key.Replace('\\', '/'); // based on MonoGame's ContentManager.Load<T> logic
        }
    }
}

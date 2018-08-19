using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using xTile;

namespace StardewModdingAPI
{
    /// <summary>An API that provides access to a content pack.</summary>
    public interface IContentPack
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The full path to the content pack's folder.</summary>
        string DirectoryPath { get; }

        /// <summary>The content pack's manifest.</summary>
        IManifest Manifest { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Read a JSON file from the content pack folder.</summary>
        /// <typeparam name="TModel">The model type. This should be a plain class that has public properties for the data you want. The properties can be complex types.</typeparam>
        /// <param name="path">The file path relative to the content pack directory.</param>
        /// <returns>Returns the deserialised model, or <c>null</c> if the file doesn't exist or is empty.</returns>
        TModel ReadJsonFile<TModel>(string path) where TModel : class;

        /// <summary>Save data to a JSON file in the content pack's folder.</summary>
        /// <typeparam name="TModel">The model type. This should be a plain class that has public properties for the data you want. The properties can be complex types.</typeparam>
        /// <param name="path">The file path relative to the mod folder.</param>
        /// <param name="data">The arbitrary data to save.</param>
        void WriteJsonFile<TModel>(string path, TModel data) where TModel : class;

        /// <summary>Load content from the content pack folder (if not already cached), and return it. When loading a <c>.png</c> file, this must be called outside the game's draw loop.</summary>
        /// <typeparam name="T">The expected data type. The main supported types are <see cref="Map"/>, <see cref="Texture2D"/>, and dictionaries; other types may be supported by the game's content pipeline.</typeparam>
        /// <param name="key">The local path to a content file relative to the content pack folder.</param>
        /// <exception cref="ArgumentException">The <paramref name="key"/> is empty or contains invalid characters.</exception>
        /// <exception cref="ContentLoadException">The content asset couldn't be loaded (e.g. because it doesn't exist).</exception>
        T LoadAsset<T>(string key);

        /// <summary>Get the underlying key in the game's content cache for an asset. This can be used to load custom map tilesheets, but should be avoided when you can use the content API instead. This does not validate whether the asset exists.</summary>
        /// <param name="key">The the local path to a content file relative to the content pack folder.</param>
        /// <exception cref="ArgumentException">The <paramref name="key"/> is empty or contains invalid characters.</exception>
        string GetActualAssetKey(string key);
    }
}

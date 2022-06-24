using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Framework.Utilities;
using StardewModdingAPI.Internal;
using StardewValley;

namespace StardewModdingAPI.Framework
{
    /// <summary>SMAPI's extension of the game's core <see cref="Game1"/>, used to inject events.</summary>
    internal class SGame : Game1
    {
        /*********
        ** Fields
        *********/
        /// <summary>Encapsulates monitoring and logging for SMAPI.</summary>
        private readonly Monitor Monitor;

        /// <summary>The maximum number of consecutive attempts SMAPI should make to recover from a draw error.</summary>
        private readonly Countdown DrawCrashTimer = new(60); // 60 ticks = roughly one second

        /// <summary>Simplifies access to private game code.</summary>
        private readonly Reflector Reflection;

        /// <summary>Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</summary>
        private readonly Action<string> ExitGameImmediately;

        /// <summary>The initial override for <see cref="Multiplayer"/>. This value is null after initialization.</summary>
        private SMultiplayer? InitialMultiplayer;

        /// <summary>Raised when the instance is updating its state (roughly 60 times per second).</summary>
        private readonly Action<SGame, GameTime, Action> OnUpdating;

        /// <summary>Raised after the instance finishes loading its initial content.</summary>
        private readonly Action OnContentLoaded;


        /*********
        ** Accessors
        *********/
        /// <summary>Whether the current update tick is the first one for this instance.</summary>
        public bool IsFirstTick = true;

        /// <summary>The number of ticks until SMAPI should notify mods that the game has loaded.</summary>
        /// <remarks>Skipping a few frames ensures the game finishes initializing the world before mods try to change it.</remarks>
        public Countdown AfterLoadTimer { get; } = new(5);

        /// <summary>Whether the game is saving and SMAPI has already raised <see cref="IGameLoopEvents.Saving"/>.</summary>
        public bool IsBetweenSaveEvents { get; set; }

        /// <summary>Whether the game is creating the save file and SMAPI has already raised <see cref="IGameLoopEvents.SaveCreating"/>.</summary>
        public bool IsBetweenCreateEvents { get; set; }

        /// <summary>The cached <see cref="Farmer.UniqueMultiplayerID"/> value for this instance's player.</summary>
        public long? PlayerId { get; private set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="playerIndex">The player index.</param>
        /// <param name="instanceIndex">The instance index.</param>
        /// <param name="monitor">Encapsulates monitoring and logging for SMAPI.</param>
        /// <param name="reflection">Simplifies access to private game code.</param>
        /// <param name="input">Manages the game's input state.</param>
        /// <param name="modHooks">Handles mod hooks provided by the game.</param>
        /// <param name="multiplayer">The core multiplayer logic.</param>
        /// <param name="exitGameImmediately">Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</param>
        /// <param name="onUpdating">Raised when the instance is updating its state (roughly 60 times per second).</param>
        /// <param name="onContentLoaded">Raised after the game finishes loading its initial content.</param>
        public SGame(PlayerIndex playerIndex, int instanceIndex, Monitor monitor, Reflector reflection, SModHooks modHooks, SMultiplayer multiplayer, Action<string> exitGameImmediately, Action<SGame, GameTime, Action> onUpdating, Action onContentLoaded)
            : base(playerIndex, instanceIndex)
        {
            // init XNA
            Game1.graphics.GraphicsProfile = GraphicsProfile.HiDef;

            // hook into game
            Game1.multiplayer = this.InitialMultiplayer = multiplayer;
            Game1.hooks = modHooks;
            this._locations = new ObservableCollection<GameLocation>();

            // init SMAPI
            this.Monitor = monitor;
            this.Reflection = reflection;
            this.ExitGameImmediately = exitGameImmediately;
            this.OnUpdating = onUpdating;
            this.OnContentLoaded = onContentLoaded;
        }

        /// <inheritdoc />
        protected override void LoadContent()
        {
            base.LoadContent();

            this.OnContentLoaded();
        }

        /*********
        ** Protected methods
        *********/
        /// <summary>Initialize the instance when the game starts.</summary>
        protected override void Initialize()
        {
            base.Initialize();

            // The game resets public static fields after the class is constructed (see GameRunner.SetInstanceDefaults), so SMAPI needs to re-override them here.
            Game1.multiplayer = this.InitialMultiplayer;

            // The Initial* fields should no longer be used after this point, since mods may further override them after initialization.
            this.InitialMultiplayer = null;
        }

        /// <summary>The method called when the instance is updating its state (roughly 60 times per second).</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        protected override void Update(GameTime gameTime)
        {
            // update
            try
            {
                this.OnUpdating(this, gameTime, () => base.Update(gameTime));
                this.PlayerId = Game1.player?.UniqueMultiplayerID;
            }
            finally
            {
                this.IsFirstTick = false;
            }
        }

        /// <summary>The method called to draw everything to the screen.</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        /// <param name="target_screen">The render target, if any.</param>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "copied from game code as-is")]
        protected override void _draw(GameTime gameTime, RenderTarget2D target_screen)
        {
            Context.IsInDrawLoop = true;
            try
            {
                base._draw(gameTime, target_screen);
                this.DrawCrashTimer.Reset();
            }
            catch (Exception ex)
            {
                // log error
                this.Monitor.Log($"An error occurred in the overridden draw loop: {ex.GetLogSummary()}", LogLevel.Error);

                // exit if irrecoverable
                if (!this.DrawCrashTimer.Decrement())
                {
                    this.ExitGameImmediately("The game crashed when drawing, and SMAPI was unable to recover the game.");
                    return;
                }

                // recover draw state
                try
                {
                    if (Game1.spriteBatch.IsOpen(this.Reflection))
                    {
                        this.Monitor.Log("Recovering sprite batch from error...");
                        Game1.spriteBatch.End();
                    }

                    Game1.uiMode = false;
                    Game1.uiModeCount = 0;
                    Game1.nonUIRenderTarget = null;
                }
                catch (Exception innerEx)
                {
                    this.Monitor.Log($"Could not recover game draw state: {innerEx.GetLogSummary()}", LogLevel.Error);
                }
            }
            Context.IsInDrawLoop = false;
        }
    }
}

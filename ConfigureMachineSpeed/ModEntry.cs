using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using System.Linq;
using ConfigureMachineSpeed.Framework;

namespace ConfigureMachineSpeed
{
    public class ModEntry : Mod
    {
        private ModConfig Config;
        private readonly float EPSILON = 0.01f;

        /*
         * Mod Entry & Config Validation
         */

        public override void Entry(IModHelper helper)
        {
            this.Config = processConfig(helper.ReadConfig<ModConfig>());
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.UpdateTicking += this.OnUpdateTicking;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }
 
        // Validate and alter the input from config.json
        private ModConfig processConfig(ModConfig cfg)
        {
            if (cfg.UpdateInterval <= 0)
                cfg.UpdateInterval = 1;
            foreach (MachineConfig machine in cfg.Machines)
            {
                if (!machine.UsePercent && machine.Time <= 0)
                    machine.Time = 10;
            }
            return cfg;
        }

        /*
         * Event Callbacks
         */

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (!Context.IsMainPlayer)
                return;
            configureAllMachines();
        }

        private void OnUpdateTicking(object sender, UpdateTickingEventArgs e)
        {
            if (!Context.IsMainPlayer)
                return;
            if (e.IsMultipleOf(this.Config.UpdateInterval))
            {
                configureAllMachines();
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsPlayerFree || Game1.currentMinigame != null)
                return;

            // Reload config
            if (e.Button == this.Config.ReloadConfigKey)
            {
                this.Config = processConfig(this.Helper.ReadConfig<ModConfig>());
                Game1.hudMessages.Add(new HUDMessage("Machine Speed Configuration Reloaded", 2));
            }
        }


        /*
         * Helper Methods
         */

        // Sweep through all the machines in the world and configure them with the appropriate configuration.
        private void configureAllMachines()
        {
            IEnumerable<GameLocation> locations = GetLocations();
            foreach (MachineConfig cfg in this.Config.Machines)
            {
                foreach (GameLocation loc in locations)
                {
                    Func<KeyValuePair<Vector2, StardewValley.Object>, bool> nameMatch = p => p.Value.name == cfg.Name;
                    foreach (KeyValuePair<Vector2, StardewValley.Object> pair in loc.objects.Pairs)
                    {
                        if (nameMatch(pair))
                            configureMachine(cfg, pair.Value);
                    }
                }
            }
        }

        // Configures a given machine with a given configuration.
        // Be sure to check that the second parameter is a machine before passing it to this function.
        private void configureMachine (MachineConfig cfg, StardewValley.Object obj)
        {
            if (!(obj.MinutesUntilReady % 10 == 8))
            {
                // If machine hasn't been configured yet.   
                if (obj is Cask && obj.heldObject.Value != null)
                {
                    // Configure casks
                    if (cfg.UsePercent && Math.Abs(cfg.Time - 100f) > EPSILON)
                    {
                        // By percentage
                        ((Cask)obj).daysToMature.Value = ((Cask)obj).daysToMature.Value * cfg.Time / 100;
                    } else
                    {
                        // By minutes
                        ((Cask)obj).daysToMature.Value = cfg.Time / 1440;
                    }
                    obj.MinutesUntilReady = 168;
                } else if (obj.MinutesUntilReady > 0) {
                    // Configure all machines other than casks
                    float x;
                    if (cfg.UsePercent && Math.Abs(cfg.Time - 100f) > EPSILON)
                    {
                        // By percentage
                        obj.MinutesUntilReady = Math.Max(((int)(obj.MinutesUntilReady * cfg.Time / 100 / 10)) * 10 - 2, 8);
                    }
                    else if (Math.Abs(cfg.Time - 100f) > EPSILON)
                    {
                        // By minutes
                        obj.MinutesUntilReady = Math.Max(((int)(cfg.Time / 10)) * 10 - 2, 8);
                    }
                }
            } else if (obj is Cask && obj.heldObject.Value == null)
            {
                this.Monitor.Log($"Cask Reset: MinutesUntilReady={obj.MinutesUntilReady}, daysToMature={((Cask)obj).daysToMature.Value}");
                obj.MinutesUntilReady = 0;
            }
        }

        /// Get all game locations.
        /// Copied with permission from Pathoschild
        public static IEnumerable<GameLocation> GetLocations()
        {
            return Game1.locations
                .Concat(
                    from location in Game1.locations.OfType<BuildableGameLocation>()
                    from building in location.buildings
                    where building.indoors.Value != null
                    select building.indoors.Value
                );
        }

    }
}

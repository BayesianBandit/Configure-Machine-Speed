using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Buildings;
using System.Linq;

namespace ConfigureMachineSpeed
{
    public class ModEntry : Mod
    {
        private ModConfig Config;

         public override void Entry(IModHelper helper)
        {
            this.Config = processConfig(helper.ReadConfig<ModConfig>());
            printConfig();
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.UpdateTicking += this.OnUpdateTicking;
        }
 
        private ModConfig processConfig(ModConfig cfg)
        {
            if (cfg.UpdateInterval <= 0)
                cfg.UpdateInterval = 1;
            foreach (MachineConfig machine in cfg.Machines)
            {
                if (machine.Minutes <= 0)
                    machine.Minutes = 10;
                machine.Minutes = ((int)machine.Minutes / 10) * 10;
            }
            return cfg;
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            configureAllMachines();
        }

        private void OnUpdateTicking(object sender, UpdateTickingEventArgs e)
        {
            if (e.IsMultipleOf(this.Config.UpdateInterval))
            {
                configureAllMachines();
            }
        }

        private void configureAllMachines()
        {
            IEnumerable<GameLocation> locations = GetLocations();
            foreach (MachineConfig cfg in this.Config.Machines)
            {
                foreach (GameLocation loc in locations)
                {
                    Func<KeyValuePair<Vector2, StardewValley.Object>, bool> nameMatch = p => p.Value.name == cfg.Name;
                    foreach (KeyValuePair<Vector2, StardewValley.Object> pair in loc.objects.Pairs.Where(nameMatch))
                        configureMachine(cfg, pair.Value);
                }
            }
        }

        private void configureMachine (MachineConfig cfg, StardewValley.Object obj)
        {
            if (cfg.Minutes < obj.MinutesUntilReady)
                obj.MinutesUntilReady = cfg.Minutes;
        }

        /// <summary>Get all game locations.</summary>
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

        private void printConfig()
        {
            foreach (MachineConfig machine in this.Config.Machines)
            {
                this.Monitor.Log($"{machine.Name} minutes = {machine.Minutes}");
            }
        }
    }
}

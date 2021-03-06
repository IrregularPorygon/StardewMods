﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;

namespace GiantCropRing
{
    public class ModEntry : Mod
    {
        private ModConfig config;
        private Texture2D giantRingTexture;

        public override void Entry(IModHelper helper)
        {
            SaveEvents.BeforeSave += this.BeforeSave;
            MenuEvents.MenuChanged += this.MenuChanged;
            this.config = helper.ReadConfig<ModConfig>();
            this.giantRingTexture = this.Helper.Content.Load<Texture2D>("assets/ring.png");

            GiantRing.texture = this.giantRingTexture;
            GiantRing.price = this.config.cropRingPrice / 2;
        }

        private void MenuChanged(object sender, EventArgsClickableMenuChanged e)
        {
            if (Game1.activeClickableMenu is ShopMenu)
            {
                var shop = (ShopMenu) Game1.activeClickableMenu;
                if (shop.portraitPerson != null && shop.portraitPerson.name == "Pierre") // && Game1.dayOfMonth % 7 == )
                {
                    var items =
                        this.Helper.Reflection.GetField<Dictionary<Item, int[]>>(shop, "itemPriceAndStock").GetValue();
                    var selling = this.Helper.Reflection.GetField<List<Item>>(shop, "forSale").GetValue();

                    var ring = new GiantRing();
                    items.Add(ring, new[] {this.config.cropRingPrice, int.MaxValue});
                    selling.Add(ring);
                }
            }
        }

        private void BeforeSave(object sender, EventArgs e)
        {
            var chance = 0.0;
            if (Game1.player.leftRing.Value is GiantRing) chance += this.config.cropChancePercentWithRing;
            if (Game1.player.rightRing.Value is GiantRing) chance += this.config.cropChancePercentWithRing;

            if (!this.config.shouldWearingBothRingsDoublePercentage && chance > this.config.cropChancePercentWithRing)
                chance = this.config.cropChancePercentWithRing;


            if (chance > 0) this.MaybeChangeCrops(chance, Game1.getFarm());
        }

        private void MaybeChangeCrops(double chance, GameLocation environment)
        {
            foreach (var tup in this.GetValidCrops())
            {
                var xTile = (int) tup.Item1.X;
                var yTile = (int) tup.Item1.Y;

                var crop = tup.Item2;


                var rand = new Random((int) Game1.uniqueIDForThisGame + (int) Game1.stats.DaysPlayed + xTile * 2000 +
                                      yTile).NextDouble();


                var okCrop = true;
                if (crop.currentPhase == crop.phaseDays.Count - 1 &&
                    (crop.indexOfHarvest == 276 || crop.indexOfHarvest == 190 || crop.indexOfHarvest == 254) &&
                    rand < chance)
                {
                    for (var index1 = xTile - 1; index1 <= xTile + 1; ++index1)
                    {
                        for (var index2 = yTile - 1; index2 <= yTile + 1; ++index2)
                        {
                            var key = new Vector2(index1, index2);
                            if (!environment.terrainFeatures.ContainsKey(key) ||
                                !(environment.terrainFeatures[key] is HoeDirt) ||
                                (environment.terrainFeatures[key] as HoeDirt).crop == null ||
                                (environment.terrainFeatures[key] as HoeDirt).crop.indexOfHarvest !=
                                crop.indexOfHarvest)
                            {
                                okCrop = false;

                                break;
                            }
                        }

                        if (!okCrop)
                            break;
                    }

                    if (!okCrop)
                        continue;

                    for (var index1 = xTile - 1; index1 <= xTile + 1; ++index1)
                    for (var index2 = yTile - 1; index2 <= yTile + 1; ++index2)
                    {
                        var index3 = new Vector2(index1, index2);
                        (environment.terrainFeatures[index3] as HoeDirt).crop = null;
                    }

                    (environment as Farm).resourceClumps.Add(new GiantCrop(crop.indexOfHarvest,
                        new Vector2(xTile - 1, yTile - 1)));
                }
            }
        }

        private List<Tuple<Vector2, Crop>> GetValidCrops()
        {
            return Game1.locations.Where(gl => gl is Farm).SelectMany(gl => (gl as Farm).terrainFeatures.Pairs.Where(
                    tf =>
                        tf.Value is HoeDirt hd && hd.crop != null
                                               && hd.state == 1).Select(hd =>
                    new Tuple<Vector2, Crop>(hd.Key, (hd.Value as HoeDirt).crop))
                .Where(c => !(c.Item2.dead || !c.Item2.seasonsToGrowIn.Contains(Game1.currentSeason)))).ToList();
        }
    }
}
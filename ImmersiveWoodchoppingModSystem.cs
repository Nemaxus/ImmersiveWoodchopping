using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace ImmersiveWoodchopping
{
    public class ImmersiveWoodchoppingModSystem : ModSystem
    {
        ModConfig config = new ModConfig();
        public readonly Dictionary<string, CraftingRecipeIngredient> choppingRecipes = new Dictionary<string, CraftingRecipeIngredient>();

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            config.ReadOrGenerateConfig(api);
        }
        public override void Start(ICoreAPI api)
        {
            api.RegisterCollectibleBehaviorClass("WoodChopping", typeof(WoodChopping));
            api.RegisterBlockBehaviorClass("AxeChoppable", typeof(BlockBehaviorAxeChoppable));
            
        }

        

        public override double ExecuteOrder()
        {
            return 1;
        }


        public override void AssetsFinalize(ICoreAPI api)
        {
            GenerateBasicFirewoodRecipeList(api);
            AssingDrops(api);

 
            if (api.Side == EnumAppSide.Server)
            {

                foreach (var item in api.World.Items)
                {
                    if (item.Code == null) continue;
                    if (item.Code.Path.StartsWith("axe-") && !WildcardUtil.Match("*-ruined", item.Code.Path))
                    {
                        item.CollectibleBehaviors = item.CollectibleBehaviors.Append(new WoodChopping(item));
                    }
                }
            }
            //Always check on which game side you add your behavior!
        }

        public void GenerateBasicFirewoodRecipeList(ICoreAPI api)
        {
            foreach (var grecipe in api.World.GridRecipes)
            {
                if (grecipe.Output.Code.Path.StartsWith("firewood"))
                {
                    AssetLocation icode;
                    string ipath;

                    foreach (CraftingRecipeIngredient ingredient in grecipe.resolvedIngredients)
                    {
                        icode = ingredient.Code;
                        ipath = icode.Path;

                        if (ipath.StartsWith("log-"))
                        {
                            if (ingredient.AllowedVariants != null)
                            {
                                foreach (var variant in ingredient.AllowedVariants)
                                {
                                    if (!choppingRecipes.ContainsKey(variant))
                                    {
                                        choppingRecipes.Add(new AssetLocation("log-" + variant.Replace("-ud", "-*")).ToString(), grecipe.Output);
                                        grecipe.Enabled = false;
                                        grecipe.ShowInCreatedBy = false;
                                    }
                                }
                            }
                            else if (choppingRecipes.ContainsKey(icode.ToString().Replace("-ud", "-*")))
                            {
                                choppingRecipes[icode.ToString().Replace("-ud", "-*")] = grecipe.Output;
                                grecipe.Enabled = false;
                                grecipe.ShowInCreatedBy = false;
                            }
                        }
                        else if (ipath.StartsWith("logsection-placed-"))
                        {
                            if (!choppingRecipes.ContainsKey(icode.ToString()))
                            {
                                choppingRecipes.Add(icode.ToString().Replace("-ne-ud", "-*-*"), grecipe.Output);
                                grecipe.Enabled = false;
                                grecipe.ShowInCreatedBy = false;
                            }
                            else
                            {
                                choppingRecipes[ingredient.Code.ToString().Replace("-ne-ud", "-*-*")] = grecipe.Output;
                                grecipe.Enabled = false;
                                grecipe.ShowInCreatedBy = false;
                            }
                        }
                    }
                }
            }
        }

        public Dictionary<string, CraftingRecipeIngredient> GetFirewoodRecipesList()
        {
            return choppingRecipes;
        }

        public void AssingDrops(ICoreAPI api)
        {
            foreach (var block in api.World.Blocks)
            {
                if (block.Code == null) continue;
                if (block.Code.Path.StartsWith("log-placed") ||
                    block.Code.Path.StartsWith("logsection-placed"))
                {
                    CraftingRecipeIngredient firewoodResult = null;

                    foreach (var key in choppingRecipes.Keys)
                    {
                        if (WildcardUtil.Match(new AssetLocation(key), block.Code))
                        {
                            firewoodResult = choppingRecipes[key];
                            break;
                        }
                    }
                    //Debug.WriteLine(block.Code);
                    if (firewoodResult != null)
                    {
                        BlockBehaviorAxeChoppable behaviour = new BlockBehaviorAxeChoppable(block);
                        JObject jobj = new JObject
                        {
                            { "hideInteractionHelpInSurvival", new JValue(false) },
                            { "drop", new JValue(firewoodResult.Code.ToString()) },
                            { "dropAmount", new JValue(firewoodResult.Quantity) },
                        };
                        behaviour.Initialize(new JsonObject(jobj));

                        block.BlockBehaviors = block.BlockBehaviors.Append(behaviour);
                        //Debug.WriteLine("Found " + block.Code);
                        if (api.World.Config.TryGetBool("logInOffhandChopping") == true && block.StorageFlags < EnumItemStorageFlags.Offhand)
                        {
                            block.StorageFlags = block.StorageFlags + (int)EnumItemStorageFlags.Offhand;
                        }
                    }
                }
            }
        }
    }
}

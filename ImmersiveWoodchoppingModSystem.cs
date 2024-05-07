using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel.Design;
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
        public readonly Dictionary<string, CraftingRecipeIngredient> choppingRecipes = new();
        public readonly List<string> choppingMaterials = new();

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
            GenerateFirewoodRecipeList(api);
            AssingDrops(api);

            if (api.Side == EnumAppSide.Server)
            {
                foreach (var item in api.World.Items)
                {
                    if (item.Code == null) continue;
                    if (item.Tool == EnumTool.Axe && !WildcardUtil.Match("*-ruined", item.Code.Path))
                    //if (item.Code.Path.StartsWith("axe-") && !WildcardUtil.Match("*-ruined", item.Code.Path))
                    {
                        item.CollectibleBehaviors = item.CollectibleBehaviors.Append(new WoodChopping(item));
                        //Debug.WriteLine("Behavior added to: " + item.Code);
                    }
                }
            }
            //Always check on which game side you add your behavior!
        }

        public void GenerateFirewoodRecipeList(ICoreAPI api)
        {
            foreach (var grecipe in api.World.GridRecipes)
            {
                if (!grecipe.Output.Code.Path.StartsWith("firewood")) continue;
                if (grecipe.resolvedIngredients.Length != 2) continue;

                bool flag = false;
                if (grecipe.Width == 1 && grecipe.Height == 2)
                {
                    flag = true;
                }
                else if (grecipe.Width == 2 && grecipe.Height == 1)
                {
                    flag = true;
                }
                if (!flag) continue;

                AssetLocation icode;
                string ipath;
                string icodefirstpart;
                bool enabled = !api.World.Config.GetBool(Constants.ModId + ":DisableGridRecipe", true);

                foreach (CraftingRecipeIngredient ingredient in grecipe.resolvedIngredients)
                {
                    icode = ingredient.Code;
                    ipath = icode.Path;
                    if (ingredient.Type != EnumItemClass.Block) continue;

                    icodefirstpart = icode.FirstCodePart();

                    string genVariant;

                    if (ingredient.AllowedVariants != null)
                    {
                        foreach (var variant in ingredient.AllowedVariants)
                        {
                            genVariant = icode.ToString().Replace("*", variant).Replace("-ne", "-*").Replace("-ud", "-*");
                            
                                //new AssetLocation(icode.Domain, icodefirstpart + "-" + variant.Replace("-ne-ud", "-*-*").Replace("-ud", "-*")).ToString();
                            if (!choppingRecipes.ContainsKey(genVariant))
                            {
                                choppingRecipes.Add(genVariant, grecipe.Output);
                            }
                            else
                            {
                                choppingRecipes[genVariant] = grecipe.Output;
                            }
                        }
                    }
                    else
                    {
                        genVariant = icode.ToString().Replace("-ne", "-*").Replace("-ud", "-*");
                        if (!choppingRecipes.ContainsKey(genVariant))
                        {
                            choppingRecipes.Add(genVariant, grecipe.Output);
                        }
                        else
                        {
                            choppingRecipes[genVariant] = grecipe.Output;
                        }
                    }

                    if (!choppingMaterials.Contains(icodefirstpart))
                    {
                        choppingMaterials.Add(icodefirstpart);
                    }
                    grecipe.Enabled = enabled;
                    grecipe.ShowInCreatedBy = enabled;
                }

            }
            foreach (string key in choppingRecipes.Keys)
            {
                Debug.WriteLine(key + " " + choppingRecipes[key].Code);
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
                if (!choppingMaterials.Contains(block.Code.FirstCodePart())) continue;

                CraftingRecipeIngredient firewoodResult = null;

                foreach (var key in choppingRecipes.Keys)
                {
                    if (WildcardUtil.Match(new AssetLocation(key), block.Code))
                    {
                        firewoodResult = choppingRecipes[key];
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
                    /*if (api.World.Config.TryGetBool("logInOffhandChopping") == true && block.StorageFlags < EnumItemStorageFlags.Offhand)
                    {
                        block.StorageFlags = block.StorageFlags + (int)EnumItemStorageFlags.Offhand;
                    }*/
                }
            }
        }
    }
}

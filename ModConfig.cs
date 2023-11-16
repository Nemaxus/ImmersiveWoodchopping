using System.Security.Cryptography.X509Certificates;
using Vintagestory.API.Common;

namespace ImmersiveWoodchopping
{
    public class ModConfig
    {
        ImmersiveWoodchoppingConfig config;
        public void ReadOrGenerateConfig(ICoreAPI api)
        {
            try
            {
                config = api.LoadModConfig<ImmersiveWoodchoppingConfig>("ImmersiveWoodchoppingConfig.json");
                if (config == null)
                {
                    api.StoreModConfig(new ImmersiveWoodchoppingConfig(), "ImmersiveWoodchoppingConfig.json");
                    config = api.LoadModConfig<ImmersiveWoodchoppingConfig>("ImmersiveWoodchoppingConfig.json");
                }
                else
                {
                    api.StoreModConfig(new ImmersiveWoodchoppingConfig(config), "ImmersiveWoodchoppingConfig.json");
                }
            }
            catch
            {
                api.StoreModConfig(new ImmersiveWoodchoppingConfig(), "ImmersiveWoodchoppingConfig.json");
                config = api.LoadModConfig<ImmersiveWoodchoppingConfig>("ImmersiveWoodchoppingConfig.json");
            }

            api.World.Config.SetBool("AutoLogPlacement", config.AutoLogPlacement);
            api.World.Config.SetBool("damageToolOnChop", config.damageToolOnChop);
            api.World.Config.SetInt("intsaChopMinTier", config.intsaChopMinTier);
        }
    }
}
using ProtoBuf;

namespace ImmersiveWoodchopping
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ImmersiveWoodchoppingConfig
    {
        public bool AutoLogPlacement = false;
        public bool DamageToolOnChop = false;
        public int IntsaChopMinTier = 1;
        public bool DisableGridRecipe = true;
        public ImmersiveWoodchoppingConfig()
        {

        }

        public ImmersiveWoodchoppingConfig(ImmersiveWoodchoppingConfig previousConfig)
        {
            AutoLogPlacement = previousConfig.AutoLogPlacement;
            DamageToolOnChop = previousConfig.DamageToolOnChop;
            IntsaChopMinTier = previousConfig.IntsaChopMinTier;
            DisableGridRecipe = previousConfig.DisableGridRecipe;
        }
    }
}
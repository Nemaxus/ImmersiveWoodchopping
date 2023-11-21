namespace ImmersiveWoodchopping
{
    public class ImmersiveWoodchoppingConfig
    {
        public bool AutoLogPlacement = false;
        public bool DamageToolOnChop = false;
        public int IntsaChopMinTier = 1;
        public ImmersiveWoodchoppingConfig()
        {

        }

        public ImmersiveWoodchoppingConfig(ImmersiveWoodchoppingConfig previousConfig)
        {
            AutoLogPlacement = previousConfig.AutoLogPlacement;
            DamageToolOnChop = previousConfig.DamageToolOnChop;
            IntsaChopMinTier = previousConfig.IntsaChopMinTier;
        }
    }
}
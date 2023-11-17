namespace ImmersiveWoodchopping
{
    public class ImmersiveWoodchoppingConfig
    {
        public bool AutoLogPlacement = false;
        public bool damageToolOnChop = false;
        public int intsaChopMinTier = 1;
        public ImmersiveWoodchoppingConfig()
        {

        }

        public ImmersiveWoodchoppingConfig(ImmersiveWoodchoppingConfig previousConfig)
        {
            AutoLogPlacement = previousConfig.AutoLogPlacement;
            damageToolOnChop = previousConfig.damageToolOnChop;
            intsaChopMinTier = previousConfig.intsaChopMinTier;
        }
    }
}